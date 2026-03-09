// Links:
// - https://github.com/autowp/arduino-mcp2515/blob/86fd56e54266defda9600efebc76f506e24c1cc1/mcp2515.cpp


using Iot.Device.Mcp25xxx;
using Iot.Device.Mcp25xxx.Register;
using Iot.Device.Mcp25xxx.Register.CanControl;
using Iot.Device.Mcp25xxx.Tests.Register.CanControl;
using Microsoft.Extensions.Logging;
using nanoFramework.Logging;
using nanoMeow.CanBus;
using nanoMeow.CanBus.Enums;
using nanoMeow.Mcp2515.Abstractions;
using nanoMeow.Mcp2515.Enums;
using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;


namespace nanoMeow.Mcp2515.Services
{

    public class Mcp2515Service : IDisposable
    {
        const byte CANCTRL_REQOP = 0xE0;
        const byte CANCTRL_OSM = 0x08;

        const byte CANSTAT_OPMOD = 0b11100000;

        private readonly ILogger _logger;

        public ISpiMcuService SpiMcuService { get; set; }

        public SpiDevice SpiDevice { get; set; }

        public Iot.Device.Mcp25xxx.Mcp2515 Mcp2515 { get; set; }

        public PinSpiMcuMcp2515 PinSpiMcuMcp2515 { get; }

        public GpioController GpioController { get; }

        public GpioPin Interrupt { get; set; } // Mcp25xxx bad open interrupt

        public Mcp2515Service(
            PinSpiMcuMcp2515 pinSpiMcuMcp2515,
            GpioController gpioController
        )
        {
            _logger = this.GetCurrentClassLogger();

            PinSpiMcuMcp2515 = pinSpiMcuMcp2515;
            GpioController = gpioController;

            var spiConnectionSettings = new SpiConnectionSettings(
                busId: PinSpiMcuMcp2515.BusId,
                chipSelectLine: PinSpiMcuMcp2515.CS
            )
            {
                DataFlow = DataFlow.MsbFirst,
                Configuration = SpiBusConfiguration.FullDuplex,
                ClockFrequency = 10_000_000,
            };
            SpiDevice = new SpiDevice(spiConnectionSettings);

            Mcp2515 = new Iot.Device.Mcp25xxx.Mcp2515(
                spiDevice: SpiDevice,
                gpioController: GpioController
            );

            var interruptPin = PinSpiMcuMcp2515.INT;
            if (interruptPin != -1)
            {
                _logger.LogInformation(
                    "Open pin {0} as interrupt pin", interruptPin
                );
                Interrupt = GpioController.OpenPin(
                    pinNumber: interruptPin,
                    mode: PinMode.Input
                );
                Interrupt.ValueChanged += HandleInterrupt;
            }
        }

        private void HandleInterrupt(
            object sender,
            PinValueChangedEventArgs e
        )
        {
            _logger.LogInformation(
                "Interrupt.ValueChanged {0}", sender
            );
        }

        //public void Reset()
        //{
        //    Mcp2515.Reset();

        //    Thread.Sleep(20); // Wait for reset to complete

        //    //Mcp2515.WriteByte(new CanIntE(
        //    //    receiveBuffer0FullInterruptEnable: false,
        //    //    receiveBuffer1FullInterruptEnable: false,
        //    //    transmitBuffer0EmptyInterruptEnable: false,
        //    //    transmitBuffer1EmptyInterruptEnable: false,
        //    //    transmitBuffer2EmptyInterruptEnable: false,
        //    //    errorInterruptEnable: true,
        //    //    wakeUpInterruptEnable: false,
        //    //    messageErrorInterruptEnable: true
        //    //));

        //    Mcp2515.WriteByte(new RxB0Ctrl(
        //        filterHit: false, // mcp2515 will set correct filter hit bit in status register, so we can ignore it here
        //        rolloverEnable: true,
        //        receivedRemoteTransferRequest: true,
        //        receiveBufferOperatingMode: OperatingMode.ReceiveAllValidMessages
        //    ));

        //    Mcp2515.WriteByte(new RxB1Ctrl(
        //        filterHit: RxB1Ctrl.Filter.Filter2, // mcp2515 will set correct filter hit bit in status register, so we can ignore it here
        //        receivedRemoteTransferRequest: true,
        //        receiveBufferOperatingMode: OperatingMode.ReceiveAllValidMessages
        //    ));

        //    // clear filters and masks
        //    // do not filter any standard frames for RXF0 used by RXB0
        //    // do not filter any extended frames for RXF1 used by RXB1
        //    //var rxfs = new Mcp2515Rxf[]
        //    //{
        //    //    Mcp2515Rxf.RXF0, Mcp2515Rxf.RXF1, Mcp2515Rxf.RXF2,
        //    //    Mcp2515Rxf.RXF3, Mcp2515Rxf.RXF4, Mcp2515Rxf.RXF5
        //    //};
        //    //for (int i = 0; i < rxfs.Length; i++)
        //    //{
        //    //    var ext = (i == 1);
        //    //    SetFilter(rxfs[i], ext, 0);
        //    //}

