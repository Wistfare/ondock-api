using ondock.api.Grpc.Services;

namespace ondock.api.Extensions;

public static class GrpcServiceExtensions
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 50 * 1024 * 1024; // 50MB for media uploads
            options.MaxSendMessageSize = 50 * 1024 * 1024;
        });

        services.AddGrpcReflection();

        return services;
    }

    public static IEndpointRouteBuilder MapGrpcServices(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AuthGrpcService>();
        endpoints.MapGrpcService<ChatGrpcService>();
        endpoints.MapGrpcService<AnnouncementsGrpcService>();
        endpoints.MapGrpcService<MonitoringGrpcService>();
        endpoints.MapGrpcService<NotificationsGrpcService>();
        endpoints.MapGrpcService<CategoriesGrpcService>();
        endpoints.MapGrpcService<ContactsGrpcService>();
        endpoints.MapGrpcService<UsersGrpcService>();
        endpoints.MapGrpcService<IdentityGrpcService>();
        endpoints.MapGrpcService<EncryptionGrpcService>();
        endpoints.MapGrpcService<UserStatusGrpcService>();
        endpoints.MapGrpcService<LoadViewGrpcService>();

        // Enable gRPC reflection for development/testing tools like grpcurl
        endpoints.MapGrpcReflectionService();

        return endpoints;
    }
}
