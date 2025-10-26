using System.Security.Claims;
using Maynard.Auth;
using Maynard.Extensions;
using Maynard.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Maynard.Web;

public class JwtAuthenticator(IHttpContextAccessor context) : AuthenticationStateProvider
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public TokenInfo _token;

    public TokenInfo Token
    {
        get
        {
            try
            {
                _token ??= context.ValidateJwt();
            }
            catch (Exception e)
            {
                Log.Error("Couldn't load token", e);
            }

            return _token;
        }
        private set => _token = value;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            Token = context.ValidateJwt();
        }
        catch
        {
            Log.Error("Invalid or nonexistent JWT");
        }
        
        return Token?.ToAuthenticationState() 
            ?? new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        // ClaimsPrincipal claims = CurrentToken?.ToClaims() ?? new ClaimsPrincipal(new ClaimsIdentity());
        // return new AuthenticationState(new ClaimsPrincipal(claims));
    }
}