        //    //var masks = new Mcp2515Mask[]
        //    //{
        //    //    Mcp2515Mask.MASK0, Mcp2515Mask.MASK1
        //    //};
        //    //for (int i = 0; i < masks.Length; i++)
        //    //{
        //    //    SetFilterMask(masks[i], true, 0);
        //    //}
        //}

        private const int CAN_MAX_DLEN = 8;

        /* special address description flags for the CAN_ID */
        //const ulong CAN_EFF_FLAG = 0x80000000UL; /* EFF/SFF is set in the MSB */
        //const ulong CAN_RTR_FLAG = 0x40000000UL; /* remote transmission request */
        //const ulong CAN_ERR_FLAG = 0x20000000UL; /* error message frame */

        /* valid bits in CAN ID for frame formats */
        //const ulong CAN_SFF_MASK = 0x000007FFUL; /* standard frame format (SFF) */
        //const ulong CAN_EFF_MASK = 0x1FFFFFFFUL; /* extended frame format (EFF) */
        //const ulong CAN_ERR_MASK = 0x1FFFFFFFUL; /* omit EFF, RTR, ERR flags */

        private readonly Mcp2515TXBnRegs[] TXB = new Mcp2515TXBnRegs[]
        {
            new() { Ctrl = Address.TxB0Ctrl, Sidh = Address.TxB0Sidh, Data = Address.TxB0D0},
            new() { Ctrl = Address.TxB1Ctrl, Sidh = Address.TxB1Sidh, Data = Address.TxB1D0},
            new() { Ctrl = Address.TxB2Ctrl, Sidh = Address.TxB2Sidh, Data = Address.TxB2D0}
        };

        private readonly Mcp2515RXBnRegs[] RXB = new Mcp2515RXBnRegs[]
        {
            new()
            {
                Ctrl = Address.RxB0Ctrl,
                Sidh = RxBufferAddressPointer.RxB0Sidh,
                Data = RxBufferAddressPointer.RxB0D0,
                RXnIF = Mcp2515CanIntF.RX0IF
            },
            new()
            {
                Ctrl = Address.RxB1Ctrl,
                Sidh = RxBufferAddressPointer.RxB1Sidh,
                Data = RxBufferAddressPointer.RxB1D0,
                RXnIF = Mcp2515CanIntF.RX1IF
            }
        };

        const byte TXB_TXREQ = 0x08;


        public void SendMessage(CanFrame frame)
        {
            if (frame.CanDlc > CAN_MAX_DLEN)
            {
                throw new InvalidOperationException(
                    message: "Data length exceeds maximum CAN frame size"
                );
            }

            var txBuffers = new Mcp2515TXBn[]
            {
                Mcp2515TXBn.TXB0, Mcp2515TXBn.TXB1, Mcp2515TXBn.TXB2
            };

            for (int i = 0; i < txBuffers.Length; i++)
            {
                var txbuf = TXB[(byte)txBuffers[i]];

                byte ctrlval = Mcp2515.Read(txbuf.Ctrl);
                if ((ctrlval & TXB_TXREQ) == 0)
                {
                    SendMessage(txBuffers[i], frame);
                    return;
                }
            }

            throw new Exception(
                message: "All transmit buffers are full"
            );
        }

        private void SendMessage(Mcp2515TXBn txbn, CanFrame frame)
        {
            if (frame.CanDlc > CAN_MAX_DLEN)
            {
                throw new InvalidOperationException(
                    message: "Data length exceeds maximum CAN frame size"
                );
            }

            var txbuf = TXB[(byte)txbn];

            var data = new SpanByte(new byte[13]);

            bool ext = (frame.CanId & (ulong)CanIdFlag.EFF) != 0;
            bool rtr = (frame.CanId & (ulong)CanIdFlag.RTR) != 0;
            uint id = (uint)(frame.CanId & (ext ? (ulong)CanIdMask.EFF : (ulong)CanIdMask.SFF));

            PrepareId(data, ext, id);

            data[MCP_DLC] = (byte)(rtr ? (frame.CanDlc | RTR_MASK) : frame.CanDlc);

            var source = new SpanByte(frame.Data);
            var destination = data.Slice(MCP_DATA, frame.CanDlc);
            source.CopyTo(destination);

            Mcp2515.Write(txbuf.Sidh, data.Slice(0, 5 + frame.CanDlc));

            Mcp2515.BitModify(
                address: txbuf.Ctrl,
                mask: TXB_TXREQ,
                value: TXB_TXREQ
            );

            byte ctrl = Mcp2515.Read(txbuf.Ctrl);
            if ((ctrl & (byte)(Mcp2515TXBnCtrl.ABTF | Mcp2515TXBnCtrl.MLOA | Mcp2515TXBnCtrl.TXERR)) != 0)
            {
                throw new Exception(
                    message: "Error occurred during CAN message transmission"
                );
            }
        }

