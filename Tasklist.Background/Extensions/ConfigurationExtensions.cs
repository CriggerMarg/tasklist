
using Microsoft.Extensions.Configuration;

namespace Tasklist.Background.Extensions
{
    public static class ConfigurationExtensions
    {
        public static int ReadIntConfigValue(this IConfiguration configuration, string key, int defaultValue)
        {
            var value = configuration[key];
            if (value == null)
            {
                return defaultValue;
            }
            int number;
            if(int.TryParse(value, out number))
            {
                return number;
            }
            return defaultValue;
        }

    }
}
