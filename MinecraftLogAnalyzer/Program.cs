﻿using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using MinecraftLogAnalyzer.Models;
using MinecraftLogAnalyzer;

ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args);

if (result.Tag == ParserResultType.NotParsed)
    return;

CommandLineOptions options = result.Value;

string ExportDir = options.ExportDir != null ? options.ExportDir : options.LogDirectory + @"\export";
if (options.ExportDir == null && !options.NoExport)
    Directory.CreateDirectory(ExportDir);

if (!Directory.Exists(ExportDir) && !options.NoExport)
{
    Console.WriteLine("Error: The specified export directory does not exist.");
    return;
}
    
string logDir = options.LogDirectory;
//string logDir = args.Length == 0 ? @"C:\Users\jatis\Downloads\Serverlogs\2020Server" : args[0];

string[] files = Directory.GetFiles(logDir, "*.gz");

List<Log> logs = new List<Log>();

foreach (string file in files)
{
    Log log = new Log();
    // 2022-12-04-1.log
    List<string> lines = new List<string>();
    using (FileStream reader = File.OpenRead(file))
    using (GZipStream zip = new GZipStream(reader, CompressionMode.Decompress, true))
    using (StreamReader unzip = new StreamReader(zip))
        while (!unzip.EndOfStream)
        {
            lines.Add(unzip.ReadLine()!);
        }
    log.StartTime = DateTime.Parse(Path.GetFileName(file).Substring(0, 10) + " " + lines[0].Substring(1, 8));
    log.Lines = lines.ToArray();
    logs.Add(log);
}

int totalLines = 0;
foreach (Log l in logs)
{
    l.Parse();
    totalLines += l.Lines.Length;
}
    
Console.WriteLine($"Parsed {logs.Count} logs with a total of {totalLines} lines");

#region PlayTime
List<LogMessage> playerJoinAndLeaves = new List<LogMessage>();
foreach (Log l in logs)
{
    //Server thread/INFO
    var joinLeaves = from m in l.LogMessages 
                     where m.Category == "Server thread" && m.Severity == "INFO" 
                     where m.Message.Contains(" joined the game") || m.Message.Contains(" left the game") 
                     select m;
    playerJoinAndLeaves.AddRange(joinLeaves);
}
Console.WriteLine($"Found {playerJoinAndLeaves.Count} Player join and leave events");
Dictionary<string, TimeSpan> playtime = new Dictionary<string, TimeSpan>();
Dictionary<string, DateTime> runningSessions = new Dictionary<string, DateTime>();
foreach (LogMessage joinLeaveEvent in playerJoinAndLeaves)
{
    string playerName = joinLeaveEvent.Message.Substring(0, joinLeaveEvent.Message.IndexOf(' '));
    if (joinLeaveEvent.Message.Contains(" joined the game"))
    {
        if (runningSessions.ContainsKey(playerName)) // Server crashed and the session and was not logged
            runningSessions[playerName] = joinLeaveEvent.TimeStamp; // Reset the current session to the time of this join leave event
        else 
            runningSessions.Add(playerName, joinLeaveEvent.TimeStamp);
    }
    else
    {
        if (runningSessions.ContainsKey(playerName))
        {
            DateTime startTime = runningSessions[playerName];
            runningSessions.Remove(playerName);
            if (!playtime.ContainsKey(playerName))
                playtime.Add(playerName, TimeSpan.Zero);
            playtime[playerName] += joinLeaveEvent.TimeStamp - startTime;
        }
        else
        {
            Console.WriteLine($"The session date of {playerName} seems to be corrupted");
        }
    }
}

List<Player> players = new List<Player>();
foreach (KeyValuePair<string, TimeSpan> player in playtime)
    players.Add(new Player() { Name = player.Key, PlayTime = player.Value });

/*var sortedPlaytimes = from entry in playtime 
                      orderby entry.Value descending 
                      select entry;
*/
Console.WriteLine("Finished Calculating Playtimes");
#endregion

#region ChatMessages
foreach (Log l in logs)
{
    var messages = from msg in l.LogMessages
                           where msg.Category.StartsWith("Async Chat Thread")
                           select msg;
    
    if (messages.Count() == 0)
        continue;

    bool oldMsgs = Regex.Match(messages.First().Message, @"(?<=\<)(.*?)(?=\>)").Success;
    foreach (Player p in players)
    {
        if (oldMsgs)
        {
            var chatMessages = from msg in messages
                               where msg.Message.Contains($"<{p.Name}>")
                               select new KeyValuePair<DateTime, string>(msg.TimeStamp, msg.Message.Substring(msg.Message.IndexOf($"<{p.Name}>") + $"<{p.Name}> ".Length));
            foreach (var chatMsg in chatMessages)
                p.ChatMessages.Add(chatMsg.Key, chatMsg.Value);
        }
        else
        {
            var chatMessages = from msg in messages
                               where msg.Message.Contains($" | {p.Name}:")
                               select new KeyValuePair<DateTime, string>(msg.TimeStamp, msg.Message.Substring(msg.Message.IndexOf($" | {p.Name}:") + $" | {p.Name}: ".Length));
            foreach (var chatMsg in chatMessages)
                p.ChatMessages.Add(chatMsg.Key, chatMsg.Value);
        }
    }
}
Console.WriteLine("Finished Processing Player Chat Messages");
#endregion