        private CanFrame ReadMessage(Mcp2515RXBn rxbn)
        {
            var frame = new CanFrame();

            var rxb = RXB[(byte)rxbn];

            var tbufdata = Mcp2515.ReadRxBuffer(rxb.Sidh, byteCount: 5);

            uint id = (uint)((tbufdata[MCP_SIDH] << 3) + (tbufdata[MCP_SIDL] >> 5));

            if ((tbufdata[MCP_SIDL] & TXB_EXIDE_MASK) == TXB_EXIDE_MASK)
            {
                id = (uint)((id << 2) + (tbufdata[MCP_SIDL] & 0x03));
                id = (id << 8) + tbufdata[MCP_EID8];
                id = (id << 8) + tbufdata[MCP_EID0];
                id |= (uint)(ulong)CanIdFlag.EFF;
            }

            byte dlc = (byte)(tbufdata[MCP_DLC] & DLC_MASK);
            if (dlc > CAN_MAX_DLEN)
            {
                throw new Exception(
                    message: "Data length exceeds maximum CAN frame size"
                );
            }

            uint ctrl = Mcp2515.Read(rxb.Ctrl);
            if ((ctrl & (uint)Mcp2515RXBnCtrl.RTR) != 0)
            {
                id |= (uint)(ulong)CanIdFlag.RTR;
            }

            frame.CanId = id;
            frame.CanDlc = dlc;
            frame.Data = Mcp2515.ReadRxBuffer(rxb.Data, dlc);

            Mcp2515.BitModify(Address.CanIntF, (byte)rxb.RXnIF, 0);

            return frame;
        }

        public CanFrame ReadMessage()
        {
            var stat = Mcp2515.ReadStatus();

            if (stat.HasFlag(ReadStatusResponse.Rx0If))
                return ReadMessage(Mcp2515RXBn.RXB0);
            if (stat.HasFlag(ReadStatusResponse.Rx1If))
                return ReadMessage(Mcp2515RXBn.RXB1);
            
            return null;
        }

        public bool TryReadMessage(out CanFrame frame)
        {
            try
            {
                frame = ReadMessage();
            }
            catch (Exception)
            {
                frame = null;
                return false;
            }

            return true;
        }

        public void SetFilter(
            Mcp2515Rxf filter,
            bool ext, uint ulData)
        {
            SetConfigMode();

            var reg = filter switch
            {
                Mcp2515Rxf.RXF0 => Address.RxF0Sidh,
                Mcp2515Rxf.RXF1 => Address.RxF1Sidh,
                Mcp2515Rxf.RXF2 => Address.RxF2Sidh,
                Mcp2515Rxf.RXF3 => Address.RxF3Sidh,
                Mcp2515Rxf.RXF4 => Address.RxF4Sidh,
                Mcp2515Rxf.RXF5 => Address.RxF5Sidh,
                _ => throw new InvalidOperationException(
                    message: "Invalid filter."),
            };

            var tbufdata = new SpanByte(new byte[4]);
            PrepareId(tbufdata, ext, ulData);
            Mcp2515.Write(reg, tbufdata);
        }

        public void SetStandardFilter(
            byte rxFilterNumber,
            ushort standardIdentifierFilter
        )
        {
            var reg = rxFilterNumber switch
            {
                0 => Address.RxF0Sidh,
                1 => Address.RxF1Sidh,
                2 => Address.RxF2Sidh,
                3 => Address.RxF3Sidh,
                4 => Address.RxF4Sidh,
                5 => Address.RxF5Sidh,
                _ => throw new InvalidOperationException(
                    message: "Invalid filter number."
                ),
            };

            var tbufdata = new SpanByte(new byte[4]);
            tbufdata[0] = (byte)(standardIdentifierFilter >> 3);
            tbufdata[1] = (byte)((standardIdentifierFilter & 0x07) << 5);

            Mcp2515.Write(reg, tbufdata);
        }

        public void SetStandardFilterMask(
            byte rxMaskNumber,
            ushort standardIdentifierMask)
        {
            var reg = rxMaskNumber switch
            {
                0 => Address.RxM0Sidh,
                1 => Address.RxM1Sidh,
                _ => throw new InvalidOperationException(
                    message: "Invalid mask number."
                ),
            };
            var tbufdata = new SpanByte(new byte[4]);

            tbufdata[0] = (byte)(standardIdentifierMask >> 3);
            tbufdata[1] = (byte)((standardIdentifierMask & 0x07) << 5);

            Mcp2515.Write(reg, tbufdata);
        }

