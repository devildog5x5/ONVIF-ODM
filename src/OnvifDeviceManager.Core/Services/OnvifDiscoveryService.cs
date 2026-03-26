using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifDiscoveryService
{
    private static readonly XNamespace WsaNs = "http://schemas.xmlsoap.org/ws/2004/08/addressing";
    private static readonly XNamespace WsdNs = "http://schemas.xmlsoap.org/ws/2005/04/discovery";
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace TdsNs = "http://www.onvif.org/ver10/network/wsdl";

    private const string MulticastAddress = "239.255.255.250";
    private const int MulticastPort = 3702;

    public async Task<List<OnvifDevice>> DiscoverDevicesAsync(int timeoutSeconds = 5, CancellationToken cancellationToken = default)
    {
        var devices = new List<OnvifDevice>();
        var discoveredAddresses = new HashSet<string>();

        var probeMessage = CreateProbeMessage();
        var probeBytes = Encoding.UTF8.GetBytes(probeMessage);

        var networkInterfaces = GetActiveNetworkInterfaces();

        var tasks = networkInterfaces.Select(ni =>
            DiscoverOnInterfaceAsync(ni, probeBytes, timeoutSeconds, discoveredAddresses, devices, cancellationToken));

        await Task.WhenAll(tasks);

        return devices;
    }

    private async Task DiscoverOnInterfaceAsync(
        IPAddress localAddress,
        byte[] probeBytes,
        int timeoutSeconds,
        HashSet<string> discoveredAddresses,
        List<OnvifDevice> devices,
        CancellationToken cancellationToken)
    {
        try
        {
            using var udpClient = new UdpClient(new IPEndPoint(localAddress, 0));
            udpClient.Client.ReceiveTimeout = timeoutSeconds * 1000;

            var multicastEndpoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), MulticastPort);
            await udpClient.SendAsync(probeBytes, probeBytes.Length, multicastEndpoint);

            var endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                var remainingTime = endTime - DateTime.UtcNow;
                if (remainingTime <= TimeSpan.Zero)
                    break;

                using var recvCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                recvCts.CancelAfter(remainingTime);

                try
                {
                    var result = await udpClient.ReceiveAsync(recvCts.Token).ConfigureAwait(false);
                    var response = Encoding.UTF8.GetString(result.Buffer);
                    var device = ParseProbeResponse(response);

                    if (device != null)
                    {
                        lock (discoveredAddresses)
                        {
                            if (discoveredAddresses.Add(device.Address))
                            {
                                device.IsOnline = true;
                                device.Status = DeviceStatus.Online;
                                device.LastSeen = DateTime.Now;
                                devices.Add(device);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (AggregateException ae)
                {
                    // Dispose/cancel can surface as AggregateException; treat like socket cancel.
                    if (!ae.InnerExceptions.All(IsBenignUdpReceiveException))
                        throw;
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Interface may not support multicast
        }
    }

    private static bool IsBenignUdpReceiveException(Exception ex) =>
        ex is OperationCanceledException
        or SocketException
        or ObjectDisposedException;

    private static List<IPAddress> GetActiveNetworkInterfaces()
    {
        var addresses = new List<IPAddress>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            if (!ni.SupportsMulticast) continue;

            var props = ni.GetIPProperties();
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    addresses.Add(addr.Address);
                }
            }
        }

        if (addresses.Count == 0)
            addresses.Add(IPAddress.Any);

        return addresses;
    }

    private static string CreateProbeMessage()
    {
        var messageId = $"uuid:{Guid.NewGuid()}";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope""
            xmlns:a=""http://schemas.xmlsoap.org/ws/2004/08/addressing""
            xmlns:d=""http://schemas.xmlsoap.org/ws/2005/04/discovery""
            xmlns:dn=""http://www.onvif.org/ver10/network/wsdl"">
  <s:Header>
    <a:Action s:mustUnderstand=""1"">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>
    <a:MessageID>{messageId}</a:MessageID>
    <a:ReplyTo>
      <a:Address>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</a:Address>
    </a:ReplyTo>
    <a:To s:mustUnderstand=""1"">urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To>
  </s:Header>
  <s:Body>
    <d:Probe>
      <d:Types>dn:NetworkVideoTransmitter</d:Types>
    </d:Probe>
  </s:Body>
</s:Envelope>";
    }

    private static OnvifDevice? ParseProbeResponse(string response)
    {
        try
        {
            var doc = XDocument.Parse(response);
            var probeMatch = doc.Descendants(WsdNs + "ProbeMatch").FirstOrDefault();
            if (probeMatch == null) return null;

            var xAddrs = probeMatch.Element(WsdNs + "XAddrs")?.Value;
            if (string.IsNullOrEmpty(xAddrs)) return null;

            var serviceAddress = xAddrs.Split(' ').FirstOrDefault() ?? xAddrs;

            var address = "Unknown";
            try
            {
                var uri = new Uri(serviceAddress);
                address = uri.Host;
            }
            catch { }

            var scopes = probeMatch.Element(WsdNs + "Scopes")?.Value ?? string.Empty;
            var scopeList = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var name = ExtractScopeValue(scopeList, "onvif://www.onvif.org/name/");
            var manufacturer = ExtractScopeValue(scopeList, "onvif://www.onvif.org/manufacturer/");
            var model = ExtractScopeValue(scopeList, "onvif://www.onvif.org/model/");
            var hardware = ExtractScopeValue(scopeList, "onvif://www.onvif.org/hardware/");

            return new OnvifDevice
            {
                ServiceAddress = serviceAddress,
                XAddrs = xAddrs,
                Address = address,
                Name = Uri.UnescapeDataString(name),
                Manufacturer = Uri.UnescapeDataString(manufacturer),
                Model = Uri.UnescapeDataString(model),
                HardwareId = Uri.UnescapeDataString(hardware),
                Scopes = scopeList
            };
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractScopeValue(string[] scopes, string prefix)
    {
        var scope = scopes.FirstOrDefault(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return scope != null ? scope[prefix.Length..] : string.Empty;
    }
}
