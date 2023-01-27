namespace MinecraftLogAnalyzer.Models
{
    public record MobKills
    {
        public string MobName { get; set; }
        public List<PlayerKill> PlayerKills { get; set; } = new List<PlayerKill>();
    }
}