        //public void SetFilterMask(
        //    Mcp2515Mask mask,
        //    bool ext, uint ulData)
        //{
        //    SetConfigMode();

        //    var reg = mask switch
        //    {
        //        Mcp2515Mask.MASK0 => Address.RxM0Sidh,
        //        Mcp2515Mask.MASK1 => Address.RxM1Sidh,
        //        _ => throw new Exception("Invalid mask number"),
        //    };

        //    var tbufdata = new SpanByte(new byte[4]);
        //    PrepareId(tbufdata, ext, ulData);
        //    Mcp2515.Write(reg, tbufdata);
        //}

        const byte TXB_EXIDE_MASK = 0x08;
        const byte DLC_MASK = 0x0F;
        const byte RTR_MASK = 0x40;

        const byte MCP_SIDH = 0;
        const byte MCP_SIDL = 1;
        const byte MCP_EID8 = 2;
        const byte MCP_EID0 = 3;
        const byte MCP_DLC = 4;
        const byte MCP_DATA = 5;

        private void PrepareId(SpanByte buffer, bool ext, uint id)
        {
            var canid = (ushort)(id & 0xFFFF);

            if (ext)
            {
                buffer[MCP_EID0] = (byte)(canid & 0xFF);
                buffer[MCP_EID8] = (byte)(canid >> 8);
                canid = (ushort)(canid >> 16);
                buffer[MCP_SIDL] = (byte)(canid & 0x03);
                buffer[MCP_SIDL] += (byte)((canid & 0x1C) << 3);
                buffer[MCP_SIDL] |= TXB_EXIDE_MASK;
                buffer[MCP_SIDH] = (byte)(canid >> 5);
            }
            else
            {
                buffer[MCP_SIDH] = (byte)(canid >> 3);
                buffer[MCP_SIDL] = (byte)((canid & 0x07) << 5);
                buffer[MCP_EID0] = 0;
                buffer[MCP_EID8] = 0;
            }
        }

        public void SetMode(CanCtrl canCtrl, int timeoutMilis = 100)
        {
            Mcp2515.WriteByte(canCtrl);
            Thread.Sleep(20);

            var mode = canCtrl.RequestOperationMode;
            _logger.LogInformation(
                "Requesting MCP2515 to enter {0} mode",
                mode
            );

            var timeoutTicks = DateTime.UtcNow.Ticks +
                timeoutMilis * TimeSpan.TicksPerMillisecond;

            var modeMatch = false;
            while (DateTime.UtcNow.Ticks < timeoutTicks)
            {
                modeMatch = IsInMode(mode);
                if (modeMatch) break;

                _logger.LogInformation(
                    "Mode not yet set, sleeping for 20ms..."
                );
                Thread.Sleep(20);
            }

            if (!modeMatch)
            {
                //throw new TimeoutException(
                //    message: "Failed to enter requested mode"
                //);

                _logger.LogInformation("Failed to enter requested mode");
                _logger.LogInformation("Sleeping indefinitely...");
                Thread.Sleep(Timeout.Infinite);
            }

            _logger.LogInformation(
                "MCP2515 mode set to {0} mode",
                mode
            );
        }

        private void SetMode(
            OperationMode mode,
            int timeoutMilis = 100
        )
        {
            if (IsInMode(mode))
                return;

            var timeoutTicks = DateTime.UtcNow.Ticks +
                timeoutMilis * TimeSpan.TicksPerMillisecond;

            Mcp2515.BitModify(
                address: Address.CanCtrl,
                mask: CANCTRL_REQOP | CANCTRL_OSM,
                value: (byte)mode
            );

            var modeMatch = false;
            while (DateTime.UtcNow.Ticks < timeoutTicks)
            {
                modeMatch = IsInMode(mode);
                if (modeMatch) break;
            }

            if (!modeMatch)
            {
                throw new TimeoutException(
                    message: "Failed to enter requested mode"
                );
            }

            _logger.LogInformation(
                "MCP2515 mode set to {0} mode", mode
            );
        }

        private bool IsInMode(OperationMode mode)
        {
            var currentMode = Mcp2515.Read(Address.CanStat);
            currentMode &= CANSTAT_OPMOD;
            return currentMode == (byte)mode << 5;
        }

        public void SetNormalMode(int timeoutMilis = 100)
        {
            SetMode(OperationMode.NormalOperation, timeoutMilis);
        }

        public void SetConfigMode(int timeoutMilis = 100)
        {
            SetMode(OperationMode.Configuration, timeoutMilis);
        }

        public void SetLoopbackMode(int timeoutMilis = 100)
        {
            SetMode(OperationMode.Loopback, timeoutMilis);
        }

        public void Dispose()
        {
            Mcp2515?.Dispose();
            Mcp2515 = null;
        }

