using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Market.API.Application;
using Market.API.Infrastructure;
using Market.API.Persistence;
namespace Market.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMarketApiServices(
       this IServiceCollection services,
       IConfiguration configuration)
        {
            services.AddApplication();
            services.AddPersistence(configuration);
            
            //services.AddInfrastructure(configuration);

            return services;
        }
    }
}