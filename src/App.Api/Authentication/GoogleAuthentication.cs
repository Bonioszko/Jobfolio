using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace App.Api.Authentication;

internal static class GoogleAuthentication
{
    public const string Scheme = "GoogleOpenIdConnect";
    public const string CallbackPath = "/signin-google";

    public static AuthenticationBuilder AddGoogleOpenIdConnect(
        this AuthenticationBuilder authentication,
        GoogleAuthenticationSettings settings)
    {
        if (!settings.Enabled)
        {
            return authentication;
        }

        return authentication.AddOpenIdConnect(Scheme, options =>
        {
            options.Authority = "https://accounts.google.com";
            options.ClientId = settings.ClientId;
            options.ClientSecret = settings.ClientSecret;
            options.CallbackPath = CallbackPath;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.ResponseType = "code";
            options.UsePkce = true;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.MapInboundClaims = false;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.Prompt = "select_account";
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var principal = context.Principal is null
                        ? null
                        : GoogleClaimsPrincipalFactory.Create(context.Principal, settings);
                    if (principal is null)
                    {
                        context.Fail("The Google identity is not verified or allowlisted.");
                    }
                    else
                    {
                        context.Principal = principal;
                    }

                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    context.Response.Redirect(settings.FrontendRedirect("sign-in-failed"));
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
    }
}
