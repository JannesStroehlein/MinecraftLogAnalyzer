using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MinecraftLogAnalyzer.Models
{
    internal record LogMessage
    {
        private static Regex logMessageRegex = new Regex(@"\[(\d{2}:\d{2}:\d{2})\] \[(.*)/(.*)\]: (.*)");

        public DateTime TimeStamp { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }

        public LogMessage(DateTime date, string message)
        {
            //[13:40:38] [Server thread/INFO]: Lonnietbc left the game
            Match match = logMessageRegex.Match(message);
            if (!match.Success)
                return;
            this.TimeStamp = date.Date.Add(DateTime.Parse(match.Groups[1].Value).TimeOfDay);
            this.Category = match.Groups[2].Value;
            this.Severity = match.Groups[3].Value;
            this.Message = match.Groups[4].Value;

            /*if (!message.Contains(']') || message.StartsWith('\t'))
                return;

            string[] msgSegments = message.Split('[');
            if (msgSegments.Length < 3)
                return;
            int splitPos = msgSegments[2].IndexOf('/');
            int msgStart = msgSegments[2].IndexOf(']');
            Category = msgSegments[2].Substring(0, splitPos);
            Severity = msgSegments[2].Substring(splitPos + 1, msgStart - splitPos - 1);
            int totalMsgStart = message.IndexOf("]: ") + 3;
            Message = message.Substring(totalMsgStart).TrimEnd().Replace("\0", "");*/
        }
    }
}
