using Michiru.Configuration;
using Michiru.Managers.Jobs;
using Quartz;
using Serilog;

namespace Michiru.Managers;

public class Scheduler {
    private static readonly ILogger Logger = Log.ForContext(typeof(Scheduler));

    public static async Task Initialize() {
        Logger.Information("Creating and Building...");
        var theScheduler = await SchedulerBuilder.Create()
            .UseDefaultThreadPool(x => x.MaxConcurrency = 2)
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
        
        Logger.Information("Initialized!");
    }
}