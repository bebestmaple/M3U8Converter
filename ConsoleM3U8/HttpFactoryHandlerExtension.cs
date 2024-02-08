using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleM3U8
{
    public static class HttpFactoryHandlerExtension
    {
        public static IServiceCollection AddHttpFactoryHandler(this IServiceCollection services)
        {
            services.AddHttpClient(HttpFactoryHandler._HttpClientName).ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                };

            });

            services.TryAddSingleton<HttpFactoryHandler>();

            return services;
        }
    }
}