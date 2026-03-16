using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace OnvifDeviceManager.Services;

public class SoapClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private static readonly XNamespace Soap12Ns = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace Soap11Ns = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    public SoapClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            Credentials = null
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<XElement> SendRequestAsync(string url, XElement body, string? username = null, string? password = null)
    {
        HttpResponseMessage response;
        string responseContent;

        try
        {
            var envelope = CreateSoapEnvelope(body, username, password);
            var content = new StringContent(envelope.ToString(), Encoding.UTF8, "application/soap+xml");

            response = await _httpClient.PostAsync(url, content);
            responseContent = await response.Content.ReadAsStringAsync();

            // Some cameras (Axis) return 401 and require re-sending with digest auth
            // or may return SOAP 1.1 with text/xml content type
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Try again with HTTP Basic auth header as fallback
                if (!string.IsNullOrEmpty(username))
                {
                    var basicContent = new StringContent(envelope.ToString(), Encoding.UTF8, "application/soap+xml");
                    var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                    basicContent.Headers.Add("Authorization", $"Basic {creds}");

                    response = await _httpClient.PostAsync(url, basicContent);
                    responseContent = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new SoapFaultException($"Authentication failed (HTTP 401). Verify username and password.");
                }
                else
                {
                    throw new SoapFaultException("Authentication required (HTTP 401)");
                }
            }

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new SoapFaultException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");
            }
        }
        catch (TaskCanceledException)
        {
            throw new SoapFaultException($"Connection timed out reaching {url}");
        }
        catch (HttpRequestException ex)
        {
            throw new SoapFaultException($"Network error connecting to {url}: {ex.Message}");
        }
        catch (SoapFaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("SoapClient.SendRequestAsync - HTTP phase", ex);
            throw new SoapFaultException($"Connection error: {ex.Message}");
        }

        // Parse response XML
        XDocument responseDoc;
        try
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                throw new SoapFaultException("Empty response from device");

            responseContent = responseContent.Trim();
            if (!responseContent.StartsWith("<"))
                throw new SoapFaultException($"Device returned non-XML response: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");

            responseDoc = XDocument.Parse(responseContent);
        }
        catch (SoapFaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("SoapClient.SendRequestAsync - XML parse", ex);
            throw new SoapFaultException($"Failed to parse device response as XML: {ex.Message}");
        }

        // Find the Body element — try SOAP 1.2 first, then SOAP 1.1
        var responseBody = responseDoc.Descendants(Soap12Ns + "Body").FirstOrDefault()
                        ?? responseDoc.Descendants(Soap11Ns + "Body").FirstOrDefault();

        if (responseBody == null)
        {
            CrashLogger.Log($"No SOAP Body found in response from {url}. Full response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
            throw new SoapFaultException("Invalid response from device: no SOAP Body element found");
        }

        // Check for SOAP faults (both 1.1 and 1.2)
        var fault = responseBody.Element(Soap12Ns + "Fault")
                 ?? responseBody.Element(Soap11Ns + "Fault");
        if (fault != null)
        {
            var reason = fault.Descendants(Soap12Ns + "Text").FirstOrDefault()?.Value
                ?? fault.Descendants(Soap11Ns + "faultstring").FirstOrDefault()?.Value
                ?? fault.Element(Soap12Ns + "Reason")?.Value
                ?? fault.Element(Soap11Ns + "faultstring")?.Value
                ?? "Unknown device error";
            throw new SoapFaultException(reason);
        }

        return responseBody;
    }

    private XDocument CreateSoapEnvelope(XElement body, string? username, string? password)
    {
        var header = new XElement(Soap12Ns + "Header");

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            header.Add(CreateSecurityHeader(username, password));
        }

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Soap12Ns + "Envelope",
                new XAttribute(XNamespace.Xmlns + "s", Soap12Ns),
                header,
                new XElement(Soap12Ns + "Body", body)
            )
        );
    }

    private XElement CreateSecurityHeader(string username, string password)
    {
        var nonce = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        var created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var nonceBase64 = Convert.ToBase64String(nonce);
        var digest = ComputePasswordDigest(nonce, created, password);

        return new XElement(WsseNs + "Security",
            new XAttribute(XNamespace.Xmlns + "wsse", WsseNs),
            new XAttribute(XNamespace.Xmlns + "wsu", WsuNs),
            new XElement(WsseNs + "UsernameToken",
                new XElement(WsseNs + "Username", username),
                new XElement(WsseNs + "Password",
                    new XAttribute("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"),
                    digest),
                new XElement(WsseNs + "Nonce",
                    new XAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"),
                    nonceBase64),
                new XElement(WsuNs + "Created", created)
            )
        );
    }

    private static string ComputePasswordDigest(byte[] nonce, string created, string password)
    {
        var createdBytes = Encoding.UTF8.GetBytes(created);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var combined = new byte[nonce.Length + createdBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(createdBytes, 0, combined, nonce.Length, createdBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, nonce.Length + createdBytes.Length, passwordBytes.Length);

        var hash = SHA1.HashData(combined);
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class SoapFaultException : Exception
{
    public SoapFaultException(string message) : base(message) { }
}
