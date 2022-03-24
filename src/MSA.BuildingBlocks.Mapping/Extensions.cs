using Microsoft.Extensions.DependencyInjection;

namespace AutoMapper.Extensions.MappingProfile;

public static class Extensions
{
    public static IServiceCollection AddMappingProfiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        return services;
    }
}