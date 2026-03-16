using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace OnvifDeviceManager.Services;

public class SoapClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    public SoapClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<XElement> SendRequestAsync(string url, XElement body, string? username = null, string? password = null)
    {
        var envelope = CreateSoapEnvelope(body, username, password);
        var content = new StringContent(envelope.ToString(), Encoding.UTF8, "application/soap+xml");

        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        var responseDoc = XDocument.Parse(responseContent);
        var responseBody = responseDoc.Descendants(SoapNs + "Body").FirstOrDefault();

        if (responseBody == null)
            throw new InvalidOperationException("Invalid SOAP response: no Body element found");

        var fault = responseBody.Element(SoapNs + "Fault");
        if (fault != null)
        {
            var reason = fault.Descendants(SoapNs + "Text").FirstOrDefault()?.Value
                ?? fault.Element(SoapNs + "Reason")?.Value
                ?? "Unknown SOAP fault";
            throw new SoapFaultException(reason);
        }

        return responseBody;
    }

    private XDocument CreateSoapEnvelope(XElement body, string? username, string? password)
    {
        var header = new XElement(SoapNs + "Header");

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            header.Add(CreateSecurityHeader(username, password));
        }

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SoapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "s", SoapNs),
                header,
                new XElement(SoapNs + "Body", body)
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
