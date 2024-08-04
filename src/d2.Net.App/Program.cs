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

    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    ConfigureEndpoints(app);
}

void ConfigureEndpoints(WebApplication app)
{
}
