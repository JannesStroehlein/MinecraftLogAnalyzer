using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftLogAnalyzer.Models
{
    public record Log
    {
        public DateTime StartTime { get; set; }
        public string[] Lines { get; set; }

        public List<LogMessage> LogMessages { get; set; }

        public void Parse()
        {
            LogMessages = new List<LogMessage>();
            foreach (string l in Lines)
            {
                LogMessage m = new LogMessage(StartTime, l);
                if (m.Category != null)
                    LogMessages.Add(m);
            }
        }
    }
}
