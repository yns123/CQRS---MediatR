using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TicketReservation.Common.Abstractions;

/// <summary>Her slice kendi endpoint'ini bu arayüzle tanımlar; Program.cs hiçbirini tek tek bilmez.</summary>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEndpoint), type));

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
            endpoint.Map(app);

        return app;
    }
}
