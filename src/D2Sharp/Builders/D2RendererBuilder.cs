using D2Sharp.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace D2Sharp.Builders;

/// <summary>
/// Default implementation of <see cref="ID2RendererBuilder"/> for configuring D2 renderer services.
/// </summary>
public class D2RendererBuilder : ID2RendererBuilder
{
    private readonly D2SharpOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="D2RendererBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="name">The name of the renderer being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public D2RendererBuilder(IServiceCollection services, string name, D2SharpOptions options)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the name of the renderer being configured.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the service collection that services are added to.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the options instance for configuration. This is used internally by extension methods.
    /// </summary>
    public D2SharpOptions GetOptions() => _options;
}
