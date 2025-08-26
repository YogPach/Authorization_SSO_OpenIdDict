using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Web;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Controllers
{
    [ApiController]
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly AuthorizationService _authService;
        private readonly IServiceProvider _serviceProvider;

        public AuthorizationController(
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictScopeManager scopeManager,
            AuthorizationService authService,
            IServiceProvider serviceProvider)
        {
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
            _authService = authService;
            _serviceProvider = serviceProvider; 
        }


        //[HttpGet("~/connect/authorize")]
        //[HttpPost("~/connect/authorize")]
        //public async Task<IActionResult> Authorize()
        //{
        //    var request = HttpContext.GetOpenIddictServerRequest() ??
        //                  throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        //    var parameters = _authService.ParseOAuthParameters(HttpContext, new List<string> { Parameters.Prompt });

        //    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        //    if (!_authService.IsAuthenticated(result, request))
        //    {
        //        return Challenge(properties: new AuthenticationProperties
        //        {
        //            RedirectUri = _authService.BuildRedirectUrl(HttpContext.Request, parameters)
        //        }, new[] { CookieAuthenticationDefaults.AuthenticationScheme });
        //    }

        //    var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
        //                      throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        //    var consentType = await _applicationManager.GetConsentTypeAsync(application);

        //    // we just ignore other consent types, because they are not compliant with OAuth and OpenId Connect docs, that state that Resource Owner should grant the Client access
        //    // you might also support Implicit ConsentType - where you do not require consent screen even if `prompt=consent` provided. In that case just drop this if.
        //    // you might want to support External ConsentType - where you need to get created authorization first by admin to be able to log in.
        //    if (consentType != ConsentTypes.Explicit)
        //    {
        //        return Forbid(
        //            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        //            properties: new AuthenticationProperties(new Dictionary<string, string?>
        //            {
        //                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
        //                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
        //                    "Only explicit consent clients are supported"
        //            }));
        //    }

        //    var consentClaim = result.Principal.GetClaim(Consts.ConsentNaming);

        //    // it might be extended in a way that consent claim will contain list of allowed client ids.
        //    if (consentClaim != Consts.GrantAccessValue)
        //    {
        //        var returnUrl = HttpUtility.UrlEncode(_authService.BuildRedirectUrl(HttpContext.Request, parameters));
        //        var consentRedirectUrl = $"/Consent?returnUrl={returnUrl}";

        //        return Redirect(consentRedirectUrl);
        //    }

        //    var userId = result.Principal.FindFirst(ClaimTypes.Email)!.Value;

        //    var identity = new ClaimsIdentity(
        //        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        //        nameType: Claims.Name,
        //        roleType: Claims.Role);

        //    identity.SetClaim(Claims.Subject, userId)
        //        .SetClaim(Claims.Email, userId)
        //        .SetClaim(Claims.Name, userId)
        //        .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

        //    identity.SetScopes(request.GetScopes());
        //    identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        //    identity.SetDestinations(c => AuthorizationService.GetDestinations(identity, c));

        //    return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        //}

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!_authService.IsAuthenticated(result, request))
            {
                // Return 401 instead of redirecting to login page
                return Unauthorized(new { error = "login_required", message = "User authentication required" });
            }

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                              throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

            var consentType = await _applicationManager.GetConsentTypeAsync(application);

            if (consentType != ConsentTypes.Explicit)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Only explicit consent clients are supported"
                    }));
            }

            var consentClaim = result.Principal.FindFirst(Consts.ConsentNaming)?.Value;

            if (consentClaim != Consts.GrantAccessValue)
            {
                // Return consent required response instead of redirecting
                return Unauthorized(new
                {
                    error = "consent_required",
                    message = "User consent required",
                    client_id = request.ClientId,
                    scope = request.Scope
                });
            }

            var userId = result.Principal.FindFirst(ClaimTypes.Email)!.Value;

            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, userId)
                .SetClaim(Claims.Email, userId)
                .SetClaim(Claims.Name, userId)
                .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

            identity.SetScopes(request.GetScopes());
            identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());
            identity.SetDestinations(c => AuthorizationService.GetDestinations(identity, c));

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
                throw new InvalidOperationException("The specified grant type is not supported.");

            var result =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = result.Principal.GetClaim(Claims.Subject);

            if (string.IsNullOrEmpty(userId))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Cannot find user from the token."
                    }));
            }

            var identity = new ClaimsIdentity(result.Principal.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, userId)
                .SetClaim(Claims.Email, userId)
                .SetClaim(Claims.Name, userId)
                .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

            identity.SetDestinations(c => AuthorizationService.GetDestinations(identity, c));

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
       

        // Controllers/AuthorizationController.cs
        [HttpGet("~/connect/logout")]
        [HttpPost("~/connect/logout")]
        public async Task<IActionResult> LogoutPost()
        {
            // Sign out from both authentication schemes
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = "/"
                });
        }
    }
}
