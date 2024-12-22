using Michiru.Configuration;
using Michiru.Configuration._Base_Bot;
using Michiru.Managers.Jobs;
using Quartz;
using Serilog;

namespace Michiru.Managers;

public class Scheduler {
    private static readonly ILogger Logger = Log.ForContext(typeof(Scheduler));

    public static async Task Initialize() {
        Logger.Information("Creating and Building...");
        var theScheduler = await SchedulerBuilder.Create()
            .UseDefaultThreadPool(x => x.MaxConcurrency = 3)
            .BuildScheduler();
        await theScheduler.Start();

        var statusLoopJob = JobBuilder.Create<RotatingStatusJob>().Build();
        var statusLoopTrigger = TriggerBuilder.Create()
            .WithIdentity("StatusLoop", Vars.Name)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(Config.Base.RotatingStatus.MinutesPerStatus)
                .RepeatForever())
            .Build();
        await theScheduler.ScheduleJob(statusLoopJob, statusLoopTrigger);
        
        var configSaveLoopJob = JobBuilder.Create<ConfigSaveJob>().Build();
        var configSaveLoopTrigger = TriggerBuilder.Create()
            .WithIdentity("ConfigSaveLoop", Vars.Name)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(10)
                .RepeatForever())
            .Build();
        await theScheduler.ScheduleJob(configSaveLoopJob, configSaveLoopTrigger);
        
        var lookForOfflineGoHPJob = JobBuilder.Create<LookForOfflineGoHP>().Build();
        var lookForOfflineGoHPTrigger = TriggerBuilder.Create()
            .WithIdentity("LookForOfflineGoHP", Vars.Name)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(30)
                .RepeatForever())
            .Build();
        await theScheduler.ScheduleJob(lookForOfflineGoHPJob, lookForOfflineGoHPTrigger);
        
        Logger.Information("Initialized!");
    }
}