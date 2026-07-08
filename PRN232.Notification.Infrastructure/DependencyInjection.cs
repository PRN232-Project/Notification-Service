using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace PRN232.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHostedService<Messaging.NotificationIntegrationConsumer>();
        return services;
    }
}
