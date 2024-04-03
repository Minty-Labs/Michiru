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

    public static async Task Initialize() {
        Logger.Information("Creating and Building...");
        TheScheduler = await SchedulerBuilder.Create()
            .UseDefaultThreadPool(x => x.MaxConcurrency = 1)
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
        
        Logger.Information("Initialized!");
    }
}