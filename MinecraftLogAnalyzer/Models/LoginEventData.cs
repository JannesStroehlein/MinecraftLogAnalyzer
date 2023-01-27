namespace MinecraftLogAnalyzer.Models
{
    public record LoginEventData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string World { get; set; }
        public DateTime Time { get; set; }
    }
}
