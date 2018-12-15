using System;
using Microsoft.Extensions.Configuration;

namespace mummybot.Services
{
    public class ConfigService
    {
        public readonly IConfiguration Config;

        public ConfigService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("_config.json");
            Config = builder.Build();
        }
    }
}