using Maynard.Auth;

namespace Maynard.Configuration;

public class AuthConfigurationBuilder : Builder
{
    public AuthConfigurationBuilder SetAudience(string audience) => OnceOnly<AuthConfigurationBuilder>(() => JwtHelper.Config.ServerAudience = audience);
    public AuthConfigurationBuilder SetIssuer(string issuer) => OnceOnly<AuthConfigurationBuilder>(() => JwtHelper.Config.ServerIssuer = issuer);
    public AuthConfigurationBuilder SetKeys(string privateKey, string publicKey) => OnceOnly<AuthConfigurationBuilder>(() =>
    {
        JwtHelper.Config.PrivateKey = privateKey;
        JwtHelper.Config.PublicKey = publicKey;
    });

    public AuthConfigurationBuilder SetTokenValidity(int secondsToLive, int cacheDuration = 0) => OnceOnly<AuthConfigurationBuilder>(() =>
    {
        JwtHelper.Config.LifetimeInSeconds = secondsToLive;
        JwtHelper.Config.CacheLifetimeInSeconds = cacheDuration;
    });
    
    public AuthConfigurationBuilder AddAuthenticationEventHandler(EventHandler<TokenInfo> handler) => OnceOnly<AuthConfigurationBuilder>(() => JwtHelper.Config.OnAuthenticated += handler);

}