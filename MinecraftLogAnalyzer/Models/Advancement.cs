namespace MinecraftLogAnalyzer.Models
{
    public record Advancement
    {
        public string Name { set; get; }
        public DateTime UnlockTime { get; set; }
    }
}
