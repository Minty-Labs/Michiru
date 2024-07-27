using System.Diagnostics;
using Discord;
using Michiru.Utils;
using Quartz;
using Serilog;

namespace Michiru.Managers.Jobs;

public class LookForOfflineGoHP : IJob {
    private readonly ILogger Logger = Log.ForContext(typeof(LookForOfflineGoHP));
    private int _count = 0;
    
    public async Task Execute(IJobExecutionContext context) {
        try {
            var bot = Program.Instance.GetUser(489144212911030304);
            if (_count >= 2) return;
            if (bot == null) {
                await ErrorSending.SendErrorToLoggingChannelAsync("Look For Offline GoHP:", null, "Bot not found");
                _count++;
                return;
            }

            if (bot.Status == UserStatus.Offline) {
                Logger.Information("Bot Status: {0}", Enum.GetName(bot.Status));
                return;
                var process = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = "/bin/bash",
                        Arguments = "-c \"pm2 restart 1\"",
                        RedirectStandardOutput = false,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                // var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                // await ErrorSending.SendErrorToLoggingChannelAsync("Look For Offline GoHP:\n" + MarkdownUtils.ToBold("GoHP Restarted"), null, output);
                await Program.Instance.GeneralLogChannel.SendMessageAsync(MarkdownUtils.ToHeading1("GoHP Restarted" + "\n" + MarkdownUtils.ToSubText("bottom text")));
            }
        }
        catch (Exception err) {
            await ErrorSending.SendErrorToLoggingChannelAsync("Look For Offline GoHP:", null, err);
        }
    }
    
}