using Market.Client.API.Auth;
using Market.Client.API.Handlers;
using Market.Client.API.Interface.Auth;
using Market.Client.API.Interface.User;
using Market.Client.API.Service.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Market.Client.API;

public static class DependencyInjection
{
    private const int DefaultTimeoutSeconds = 30;

    public static IServiceCollection AddMarketApiClient(this IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("ApiSettings:BaseUrl is not configured for Market.Client.");
        }

        var timeoutSeconds = config.GetValue<int?>("ApiSettings:TimeoutSeconds") ?? DefaultTimeoutSeconds;
        if (timeoutSeconds <= 0)
        {
            timeoutSeconds = DefaultTimeoutSeconds;
        }

        services.AddTransient<AuthHeaderHandler>();
        services.AddHttpClient<IAuthApiClient, AuthApiClient>(c =>
        {
            c.BaseAddress = new Uri(baseUrl);
            c.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient<IUserApiClient, UserApiClient>(c =>
        {
            c.BaseAddress = new Uri(baseUrl);
            c.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(retryAttempt));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
