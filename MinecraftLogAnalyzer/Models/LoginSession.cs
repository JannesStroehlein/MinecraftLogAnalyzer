namespace MinecraftLogAnalyzer.Models
{
    public record LoginSession
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string World { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LogoutTime { get; set; }
    }
}
