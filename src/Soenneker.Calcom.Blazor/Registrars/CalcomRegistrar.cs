using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Calcom.Blazor.Abstract;
using Soenneker.Calcom.Blazor.Configuration;
using Soenneker.Blazor.Utils.ResourceLoader.Registrars;
namespace Soenneker.Calcom.Blazor.Registrars;
/// <summary>Registers Cal.com browser services.</summary>
public static class CalcomRegistrar
{
    /// <summary>Adds scoped Cal.com services with optional self-hosted instance configuration.</summary>
    public static IServiceCollection AddCalcomAsScoped(this IServiceCollection services, Action<CalcomConfiguration>? configure = null)
    {
        services.AddResourceLoaderAsScoped();
        services.TryAddScoped(_ =>
        {
            var configuration = new CalcomConfiguration();
            configure?.Invoke(configuration);
            return configuration;
        });
        services.TryAddScoped<ICalcom, Calcom>();
        return services;
    }
}
