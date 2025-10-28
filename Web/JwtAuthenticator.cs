using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Maynard.Auth;
using Maynard.Extensions;
using Maynard.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;

namespace Maynard.Web;

public class JwtAuthenticator : AuthenticationStateProvider, IDisposable
{
    private const string TokenStateKey = nameof(TokenStateKey);
    private const string HasPersistedStateKey = nameof(HasPersistedStateKey);
    private static readonly AuthenticationState _unauthenticatedState = 
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly PersistentComponentState _persistentState;
    private readonly IHttpContextAccessor _context;
    private readonly IDisposable _persistingSubscription;

    private TokenInfo _token;
    private AuthenticationState _authState;

    public JwtAuthenticator(IHttpContextAccessor context, PersistentComponentState persistentState)
    {
        _context = context;
        _persistentState = persistentState;
        _persistingSubscription = _persistentState.RegisterOnPersisting(PersistTokenAsync, RenderMode.InteractiveServer);
    }

    public TokenInfo Token
    {
        get
        {
            _token ??= FetchToken();
            return _token;
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_authState == null)
        {
            _token ??= FetchToken();
            _authState = _token?.ToAuthenticationState() ?? _unauthenticatedState;
        }
        return Task.FromResult(_authState);
    }

    private TokenInfo FetchToken()
    {
        if (_persistentState.TryTakeFromJson<TokenInfo>(TokenStateKey, out var persistedToken))
        {
            return persistedToken;
        }

        try
        {
            return _context.ValidateJwt();
        }
        catch
        {
            return null;
        }
    }

    private Task PersistTokenAsync()
    {
        // The OnPersisting callback can be triggered multiple times during the initial server render.
        // We use HttpContext.Items to store a flag that is scoped to the specific HTTP request,
        // ensuring we only persist the state once per request.
        if (_context.HttpContext is { } httpContext && !httpContext.Items.ContainsKey(HasPersistedStateKey))
        {
            if (_token is not null)
            {
                _persistentState.PersistAsJson(TokenStateKey, _token);
            }
            httpContext.Items.Add(HasPersistedStateKey, true);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _persistingSubscription.Dispose();
    }
}