namespace MinecraftLogAnalyzer.Models
{
    internal record Advancement
    {
        public string Name { set; get; }
        public DateTime UnlockTime { get; set; }
    }
}