        public void SetBitrate(
            CanSpeed canSpeed,
            Mcp2515Clock mcp2515Clock,
            int timeoutMilis = 100)
        {
            SetConfigMode(timeoutMilis);

            byte set = 1, cfg1 = 0, cfg2 = 0, cfg3 = 0;
            switch (mcp2515Clock)
            {
                case Mcp2515Clock.MCP_20MHZ:
                    switch (canSpeed)
                    {
                        case CanSpeed.CAN_33KBPS:
                            cfg1 = MCP_20MHz_33k3BPS_CFG1;
                            cfg2 = MCP_20MHz_33k3BPS_CFG2;
                            cfg3 = MCP_20MHz_33k3BPS_CFG3;

                            break;
                        case CanSpeed.CAN_40KBPS:
                            cfg1 = MCP_20MHz_40kBPS_CFG1;
                            cfg2 = MCP_20MHz_40kBPS_CFG2;
                            cfg3 = MCP_20MHz_40kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_50KBPS:
                            cfg1 = MCP_20MHz_50kBPS_CFG1;
                            cfg2 = MCP_20MHz_50kBPS_CFG2;
                            cfg3 = MCP_20MHz_50kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_80KBPS:
                            cfg1 = MCP_20MHz_80kBPS_CFG1;
                            cfg2 = MCP_20MHz_80kBPS_CFG2;
                            cfg3 = MCP_20MHz_80kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_83K3BPS:
                            cfg1 = MCP_20MHz_83k3BPS_CFG1;
                            cfg2 = MCP_20MHz_83k3BPS_CFG2;
                            cfg3 = MCP_20MHz_83k3BPS_CFG3;

                            break;
                        case CanSpeed.CAN_100KBPS:
                            cfg1 = MCP_20MHz_100kBPS_CFG1;
                            cfg2 = MCP_20MHz_100kBPS_CFG2;
                            cfg3 = MCP_20MHz_100kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_125KBPS:
                            cfg1 = MCP_20MHz_125kBPS_CFG1;
                            cfg2 = MCP_20MHz_125kBPS_CFG2;
                            cfg3 = MCP_20MHz_125kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_200KBPS:
                            cfg1 = MCP_20MHz_200kBPS_CFG1;
                            cfg2 = MCP_20MHz_200kBPS_CFG2;
                            cfg3 = MCP_20MHz_200kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_250KBPS:
                            cfg1 = MCP_20MHz_250kBPS_CFG1;
                            cfg2 = MCP_20MHz_250kBPS_CFG2;
                            cfg3 = MCP_20MHz_250kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_500KBPS:
                            cfg1 = MCP_20MHz_500kBPS_CFG1;
                            cfg2 = MCP_20MHz_500kBPS_CFG2;
                            cfg3 = MCP_20MHz_500kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_1000KBPS:
                            cfg1 = MCP_20MHz_1000kBPS_CFG1;
                            cfg2 = MCP_20MHz_1000kBPS_CFG2;
                            cfg3 = MCP_20MHz_1000kBPS_CFG3;
                            break;
                        default:
                            set = 0;
                            break;
                    }
                    break;
                case Mcp2515Clock.MCP_16MHZ:
                    switch (canSpeed)
                    {
                        case CanSpeed.CAN_5KBPS:
                            cfg1 = MCP_16MHz_5kBPS_CFG1;
                            cfg2 = MCP_16MHz_5kBPS_CFG2;
                            cfg3 = MCP_16MHz_5kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_10KBPS:
                            cfg1 = MCP_16MHz_10kBPS_CFG1;
                            cfg2 = MCP_16MHz_10kBPS_CFG2;
                            cfg3 = MCP_16MHz_10kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_20KBPS:
                            cfg1 = MCP_16MHz_20kBPS_CFG1;
                            cfg2 = MCP_16MHz_20kBPS_CFG2;
                            cfg3 = MCP_16MHz_20kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_40KBPS:
                            cfg1 = MCP_16MHz_40kBPS_CFG1;
                            cfg2 = MCP_16MHz_40kBPS_CFG2;
                            cfg3 = MCP_16MHz_40kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_50KBPS:
                            cfg1 = MCP_16MHz_50kBPS_CFG1;
                            cfg2 = MCP_16MHz_50kBPS_CFG2;
                            cfg3 = MCP_16MHz_50kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_80KBPS:
                            cfg1 = MCP_16MHz_80kBPS_CFG1;
                            cfg2 = MCP_16MHz_80kBPS_CFG2;
                            cfg3 = MCP_16MHz_80kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_83K3BPS:
                            cfg1 = MCP_16MHz_83k3BPS_CFG1;
                            cfg2 = MCP_16MHz_83k3BPS_CFG2;
                            cfg3 = MCP_16MHz_83k3BPS_CFG3;

                            break;
                        case CanSpeed.CAN_95KBPS:
                            cfg1 = MCP_16MHz_95kBPS_CFG1;
                            cfg2 = MCP_16MHz_95kBPS_CFG2;
                            cfg3 = MCP_16MHz_95kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_100KBPS:
                            cfg1 = MCP_16MHz_100kBPS_CFG1;
                            cfg2 = MCP_16MHz_100kBPS_CFG2;
                            cfg3 = MCP_16MHz_100kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_125KBPS:
                            cfg1 = MCP_16MHz_125kBPS_CFG1;
                            cfg2 = MCP_16MHz_125kBPS_CFG2;
                            cfg3 = MCP_16MHz_125kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_200KBPS:
                            cfg1 = MCP_16MHz_200kBPS_CFG1;
                            cfg2 = MCP_16MHz_200kBPS_CFG2;
                            cfg3 = MCP_16MHz_200kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_250KBPS:
                            cfg1 = MCP_16MHz_250kBPS_CFG1;
                            cfg2 = MCP_16MHz_250kBPS_CFG2;
                            cfg3 = MCP_16MHz_250kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_500KBPS:
                            cfg1 = MCP_16MHz_500kBPS_CFG1;
                            cfg2 = MCP_16MHz_500kBPS_CFG2;
                            cfg3 = MCP_16MHz_500kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_1000KBPS:
                            cfg1 = MCP_16MHz_1000kBPS_CFG1;
                            cfg2 = MCP_16MHz_1000kBPS_CFG2;
                            cfg3 = MCP_16MHz_1000kBPS_CFG3;

                            break;
                        default:
                            set = 0;
                            break;
                    }
                    break;
                case Mcp2515Clock.MCP_8MHZ:
                    switch (canSpeed)
                    {
                        case CanSpeed.CAN_5KBPS:
                            cfg1 = MCP_8MHz_5kBPS_CFG1;
                            cfg2 = MCP_8MHz_5kBPS_CFG2;
                            cfg3 = MCP_8MHz_5kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_10KBPS:
                            cfg1 = MCP_8MHz_10kBPS_CFG1;
                            cfg2 = MCP_8MHz_10kBPS_CFG2;
                            cfg3 = MCP_8MHz_10kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_20KBPS:
                            cfg1 = MCP_8MHz_20kBPS_CFG1;
                            cfg2 = MCP_8MHz_20kBPS_CFG2;
                            cfg3 = MCP_8MHz_20kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_31K25BPS:
                            cfg1 = MCP_8MHz_31k25BPS_CFG1;
                            cfg2 = MCP_8MHz_31k25BPS_CFG2;
                            cfg3 = MCP_8MHz_31k25BPS_CFG3;

                            break;
                        case CanSpeed.CAN_33KBPS:
                            cfg1 = MCP_8MHz_33k3BPS_CFG1;
                            cfg2 = MCP_8MHz_33k3BPS_CFG2;
                            cfg3 = MCP_8MHz_33k3BPS_CFG3;

                            break;
                        case CanSpeed.CAN_40KBPS:
                            cfg1 = MCP_8MHz_40kBPS_CFG1;
                            cfg2 = MCP_8MHz_40kBPS_CFG2;
                            cfg3 = MCP_8MHz_40kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_50KBPS:
                            cfg1 = MCP_8MHz_50kBPS_CFG1;
                            cfg2 = MCP_8MHz_50kBPS_CFG2;
                            cfg3 = MCP_8MHz_50kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_80KBPS:
                            cfg1 = MCP_8MHz_80kBPS_CFG1;
                            cfg2 = MCP_8MHz_80kBPS_CFG2;
                            cfg3 = MCP_8MHz_80kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_100KBPS:
                            cfg1 = MCP_8MHz_100kBPS_CFG1;
                            cfg2 = MCP_8MHz_100kBPS_CFG2;
                            cfg3 = MCP_8MHz_100kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_125KBPS:
                            cfg1 = MCP_8MHz_125kBPS_CFG1;
                            cfg2 = MCP_8MHz_125kBPS_CFG2;
                            cfg3 = MCP_8MHz_125kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_200KBPS:
                            cfg1 = MCP_8MHz_200kBPS_CFG1;
                            cfg2 = MCP_8MHz_200kBPS_CFG2;
                            cfg3 = MCP_8MHz_200kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_250KBPS:
                            cfg1 = MCP_8MHz_250kBPS_CFG1;
                            cfg2 = MCP_8MHz_250kBPS_CFG2;
                            cfg3 = MCP_8MHz_250kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_500KBPS:
                            cfg1 = MCP_8MHz_500kBPS_CFG1;
                            cfg2 = MCP_8MHz_500kBPS_CFG2;
                            cfg3 = MCP_8MHz_500kBPS_CFG3;

                            break;
                        case CanSpeed.CAN_1000KBPS:
                            cfg1 = MCP_8MHz_1000kBPS_CFG1;
                            cfg2 = MCP_8MHz_1000kBPS_CFG2;
                            cfg3 = MCP_8MHz_1000kBPS_CFG3;

                            break;
                        default:
                            set = 0;
                            break;
                    }
                    break;
                default:
                    set = 0;
                    break;
            }

            if (set == 0)
            {
                throw new InvalidOperationException(
                    message: "Bitrate not supported"
                );
            }

            Mcp2515.WriteByte(Address.Cnf1, cfg1);
            Mcp2515.WriteByte(Address.Cnf2, cfg2);
            Mcp2515.WriteByte(Address.Cnf3, cfg3);

            _logger.LogInformation(
                "CAN Bitrate set to {0} with {1} oscillator.",
                canSpeed, mcp2515Clock);
            _logger.LogInformation(
                "Cnf value: 0x{0:X2} 0x{1:X2} 0x{2:X2}",
                cfg1, cfg2, cfg3);
        }

