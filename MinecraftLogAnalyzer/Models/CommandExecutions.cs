namespace MinecraftLogAnalyzer.Models
{
    public record CommandExecutions
    {
        public string Command { set; get; }
        public DateTime Time { set; get; }
    }
}
