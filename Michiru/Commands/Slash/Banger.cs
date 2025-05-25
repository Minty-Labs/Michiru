using Discord.Interactions;
using System.Text;
using Discord;
using Michiru.Commands.Preexecution;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration.Music;
using Michiru.Events;
using Serilog;

namespace Michiru.Commands.Slash;

public class Banger : InteractionModuleBase<SocketInteractionContext> {
    [Group("banger", "Banger Commands"),
     RequireUserPermission(GuildPermission.SendMessages),
     IntegrationType(ApplicationIntegrationType.GuildInstall),
     CommandContextType(InteractionContextType.Guild)]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {
        /*[SlashCommand("lookup", "Displays multi-service links from a single link"), RateLimit(60, 10)]
        public async Task LookupFromAllMusicStreamingServices(
            [Summary("share-link", "Song Share URL")] string mediaUrl,
            [Summary("extra-text", "Express some words of wisdom about this song")] string extraText = "") {
            var logger = Log.ForContext("SourceContext", "COMMAND::Banger");
            var conf = Config.Base.Banger.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (conf is null || !conf.Enabled) return;
            
            var theActualUrl = BangerListener.ExtractUrl(mediaUrl);
            if (string.IsNullOrWhiteSpace(theActualUrl)) {
                await RespondAsync("Invalid URL.", ephemeral: true);
                return;
            }
            var upVote = BangerListener.GetEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId, ":thumbsup:");
            var downVote = BangerListener.GetEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId, ":thumbsdown:");

            logger.Information("Checking mediaUrl for URL: {0}", theActualUrl);
            if (!BangerListener.IsUrlWhitelisted(theActualUrl, conf.WhitelistedUrls!)) {
                if (!conf.SpeakFreely)
                    await RespondAsync("Message does not contain a valid whitelisted URL.", ephemeral: true);
                return;
            }

            var hasBeenSubmittedBefore = Music.SearchForMatchingSubmissions(theActualUrl);
            if (hasBeenSubmittedBefore) {
                await BangerListener.HandleExistingSubmissionCmd(Context, conf, theActualUrl, upVote, downVote, extraText);
                return;
            }

            await BangerListener.HandleNewSubmissionCmd(Context, conf, theActualUrl, upVote, downVote, extraText);
        }*/

        [SlashCommand("getbangercount", "Gets current guild banger count")]
        public async Task GetBangerCount([Summary("ephemeral", "Ephemeral response")] bool ephemeral = false)
            => await RespondAsync($"There are {Config.GetGuildBanger(Context.Guild.Id).SubmittedBangers} bangers in this guild.", ephemeral: ephemeral);

        [SlashCommand("leaderboard", "Guild Banger count Leaderboard")]
        public async Task Leaderboard() {
            var getAll = Config.Base.Banger;
            var orderedBangers = getAll.OrderByDescending(x => x.SubmittedBangers).Take(getAll.Count).ToList();

            var embed = new EmbedBuilder()
                .WithTitle("Banger Leaderboard")
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.Now);

            embed.AddField("Total Bangers", Config.GetBangerNumber().ToString());

            var sb = new StringBuilder();

            for (var i = 0; i < orderedBangers.Count; i++) {
                var guild = Context.Client.GetGuild(orderedBangers[i].GuildId);
                if (guild is null) continue;
                var order = i + 1;
                sb.AppendLine($"{order}. {(order is 1 ? "**" : "")}{guild.Name} - Bangers: {orderedBangers[i].SubmittedBangers}{(order is 1 ? "**" : "")}");
            }

            embed.WithDescription(sb.ToString());
            await RespondAsync(embed: embed.Build());
        }
    }

    [Group("bangeradmin", "Admin Banger Commands"),
     RequireToBeSpecial,
     // RequireUserPermission((GuildPermission.SendMessages & GuildPermission.ManageMessages & GuildPermission.ManageGuild) | GuildPermission.Administrator),
     IntegrationType(ApplicationIntegrationType.GuildInstall),
     CommandContextType(InteractionContextType.Guild)]
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext> {
        [SlashCommand("toggle", "Toggles the banger system")]
        public async Task ToggleBangerSystem([Summary("toggle", "Toggle the banger system")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).Enabled = enabled;
            Config.Save();
            await RespondAsync($"Bangers are now {(enabled ? "enabled" : "disabled")}.");
        }

        [SlashCommand("setchannel", "Sets the channel to only bangers")]
        public async Task SetBangerChannel([Summary("channel", "Destination Discord Channel")] ITextChannel channel) {
            var b = Config.GetGuildBanger(Context.Guild.Id);
            if (b.GuildId == 0)
                b.GuildId = Context.Guild.Id;
            b.ChannelId = channel.Id;
            Config.Save();
            await RespondAsync($"Set Banger channel to {channel.Mention}.");
        }

        [SlashCommand("seturlerrormessage", "Changes the error message")]
        public async Task ChangeBangerUrlErrorMessage([Summary("message", "Admin defined error message")] string text) {
            var newText = string.IsNullOrWhiteSpace(text) || text is "none" or "null" ? "This URL is not whitelisted." : text;
            Config.GetGuildBanger(Context.Guild.Id).UrlErrorResponseMessage = newText;
            Config.Save();
            await RespondAsync($"Set Banger URL Error Message to: {newText}");
        }

        [SlashCommand("setexterrormessage", "Changes the error message")]
        public async Task ChangeBangerExtErrorMessage([Summary("message", "Admin defined error message")] string text) {
            var newText = string.IsNullOrWhiteSpace(text) || text is "none" or "null" ? "This file extension is not whitelisted." : text;
            Config.GetGuildBanger(Context.Guild.Id).FileErrorResponseMessage = newText;
            Config.Save();
            await RespondAsync($"Set Banger File Extension Error Message to: {newText}");
        }

        private static bool _doesItExist(string value, IEnumerable<string> list) => list.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));

        [SlashCommand("addurl", "Adds a URL to the whitelist")]
        public async Task AddUrl([Summary("url", "URL to whitelist")] string url)
            => await RespondAsync("Whitelisting URLs are disabled, please contact Lily to remove a file extension from the whitelist.", ephemeral: true);

        [SlashCommand("removeurl", "Removes a URL from the whitelist")]
        public async Task RemoveUrl([Summary("url", "URL to remove from the whitelist")] string url)
            => await RespondAsync("Whitelisting URLs are disabled, please contact Lily to remove a file extension from the whitelist.", ephemeral: true);

        [SlashCommand("addext", "Adds a file extension to the whitelist")]
        public async Task AddExt([Summary("extension", "File extension to whitelist")] string ext)
            => await RespondAsync("Whitelisting file extensions is disabled, please contact Lily to remove a file extension from the whitelist.", ephemeral: true);

        [SlashCommand("removeext", "Removes a file extension from the whitelist")]
        public async Task RemoveExt([Summary("extension", "File extension to remove from the whitelist")] string ext)
            => await RespondAsync("Whitelisting file extensions is disabled, please contact Lily to remove a file extension from the whitelist.", ephemeral: true);

        [SlashCommand("addupvote", "Adds an upvote emoji to a banger post")]
        public async Task AddUpvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).AddUpvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Upvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("adddownvote", "Adds a downvote emoji to a banger post")]
        public async Task AddDownvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).AddDownvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Downvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("usecustomupvote", "Use a custom upvote emoji")]
        public async Task UseCustomUpvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).UseCustomUpvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Custom upvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("usecustomdownvote", "Use a custom downvote emoji")]
        public async Task UseCustomDownvote([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).UseCustomDownvoteEmoji = enabled;
            Config.Save();
            await RespondAsync($"Custom downvote emoji {(enabled ? "will show" : "will not show")} on banger posts.");
        }

        [SlashCommand("setcustomupvote", "Sets a custom upvote emoji")]
        public async Task SetCustomUpvoteTheLongWay([Summary("name", "Custom upvote emoji name")] string name, [Summary("id", "Custom upvote emoji ID")] string id) {
            if (!Emote.TryParse($"<:{name}:{id}>", out var emote)) {
                await RespondAsync("Invalid emoji. Is the bot in the same guild as where this emoji is from?");
                return;
            }

            Config.GetGuildBanger(Context.Guild.Id).CustomUpvoteEmojiName = name;
            Config.GetGuildBanger(Context.Guild.Id).CustomUpvoteEmojiId = ulong.Parse(id);
            Config.Save();
            await RespondAsync($"Custom upvote emoji set to {emote}.\nNote: Having a custom emoji ID of zero will logically mean that you are using a Discord default emoji.");
        }

        [SlashCommand("setcustomdownvote", "Sets a custom downvote emoji")]
        public async Task SetCustomDownvoteTheLongWay([Summary("name", "Custom downvote emoji name")] string name, [Summary("id", "Custom downvote emoji ID")] string id) {
            if (!Emote.TryParse($"<:{name}:{id}>", out var emote)) {
                await RespondAsync("Invalid emoji. Is the bot in the same guild as where this emoji is from?");
                return;
            }

            Config.GetGuildBanger(Context.Guild.Id).CustomDownvoteEmojiName = name;
            Config.GetGuildBanger(Context.Guild.Id).CustomDownvoteEmojiId = ulong.Parse(id);
            Config.Save();
            await RespondAsync($"Custom downvote emoji set to {emote}.\nNote: Having a custom emoji ID of zero will logically mean that you are using a Discord default emoji.");
        }

        [SlashCommand("speakfreely", "Allow users to talk freely")]
        public async Task SpeakFreely([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).SpeakFreely = enabled;
            Config.Save();
            await RespondAsync($"Users {(enabled ? "can" : "cannot")} speak freely in the banger channel.");
        }

        [SlashCommand("modifybangercount", "(Bot Owner Only) Modifies banger count"), RequireOwner]
        public async Task ModifyBangerCount(
            [Summary("number", "Number of bangers to add or remove")]
            int number,
            [Summary("ephemeral", "Ephemeral response")]
            bool ephemeral = false) {
            var banger = Config.GetGuildBanger(Context.Guild.Id);
            banger.SubmittedBangers += number;
            Config.Save();
            await RespondAsync($"Banger count modified by {number}. New count: {banger.SubmittedBangers}", ephemeral: ephemeral);
        }
        
        [SlashCommand("embedsuppression", "Configure embed suppression for Banger links in a channel")]
        public async Task ConfigureBangerEmbedSuppression(
            [Summary("channel", "The channel to configure for Banger link handling.")] ITextChannel channel,
            [Summary("suppressEmbed", "True to suppress embeds, False to delete original message (default).")] bool suppressEmbed)
        {
            var bangerConfigs = Config.Base.Banger;
            var bangerConf = bangerConfigs.FirstOrDefault(x => x.ChannelId == channel.Id);

            if (bangerConf == null) {
                await RespondAsync("No Banger configuration found for this channel. Please set up Banger for this channel first.", ephemeral: true);
                return;
            }

            bangerConf.SuppressEmbedInsteadOfDelete = suppressEmbed;
            Config.Save();
            await RespondAsync($"Banger embed suppression for channel {channel.Mention} set to {suppressEmbed}.", ephemeral: true);
        }
    }
}