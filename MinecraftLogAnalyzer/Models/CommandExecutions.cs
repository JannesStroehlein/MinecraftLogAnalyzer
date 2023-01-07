namespace MinecraftLogAnalyzer.Models
{
    internal record CommandExecutions
    {
        public string Command { set; get; }
        public DateTime Time { set; get; }
    }
}
