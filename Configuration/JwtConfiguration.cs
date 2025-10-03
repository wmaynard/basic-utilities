using Maynard.Auth;
using Maynard.Interfaces;

namespace Maynard.Configuration;

internal class JwtConfiguration
{
    internal EventHandler<TokenInfo> OnAuthenticated;
    internal string ServerAudience { get; set; }
    internal string ServerIssuer { get; set; }
    internal string PrivateKey { get; set; }
    internal string PublicKey { get; set; }
    internal int LifetimeInSeconds { get; set; }
    internal int CacheLifetimeInSeconds { get; set; }

    public void Validate()
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(ServerAudience))
            errors.Add("Server Audience is required.");
        if (string.IsNullOrWhiteSpace(ServerIssuer))
            errors.Add("Server Issuer is required.");
        if (string.IsNullOrWhiteSpace(PrivateKey))
            errors.Add("Private Key is required.");
        if (string.IsNullOrWhiteSpace(PublicKey))
            errors.Add("Public Key is required.");
        if (LifetimeInSeconds < 1)
            errors.Add("Lifetime must be greater than 0.");
        if (CacheLifetimeInSeconds < 1)
            errors.Add("Cache Lifetime must be greater than 0.");
    }
}