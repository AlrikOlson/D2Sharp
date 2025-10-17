using Microsoft.Extensions.DependencyInjection;

namespace D2Sharp;

/// <summary>
/// Defines a builder interface for configuring D2 diagram renderer services.
/// Follows the Microsoft.Extensions pattern used by HttpClientBuilder, HostBuilder, etc.
/// </summary>
public interface ID2RendererBuilder
{
    /// <summary>
    /// Gets the name of the renderer being configured.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the service collection that services are added to.
    /// </summary>
    IServiceCollection Services { get; }
}
