namespace MinecraftLogAnalyzer.Models
{
    public record Player
    {
        public string Name { get; set; }
        public string UUID { get; set; }    
        public DateTime LastSeen { get; set; }
        public TimeSpan PlayTime { get; set; }
        public Dictionary<DateTime, string> ChatMessages { get; set; } = new Dictionary<DateTime, string>();
        public List<Advancement> Advancements { get; set; } = new List<Advancement>();
        public List<PlayerKill> PlayerKills { get; set; } = new List<PlayerKill>();
        public List<Death> Deaths { get; set; } = new List<Death>();
        public List<CommandExecutions> Commands { get; set; } = new List<CommandExecutions>();
        public Dictionary<string, int> IPs { get; set; } = new Dictionary<string, int>();
        public List<LoginSession> LoginPositions { get; set; } = new List<LoginSession>();
    }
}
