using Iot.Device.Mcp25xxx.Tests.Register.CanControl;
using Microsoft.Extensions.DependencyInjection;

namespace nanoMeow.Mcp2515.Extensions
{
    public static class Mcp2515Extensions
    {
        public static void AddMcp2515(this IServiceCollection services)
        {
            services.AddSingleton(typeof(Mcp2515Factory));
        }

        public static OperationMode 
            GetMcp2515ModeFromStatus(byte statValue)
        {
            return (OperationMode)((statValue & 0xE0) >> 5);
        }
    }
}