        /*
         *  Speed 8M
         */
        const byte MCP_8MHz_1000kBPS_CFG1 = 0x00;
        const byte MCP_8MHz_1000kBPS_CFG2 = 0x80;
        const byte MCP_8MHz_1000kBPS_CFG3 = 0x80;

        const byte MCP_8MHz_500kBPS_CFG1 = 0x00;
        const byte MCP_8MHz_500kBPS_CFG2 = 0x90;
        const byte MCP_8MHz_500kBPS_CFG3 = 0x82;

        const byte MCP_8MHz_250kBPS_CFG1 = 0x00;
        const byte MCP_8MHz_250kBPS_CFG2 = 0xB1;
        const byte MCP_8MHz_250kBPS_CFG3 = 0x85;

        const byte MCP_8MHz_200kBPS_CFG1 = 0x00;
        const byte MCP_8MHz_200kBPS_CFG2 = 0xB4;
        const byte MCP_8MHz_200kBPS_CFG3 = 0x86;

        const byte MCP_8MHz_125kBPS_CFG1 = 0x01;
        const byte MCP_8MHz_125kBPS_CFG2 = 0xB1;
        const byte MCP_8MHz_125kBPS_CFG3 = 0x85;

        const byte MCP_8MHz_100kBPS_CFG1 = 0x01;
        const byte MCP_8MHz_100kBPS_CFG2 = 0xB4;
        const byte MCP_8MHz_100kBPS_CFG3 = 0x86;

