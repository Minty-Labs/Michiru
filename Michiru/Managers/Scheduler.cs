using Michiru.Configuration;
using Michiru.Managers.Jobs;
using Quartz;
using Serilog;

namespace Michiru.Managers;

public class Scheduler {
    private static readonly ILogger Logger = Log.ForContext(typeof(Scheduler));
    public static IScheduler TheScheduler { get; set; } = null!;
    public static IJobDetail StatusLoopJob { get; set; } = null!;
    public static ITrigger StatusLoopTrigger { get; set; } = null!;
    
    public static IJobDetail ConfigSaveLoopJob { get; set; } = null!;
    public static ITrigger ConfigSaveLoopTrigger { get; set; } = null!;

    public static async Task Initialize() {
        Logger.Information("Creating and Building...");
        TheScheduler = await SchedulerBuilder.Create()
            .UseDefaultThreadPool(x => x.MaxConcurrency = 2)
            .BuildScheduler();
        await TheScheduler.Start();

        StatusLoopJob = JobBuilder.Create<RotatingStatusJob>().Build();
        StatusLoopTrigger = TriggerBuilder.Create()
            .WithIdentity("StatusLoop", Vars.Name)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(Config.Base.RotatingStatus.MinutesPerStatus)
                .RepeatForever())
            .Build();
        await TheScheduler.ScheduleJob(StatusLoopJob, StatusLoopTrigger);
        
        ConfigSaveLoopJob = JobBuilder.Create<ConfigSaveJob>().Build();
        ConfigSaveLoopTrigger = TriggerBuilder.Create()
            .WithIdentity("ConfigSaveLoop", Vars.Name)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(10)
                .RepeatForever())
            .Build();
        
        Logger.Information("Initialized!");
    }
}