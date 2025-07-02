using Common.Application.Interfaces;
using Common.Infrastructure.FileStatusStore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering common application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers common services to the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<IFileStatusStore, InMemoryFileStatusStore>();
        return services;
    }
}