#region Advancements
Dictionary<string, KeyValuePair<string, DateTime>> firstUnlocks = new Dictionary<string, KeyValuePair<string, DateTime>>();
foreach (Log l in logs)
{
    var advancementLogMessages = from msg in l.LogMessages
                   where msg.Category.StartsWith("Server thread") && msg.Severity == "INFO"
                   where msg.Message.Contains(" has made the advancement ")
                   select msg;
    foreach (LogMessage msg in advancementLogMessages)
    {
        Advancement advancement = new Advancement
        {
            Name = Regex.Match(msg.Message, @"(?<=\[)(.*?)(?=\])").Value,
            UnlockTime = msg.TimeStamp
        };
        if (!firstUnlocks.ContainsKey(advancement.Name))
            firstUnlocks.Add(advancement.Name, new KeyValuePair<string, DateTime>(msg.Message.Split(' ')[0], msg.TimeStamp));
        if (players.FirstOrDefault((p) => p.Name == msg.Message.Split(' ')[0]) == default(Player))
            players.Add(new Player() { Name = msg.Message.Split(' ')[0] });
        players.Find((p) => p.Name == msg.Message.Split(' ')[0])!.Advancements.Add(advancement);
    }
}
Console.WriteLine("Finished Processing Player Advancements");
#endregion

#region Kills/Deaths
Dictionary<string, int> mobKills = new Dictionary<string, int>();

// Reading all Death Messages
string langFile = @"C:\Users\jatis\source\repos\MinecraftLogAnalyzer\MinecraftLogAnalyzer\lang.lang";
Dictionary<string, string> deathMessages = new Dictionary<string, string>();
foreach (string line in File.ReadAllLines(langFile))
{
    if (line.StartsWith("death."))
    {
        string[] segments = line.Split('=');
        segments[1] = Regex.Replace(segments[1], @"(\%[0-9]\$s)", "(.*)");
        deathMessages.Add(segments[0], segments[1]);
    }       
}
var sortedDeathMsgs = from dmsg in deathMessages
                      orderby Regex.Matches(dmsg.Value, "\\(\\.\\*\\)").Count descending
                      select dmsg;

deathMessages = sortedDeathMsgs.ToDictionary(pair => pair.Key, pair => pair.Value);

foreach (Log l in logs)
{
    var possibleDeathMsgs = from msg in l.LogMessages
                            where msg.Category == "Server thread" && msg.Severity == "INFO" && !msg.Message.StartsWith("[") && msg.Message.Length < 80
                            && !msg.Message.Contains("issued server command") & !msg.Message.Contains("moved too quickly!") & !msg.Message.Contains("moved wrongly!")
                            && !msg.Message.Contains("logged in with entity id") & !msg.Message.Contains("joined the game") & !msg.Message.Contains("left the game")
                            select msg;
    foreach (LogMessage possibleDeathMsg in possibleDeathMsgs)
    {
        //(.*?)( was killed trying to hurt )(.*)
        foreach (KeyValuePair<string, string> keyValuePair in deathMessages)
        {
            Match match = Regex.Match(possibleDeathMsg.Message, keyValuePair.Value);
            if (match.Success)
            {
                /*Console.WriteLine($"{possibleDeathMsg.TimeStamp:g} Death: {keyValuePair.Key} " +
                    $"{match.Groups.Count switch
                    {
                        0 => "Wie kann eine Deathmessage mit 0 Varbiablen existieren?",
                        2 => $"{match.Groups[1].Value} died",
                        3 => $"{match.Groups[2].Value} killed {match.Groups[1].Value}",
                        4 => $"{match.Groups[2].Value} killed {match.Groups[1].Value} with {match.Groups[3].Value}",
                    }}");*/
                if (players.FirstOrDefault((p) => p.Name == match.Groups[1].Value) == default(Player))
                    players.Add(new Player() { Name = match.Groups[1].Value });
                players.Find((p) => p.Name == match.Groups[1].Value)!.Deaths.Add(
                    new Death()
                    {
                        Message = possibleDeathMsg.Message,
                        DeathQualifier = keyValuePair.Key,
                        Time = possibleDeathMsg.TimeStamp
                    });
                switch (match.Groups.Count)
                {
                    case 3:
                        if (players.FirstOrDefault((p) => p.Name == match.Groups[2].Value) == default(Player))
                        {
                            if (!mobKills.ContainsKey(match.Groups[2].Value))
                                mobKills.Add(match.Groups[2].Value, 0);
                            mobKills[match.Groups[2].Value]++;
                        }
                        else
                            players.Find((p) => p.Name == match.Groups[2].Value).PlayerKills.Add(new PlayerKill() { Target= match.Groups[1].Value, Message = possibleDeathMsg.Message, DeathQualifier = keyValuePair.Key, Time = possibleDeathMsg.TimeStamp });
                        break;
                    case 4:
                        if (players.FirstOrDefault((p) => p.Name == match.Groups[2].Value) == default(Player))
                        {
                            if (!mobKills.ContainsKey(match.Groups[2].Value))
                                mobKills.Add(match.Groups[2].Value, 0);
                            mobKills[match.Groups[2].Value]++;
                        }
                        else
                            players.Find((p) => p.Name == match.Groups[2].Value).PlayerKills.Add(new PlayerKill() { Target = match.Groups[1].Value, Message = possibleDeathMsg.Message, DeathQualifier = keyValuePair.Key, Time = possibleDeathMsg.TimeStamp, ItemName = match.Groups[3].Value.Replace("[", "").Replace("]", "") });
                        break;                
                }
                break;
            }
        }
    }
}
Console.WriteLine("Finished Processing Player Deaths/Kills");
#endregion

