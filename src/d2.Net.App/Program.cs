using Auth0.AspNetCore.Authentication;
using d2.Net.App.Components;
using d2.Net.App.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureMiddleware(app);

app.Run();
return;

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddRazorComponents()
        .AddInteractiveServerComponents();

    services.AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = configuration["Auth0:Domain"] ?? throw new InvalidOperationException("Auth0 Domain is not configured.");
        options.ClientId = configuration["Auth0:ClientId"] ?? throw new InvalidOperationException("Auth0 ClientId is not configured.");
    });

    services.AddScoped<d2.Net.D2Wrapper>();
    services.AddScoped<IDiagramService, DiagramService>();
}

void ConfigureMiddleware(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    ConfigureEndpoints(app);
}

void ConfigureEndpoints(WebApplication app)
{
    app.MapGet("/Account/Login", async (HttpContext httpContext, string redirectUri = "/") =>
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(redirectUri)
            .Build();

        await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    });

    app.MapGet("/Account/Logout", async (HttpContext httpContext, string redirectUri = "/") =>
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri(redirectUri)
            .Build();

        await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    });
}
