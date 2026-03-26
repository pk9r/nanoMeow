using System;
using nanoFramework.Hardware.Esp32;
using nanoMeow.Esp32;

namespace nanoMeow.Mcp2515.Esp32.Extensions
{
    public static class Mcp2515Esp32Extensions
    {
        // HSPI("SPI1")
        public static PinSpiMcuMcp2515 PinEsp32Spi1 =
            new() { BusId = 1, CS = Gpio.IO15, SO = Gpio.IO12, SI = Gpio.IO13, SCK = Gpio.IO14 };

        // VSPI("SPI2")
        public static PinSpiMcuMcp2515 PinEsp32Spi2 =
            new() { BusId = 2, CS = Gpio.IO05, SO = Gpio.IO19, SI = Gpio.IO23, SCK = Gpio.IO18 };

        public static void UseMcp2515ForEsp32(
            this Mcp2515Factory mcp2515Factory, 
            Esp32SpiBusIndex spiBusId, int interruptPin = -1
        )
        {
            if (spiBusId == Esp32SpiBusIndex.HSPI)
            {
                mcp2515Factory.PinSpiMcuMcp2515 = PinEsp32Spi1;

                Configuration.SetPinFunction(
                    PinEsp32Spi1.SO, DeviceFunction.SPI1_MISO);
                Configuration.SetPinFunction(
                    PinEsp32Spi1.SI, DeviceFunction.SPI1_MOSI);
                Configuration.SetPinFunction(
                    PinEsp32Spi1.SCK, DeviceFunction.SPI1_CLOCK);
            }
            else if (spiBusId == Esp32SpiBusIndex.VSPI)
            {
                mcp2515Factory.PinSpiMcuMcp2515 = PinEsp32Spi2;

                Configuration.SetPinFunction(
                    PinEsp32Spi2.SO, DeviceFunction.SPI2_MISO);
                Configuration.SetPinFunction(
                    PinEsp32Spi2.SI, DeviceFunction.SPI2_MOSI);
                Configuration.SetPinFunction(
                    PinEsp32Spi2.SCK, DeviceFunction.SPI2_CLOCK);
            }
            else
            {
                throw new InvalidOperationException();
            }

            if (interruptPin != -1)
            {
                mcp2515Factory.PinSpiMcuMcp2515.INT = interruptPin;
            }
        }
    }
}
