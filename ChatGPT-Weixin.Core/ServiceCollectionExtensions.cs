using ChatGPT_Weixin.Core.ChatGPT;
using ChatGPT_Weixin.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChatGPT_Weixin.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChatGPT(this IServiceCollection services, string apiKey)
        {
            services.AddSingleton<IChatGPTClient>(x => new ChatGPTClient(apiKey));
            services.AddSingleton<ChatGPTService>();

            return services;
        }
    }
}
