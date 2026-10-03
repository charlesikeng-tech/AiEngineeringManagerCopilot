namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public class SsoProtocolHttpClientFactory
{
    public virtual HttpClient Create(string authority) =>
        new(new SsoBackchannel(authority)) { Timeout = TimeSpan.FromSeconds(20) };
}