        const byte MCP_8MHz_80kBPS_CFG1 = 0x01;
        const byte MCP_8MHz_80kBPS_CFG2 = 0xBF;
        const byte MCP_8MHz_80kBPS_CFG3 = 0x87;

        const byte MCP_8MHz_50kBPS_CFG1 = 0x03;
        const byte MCP_8MHz_50kBPS_CFG2 = 0xB4;
        const byte MCP_8MHz_50kBPS_CFG3 = 0x86;

        const byte MCP_8MHz_40kBPS_CFG1 = 0x03;
        const byte MCP_8MHz_40kBPS_CFG2 = 0xBF;
        const byte MCP_8MHz_40kBPS_CFG3 = 0x87;

        const byte MCP_8MHz_33k3BPS_CFG1 = 0x47;
        const byte MCP_8MHz_33k3BPS_CFG2 = 0xE2;
        const byte MCP_8MHz_33k3BPS_CFG3 = 0x85;

        const byte MCP_8MHz_31k25BPS_CFG1 = 0x07;
        const byte MCP_8MHz_31k25BPS_CFG2 = 0xA4;
        const byte MCP_8MHz_31k25BPS_CFG3 = 0x84;

        const byte MCP_8MHz_20kBPS_CFG1 = 0x07;
        const byte MCP_8MHz_20kBPS_CFG2 = 0xBF;
        const byte MCP_8MHz_20kBPS_CFG3 = 0x87;

        const byte MCP_8MHz_10kBPS_CFG1 = 0x0F;
        const byte MCP_8MHz_10kBPS_CFG2 = 0xBF;
        const byte MCP_8MHz_10kBPS_CFG3 = 0x87;

        const byte MCP_8MHz_5kBPS_CFG1 = 0x1F;
        const byte MCP_8MHz_5kBPS_CFG2 = 0xBF;
        const byte MCP_8MHz_5kBPS_CFG3 = 0x87;

        /*
         *  speed 16M
         */
        const byte MCP_16MHz_1000kBPS_CFG1 = 0x00;
        const byte MCP_16MHz_1000kBPS_CFG2 = 0xD0;
        const byte MCP_16MHz_1000kBPS_CFG3 = 0x82;

        const byte MCP_16MHz_500kBPS_CFG1 = 0x00;
        const byte MCP_16MHz_500kBPS_CFG2 = 0xF0;
        const byte MCP_16MHz_500kBPS_CFG3 = 0x86;

