using System.Text;
using Discord;
using Discord.Interactions;
using Michiru.Configuration;
using Michiru.Configuration.Classes;
using Michiru.Managers;
using Michiru.Utils;

namespace Michiru.Commands.Slash;

public class BotConfigControlCmds : InteractionModuleBase<SocketInteractionContext> {
    
    [Group("config", "Configuration Commands"), RequireOwner, IntegrationType(ApplicationIntegrationType.GuildInstall)]
    public class ConfigControl : InteractionModuleBase<SocketInteractionContext> {
        public enum RotatingStatusPreAction {
            [ChoiceDisplay("Enable")] Enable = 1,
            [ChoiceDisplay("Disable")] Disable = 2,
            [ChoiceDisplay("List")] List = 3,
            [ChoiceDisplay("Next")] Next = 4
        }

        public enum RotatingStatusAction {
            [ChoiceDisplay("Add")] Add = 1,
            [ChoiceDisplay("Update")] Update = 2,
            [ChoiceDisplay("Remove")] Remove = 3
        }
        
        [SlashCommand("rotatingstatus", "Enables, disables, lists, or goes to the next rotating status")]
        public async Task RotatingStatus(RotatingStatusPreAction preAction) {
            switch (preAction) {
                case RotatingStatusPreAction.Enable:
                    Config.Base.RotatingStatus.Enabled = true;
                    await RespondAsync("Enabled Rotating Status", ephemeral: true);
                    break;
                case RotatingStatusPreAction.Disable:
                    Config.Base.RotatingStatus.Enabled = false;
                    await RespondAsync("Disabled Rotating Status", ephemeral: true);
                    break;
                case RotatingStatusPreAction.List:
                    var sb = new StringBuilder();
                    sb.AppendLine(string.Join("\n", Config.Base.RotatingStatus.Statuses.Select((x, i) => $"[{i} - {x.ActivityType} - {x.UserStatus}] {x.ActivityText}")));
                    await RespondAsync(sb.ToString());
                    return;
                case RotatingStatusPreAction.Next: {
                    await Managers.Jobs.RotatingStatus.Update();
                    await RespondAsync("Skipped to next status.", ephemeral: true);
                    return;
                }
                default: throw new ArgumentOutOfRangeException(nameof(preAction), preAction, null);
            }

            Config.Save();
        }
        
