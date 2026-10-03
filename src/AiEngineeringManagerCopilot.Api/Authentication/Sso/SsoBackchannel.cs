using System.Net;
using System.Net.Sockets;

namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public sealed class SsoBackchannel : DelegatingHandler
{
    private readonly string host;

    public SsoBackchannel(string authority)
    {
        host = new Uri(authority).Host;
        InnerHandler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (context, ct) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
                if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
                    throw new HttpRequestException("SSO endpoint did not resolve to a public address.");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch { socket.Dispose(); throw; }
            }
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri;
        if (uri is null || uri.Scheme != "https" || uri.Host != host || uri.Port != 443 || uri.UserInfo != "")
            throw new HttpRequestException("Untrusted SSO endpoint.");
        return base.SendAsync(request, ct);
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return bytes[0] is not (0 or 10 or 127) && bytes[0] < 224 &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) &&
                   !(bytes[0] == 198 && bytes[1] is 18 or 19) &&
                   !(bytes[0] == 192 && bytes[1] == 0) &&
                   !(bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99) &&
                   !(bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) &&
                   !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
               (bytes[0] & 0xe0) == 0x20 &&
               !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] < 2) &&
               !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8) &&
               !(bytes[0] == 0x20 && bytes[1] == 0x02);
    }
}
