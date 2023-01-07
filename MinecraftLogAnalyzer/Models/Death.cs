namespace MinecraftLogAnalyzer.Models
{
    internal record Death
    {
        public string DeathQualifier { set; get; }
        public string Message { set; get; }
        public DateTime Time { set; get; }
    }
}
