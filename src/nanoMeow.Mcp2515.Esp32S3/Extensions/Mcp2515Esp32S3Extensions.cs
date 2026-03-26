using nanoFramework.Hardware.Esp32;
using nanoMeow.Esp32;

namespace nanoMeow.Mcp2515.Esp32.Extensions
{
    public static class Mcp2515Esp32S3Extensions
    {
        // FSPI("SPI2")
        public static PinSpiMcuMcp2515 PinEsp32Spi1 =
            new() { BusId = 2, CS = 34, SO = 37, SI = 35, SCK = 36 };

        // HSPI("SPI3")
        public static PinSpiMcuMcp2515 PinEsp32Spi2 =
            new() { BusId = 2, CS = 10, SO = 13, SI = 11, SCK = 12 };

        public static void UseMcp2515ForEsp32S3(
            this Mcp2515Factory mcp2515Factory, 
            Esp32S3SpiBusIndex spiBusId, int interruptPin = -1
        )
        {
            if (spiBusId == Esp32S3SpiBusIndex.FSPI)
            {
                mcp2515Factory.PinSpiMcuMcp2515 = PinEsp32Spi1;

                Configuration.SetPinFunction(
                   mcp2515Factory.PinSpiMcuMcp2515.SO,
                   DeviceFunction.SPI1_MISO);
                Configuration.SetPinFunction(
                    mcp2515Factory.PinSpiMcuMcp2515.SI,
                    DeviceFunction.SPI1_MOSI);
                Configuration.SetPinFunction(
                    mcp2515Factory.PinSpiMcuMcp2515.SCK,
                    DeviceFunction.SPI1_CLOCK);
            }
            else if (spiBusId == Esp32S3SpiBusIndex.HSPI)
            {
                mcp2515Factory.PinSpiMcuMcp2515 = PinEsp32Spi2;

                Configuration.SetPinFunction(
                    mcp2515Factory.PinSpiMcuMcp2515.SO,
                    DeviceFunction.SPI2_MISO);
                Configuration.SetPinFunction(
                    mcp2515Factory.PinSpiMcuMcp2515.SI,
                    DeviceFunction.SPI2_MOSI);
                Configuration.SetPinFunction(
                    mcp2515Factory.PinSpiMcuMcp2515.SCK,
                    DeviceFunction.SPI2_CLOCK);
            }
            else
            {
                throw new System.InvalidOperationException();
            }

            if (interruptPin != -1)
            {
                mcp2515Factory.PinSpiMcuMcp2515.INT = interruptPin;
            }
        }
    }
}
