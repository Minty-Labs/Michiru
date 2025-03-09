using Michiru.Configuration;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration.Moderation;
using Michiru.Configuration.Music;
using Quartz;

namespace Michiru.Managers.Jobs;

public class ConfigSaveJob : IJob {
    public async Task Execute(IJobExecutionContext context) {
        try {
            if (Config.ShouldUpdateConfigFile)
                Config.SaveFile();
        }
        catch (Exception err) {
            await ErrorSending.SendErrorToLoggingChannelAsync("Config Save:", null, err);
        }

        try {
            if (Music.ShouldUpdateConfigFile)
                Music.SaveFile();
        }
        catch (Exception err) {
            await ErrorSending.SendErrorToLoggingChannelAsync("Music Config Save:", null, err);
        }
    }
}