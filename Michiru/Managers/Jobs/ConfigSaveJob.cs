using Michiru.Configuration;
using Michiru.Configuration._Base_Bot;
using Quartz;

namespace Michiru.Managers.Jobs;

public class ConfigSaveJob : IJob {
    public async Task Execute(IJobExecutionContext context) {
        try {
            if (Config.ShouldUpdateConfigFile) {
                Config.SaveFile();
            }
        }
        catch (Exception err) {
            await ErrorSending.SendErrorToLoggingChannelAsync("Config Save:", null, err);
        }
    }
}