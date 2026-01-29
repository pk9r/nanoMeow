using nanoMeow.Mcp2515.Services;
using System.Device.Gpio;

namespace nanoMeow.Mcp2515
{
    public class Mcp2515Factory
    {
        public PinSpiMcuMcp2515 PinSpiMcuMcp2515 { get; set; }

        public GpioController GpioController { get; set; }

        public Mcp2515Factory(GpioController gpioController)
        {
            GpioController = gpioController; 
        }

        public Mcp2515Service CreateService()
        {
            return new Mcp2515Service(PinSpiMcuMcp2515, GpioController);
        }
    }
}
