namespace nanoMeow.Mcp2515
{
    public class PinSpiMcuMcp2515
    {
        public int BusId { get; set; }

        public int CS { get; set; }

        public int SO { get; set; }

        public int SI { get; set; }

        public int SCK { get; set; }

        public int INT { get; set; } = -1;
    }
}
