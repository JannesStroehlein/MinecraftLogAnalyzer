namespace MinecraftLogAnalyzer.Models
{
    internal record PlayerKill
    {
        public string DeathQualifier { set; get; }
        public string Target { set; get; }
        public string ItemName { set; get; }
        public string Message { set; get; }
        public DateTime Time { set; get; }
    }
}