        const byte MCP_16MHz_250kBPS_CFG1 = 0x41;
        const byte MCP_16MHz_250kBPS_CFG2 = 0xF1;
        const byte MCP_16MHz_250kBPS_CFG3 = 0x85;

        const byte MCP_16MHz_200kBPS_CFG1 = 0x01;
        const byte MCP_16MHz_200kBPS_CFG2 = 0xFA;
        const byte MCP_16MHz_200kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_125kBPS_CFG1 = 0x03;
        const byte MCP_16MHz_125kBPS_CFG2 = 0xF0;
        const byte MCP_16MHz_125kBPS_CFG3 = 0x86;

        const byte MCP_16MHz_100kBPS_CFG1 = 0x03;
        const byte MCP_16MHz_100kBPS_CFG2 = 0xFA;
        const byte MCP_16MHz_100kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_95kBPS_CFG1 = 0x03;
        const byte MCP_16MHz_95kBPS_CFG2 = 0xAD;
        const byte MCP_16MHz_95kBPS_CFG3 = 0x07;

        const byte MCP_16MHz_83k3BPS_CFG1 = 0x03;
        const byte MCP_16MHz_83k3BPS_CFG2 = 0xBE;
        const byte MCP_16MHz_83k3BPS_CFG3 = 0x07;

        const byte MCP_16MHz_80kBPS_CFG1 = 0x03;
        const byte MCP_16MHz_80kBPS_CFG2 = 0xFF;
        const byte MCP_16MHz_80kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_50kBPS_CFG1 = 0x07;
        const byte MCP_16MHz_50kBPS_CFG2 = 0xFA;
        const byte MCP_16MHz_50kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_40kBPS_CFG1 = 0x07;
        const byte MCP_16MHz_40kBPS_CFG2 = 0xFF;
        const byte MCP_16MHz_40kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_33k3BPS_CFG1 = 0x4E;
        const byte MCP_16MHz_33k3BPS_CFG2 = 0xF1;
        const byte MCP_16MHz_33k3BPS_CFG3 = 0x85;

        const byte MCP_16MHz_20kBPS_CFG1 = 0x0F;
        const byte MCP_16MHz_20kBPS_CFG2 = 0xFF;
        const byte MCP_16MHz_20kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_10kBPS_CFG1 = 0x1F;
        const byte MCP_16MHz_10kBPS_CFG2 = 0xFF;
        const byte MCP_16MHz_10kBPS_CFG3 = 0x87;

        const byte MCP_16MHz_5kBPS_CFG1 = 0x3F;
        const byte MCP_16MHz_5kBPS_CFG2 = 0xFF;
        const byte MCP_16MHz_5kBPS_CFG3 = 0x87;

        /*
         *  speed 20M
         */
        const byte MCP_20MHz_1000kBPS_CFG1 = 0x00;
        const byte MCP_20MHz_1000kBPS_CFG2 = 0xD9;
        const byte MCP_20MHz_1000kBPS_CFG3 = 0x82;

        const byte MCP_20MHz_500kBPS_CFG1 = 0x00;
        const byte MCP_20MHz_500kBPS_CFG2 = 0xFA;
        const byte MCP_20MHz_500kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_250kBPS_CFG1 = 0x41;
        const byte MCP_20MHz_250kBPS_CFG2 = 0xFB;
        const byte MCP_20MHz_250kBPS_CFG3 = 0x86;

        const byte MCP_20MHz_200kBPS_CFG1 = 0x01;
        const byte MCP_20MHz_200kBPS_CFG2 = 0xFF;
        const byte MCP_20MHz_200kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_125kBPS_CFG1 = 0x03;
        const byte MCP_20MHz_125kBPS_CFG2 = 0xFA;
        const byte MCP_20MHz_125kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_100kBPS_CFG1 = 0x04;
        const byte MCP_20MHz_100kBPS_CFG2 = 0xFA;
        const byte MCP_20MHz_100kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_83k3BPS_CFG1 = 0x04;
        const byte MCP_20MHz_83k3BPS_CFG2 = 0xFE;
        const byte MCP_20MHz_83k3BPS_CFG3 = 0x87;

        const byte MCP_20MHz_80kBPS_CFG1 = 0x04;
        const byte MCP_20MHz_80kBPS_CFG2 = 0xFF;
        const byte MCP_20MHz_80kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_50kBPS_CFG1 = 0x09;
        const byte MCP_20MHz_50kBPS_CFG2 = 0xFA;
        const byte MCP_20MHz_50kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_40kBPS_CFG1 = 0x09;
        const byte MCP_20MHz_40kBPS_CFG2 = 0xFF;
        const byte MCP_20MHz_40kBPS_CFG3 = 0x87;

        const byte MCP_20MHz_33k3BPS_CFG1 = 0x0B;
        const byte MCP_20MHz_33k3BPS_CFG2 = 0xFF;
        const byte MCP_20MHz_33k3BPS_CFG3 = 0x87;
    }
}