        [SlashCommand("modifyrotatingstatus", "Adds, updates, or removes a rotating status")]
        public async Task ModifyRotatingStatus(RotatingStatusAction action,
            [Summary(description: "ex. Playing, Watching, Custom, ...")]
            string activityType = "$XX",
            [Summary(description: "ex. Online, Idle, ...")]
            string userStatus = "$XX",
            [Summary(description: "Actual Status Text")]
            string activityText = "$XX",
            [Summary(description: "Status ID")] string statusId = "$XX") {
            switch (action) {
                case RotatingStatusAction.Add:
                    var status = new Status {
                        Id = Config.Base.RotatingStatus.Statuses.Count + 1,
                        ActivityText = activityText,
                        ActivityType = activityType,
                        UserStatus = userStatus
                    };
                    Config.Base.RotatingStatus.Statuses.Add(status);
                    await RespondAsync($"Added [{status.Id} - {status.ActivityType} - {status.UserStatus}] {status.ActivityText}");
                    break;
                case RotatingStatusAction.Update:
                    var statusUpdate = Config.Base.RotatingStatus.Statuses.Single(s => s.Id == int.Parse(statusId));
                    var tempActivityText = statusUpdate.ActivityText;
                    var tempActivityType = statusUpdate.ActivityType;
                    var tempUserStatus = statusUpdate.UserStatus;
                    statusUpdate.ActivityText = activityText;
                    statusUpdate.ActivityType = activityType;
                    statusUpdate.UserStatus = userStatus;
                    await RespondAsync(
                        $"Old\n" +
                        $"[{statusUpdate.Id} - {tempActivityType} - {tempUserStatus}] {tempActivityText}\n" +
                        $"New:\n" +
                        $"[{statusUpdate.Id} - {statusUpdate.ActivityType} - {statusUpdate.UserStatus}] {statusUpdate.ActivityText}");
                    break;
                case RotatingStatusAction.Remove:
                    var statusRemoval = Config.Base.RotatingStatus.Statuses.Single(s => s.Id == int.Parse(statusId));
                    await RespondAsync($"Removed [{statusRemoval.Id} - {statusRemoval.ActivityType} - {statusRemoval.UserStatus}] {statusRemoval.ActivityText}");
                    Config.Base.RotatingStatus.Statuses.Remove(statusRemoval);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        
            Config.Save();
        }
        
        [SlashCommand("rotatingstatusinterval", "Changes the interval between rotating statuses")]
        public async Task RotatingStatusInterval([Summary(description: "Minutes between status changes")] int minutes) {
            Config.Base.RotatingStatus.MinutesPerStatus = minutes;
            await RespondAsync($"Rotating Status Interval set to {minutes} minutes", ephemeral: true);
            Config.Save();
            // await RespondAsync("Attempting to restart and update the Scheduler", ephemeral: true);
            // try {
            //     await Scheduler.TheScheduler.Shutdown();
            //     Scheduler.TheScheduler = null!;
            //     Scheduler.StatusLoopJob = null!;
            //     Scheduler.StatusLoopTrigger = null!;
            //     await Scheduler.Initialize();
            //     await RespondAsync("Scheduler Restarted", ephemeral: true);
            // }
            // catch (Exception e) {
            //     await RespondAsync($"Error restarting Scheduler: " + MarkdownUtils.ToCodeBlockMultiline(e.Message), ephemeral: true);
            // }
        }
        
        [SlashCommand("setapikey", "Changes API keys")]
        public async Task SetApiKey() {
            var modal = new ModalBuilder {
                    Title = "API Key",
                    CustomId = "setapikey"
                }
                .AddTextInput("API Type", "apiType", required: true, placeholder: "fluxpoint, cookie, unsplashsecret, unsplashaccess", style: TextInputStyle.Short)
                .AddTextInput("Key", "apiKey", required: true, placeholder: StringUtils.GetRandomString(), style: TextInputStyle.Paragraph);

            await Context.Interaction.RespondWithModalAsync(modal.Build());
        }
        
        [SlashCommand("setspotifyapikeys", "Changes Spotify API keys")]
        public async Task SetSpotifyApiKey() {
            var modal = new ModalBuilder {
                    Title = "Spotify API Keys",
                    CustomId = "setspotifyapikey"
                }
                .AddTextInput("Client", "spotclient", required: true, placeholder: "Elly Is", style: TextInputStyle.Short)
                .AddTextInput("Secret", "spotsecret", required: true, placeholder: "Ultra Cute", style: TextInputStyle.Short);

            await Context.Interaction.RespondWithModalAsync(modal.Build());
        }
        
        // [SlashCommand("setdeezerapikeys", "Changes Deezer API keys")]
        // public async Task SetDeezerApiKey() {
        //     var modal = new ModalBuilder {
        //             Title = "Deezer API Keys",
        //             CustomId = "setdeezerapikey"
        //         }
        //         .AddTextInput("Client", "deezclient", required: true, placeholder: "I love", style: TextInputStyle.Short)
        //         .AddTextInput("Secret", "deezsecret", required: true, placeholder: "My adorable floof", style: TextInputStyle.Short);
        //
        //     await Context.Interaction.RespondWithModalAsync(modal.Build());
        // }
        
        [SlashCommand("save", "Forcibly saves the current configuration values")]
        public async Task Save() {
            Config.SaveFile();
            await RespondAsync("Configuration Saved", ephemeral: true);
        }
    }
}