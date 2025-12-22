using EntityResolutionSource.Senzing.Impl;
using EntityResolutionSource.Senzing.Interfaces;
using EntityResolutionSource.Senzing.Mapping;
using EntityResolutionSource.Senzing.Mapping.Strategies;
using Infrastructure.Impl;
using EntityResolutionSource.Senzing.Impl;
using EntityResolutionSource.Senzing.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityResolutionSource.Senzing.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSenzingEntityResolutionServices(
        this IServiceCollection services
    )
    {
        // Ontology to Senzing mappers
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAttributeMappingStrategy, AddressMappingStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAttributeMappingStrategy, NameMappingStrategy>());
        // services.TryAddTransient<IAttributeMappingStrategy, AddressMappingStrategy>();
        services.TryAddTransient<IEntityResolutionService, EntityResolutionService>();
        services.TryAddTransient<SenzingEntityAdapter>();

        // Data infrastructure
        services.TryAddTransient<IEntityStateRepository, EntityStateRepository>();
        services.TryAddTransient<IStableIdRepository, StableIdRepository>();
        services.TryAddTransient<IStableIdService, StableIdService>();
        services.TryAddTransient<StableIdService>();

        // Core services
        services.TryAddSingleton<ISzEnvironmentWrapper, SzEnvironmentWrapper>();
        services.TryAddTransient<IEntityResolutionEngineEventProcessor, SenzingEventProcessor>();
        services.TryAddTransient<IEntityResolutionSource, SenzingEntityResolutionSource>();

        return services;
    }
}