#region Commands
foreach (Log l in logs)
{
    var commandLogMessages = from msg in l.LogMessages
                             where msg.Category.StartsWith("Server thread") && msg.Severity == "INFO"
                             where msg.Message.Contains(" issued server command: ")
                             select msg;
    foreach (LogMessage msg in commandLogMessages)
    {
        string player = msg.Message.Substring(0, msg.Message.IndexOf(" issued server command: "));
        if (players.FirstOrDefault((p) => p.Name == player) == default(Player))
            players.Add(new Player() { Name = player });
        players.Find((p) => p.Name == player)!.Commands.Add(new CommandExecutions()
        {
            Command = msg.Message.Substring(msg.Message.IndexOf(" issued server command: ") + " issued server command: ".Length),
            Time = msg.TimeStamp
        });
    }
}
Console.WriteLine("Finished Processing Player Command Executions");
#endregion

#region Output
Console.WriteLine("\n");
Console.WriteLine("First ones to unlock advancements:");
Console.WriteLine($"{"Username",-20} {"Date",-25} {"Advancement",-40}\n");
foreach (var firstUnlock in firstUnlocks)
    Console.WriteLine($"{firstUnlock.Value.Key,-20} {firstUnlock.Value.Value,-25:G} {firstUnlock.Key,-40}");

var sortedMobKills = from entry in mobKills
                      orderby entry.Value descending
                      select entry;
Console.WriteLine("\nDeadliest Mobs:");
Console.WriteLine($"{"Name",-20} {"Kills",-8}\n");
foreach (KeyValuePair<string, int> keyValue in sortedMobKills)
    Console.WriteLine($"{keyValue.Key,-20} {keyValue.Value,-8}");

var sortedPlayerList = from p in players
                        orderby p.Deaths.Count / p.PlayTime.TotalHours descending
                        select p;
Console.WriteLine("\nPlayer Stats:");
Console.WriteLine($"\n{"Username",-20} {"Messages", -12} {"Kills", -6} {"Deaths",-6} {"d/h",-6} {"Commands",-10} {"Advancements", -16} {"Playtime",-10}\n");
foreach (Player p in sortedPlayerList)
    Console.WriteLine($"{p.Name,-20} {p.ChatMessages.Count,-12} {p.PlayerKills.Count,-6} {p.Deaths.Count,-6} {p.Deaths.Count / p.PlayTime.TotalHours,-6:N3} {p.Commands.Count,-8} {p.Advancements.Count, -16} {p.PlayTime.TotalHours,-10:N1} h");

if (options.NoExport)
{
    Console.WriteLine("Skipping export");
}
else
{
    foreach (Player p in players)
    {
        string filenameFormat = ExportDir + "\\" + p.Name.Replace(".", "") + " {0}.json";
        string filename = string.Format(filenameFormat, "");
        int i = 1;
        while (File.Exists(filename))
            filename = string.Format(filenameFormat, "(" + (i++) + ")");

        File.WriteAllText(filename, JsonConvert.SerializeObject(p, Formatting.Indented));
    }
    Console.WriteLine("Exported all playerdata");

    File.WriteAllText(Path.Combine(ExportDir, "mobs.json"), JsonConvert.SerializeObject(mobKills, Formatting.Indented));
    Console.WriteLine("Exported all mob kills");
}

#endregion
