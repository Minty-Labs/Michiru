using Discord.Interactions;
using System.Text;
using Discord;
using Michiru.Commands.Preexecution;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration.Music;
using Michiru.Configuration.Music.Classes;
using Michiru.Events;
using Michiru.Utils;
using Serilog;

namespace Michiru.Commands.Slash;

public class Banger : InteractionModuleBase<SocketInteractionContext> {
    [Group("banger", "Banger Commands"),
     RequireUserPermission(GuildPermission.SendMessages),
     IntegrationType(ApplicationIntegrationType.GuildInstall),
     CommandContextType(InteractionContextType.Guild)]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {
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
                var bangerAmount = orderedBangers[i].SubmittedBangers;
                if (bangerAmount == 0) continue;
                sb.AppendLine($"{order}. {(order is 1 ? "**" : "")}{guild.Name} - Bangers: {bangerAmount}{(order is 1 ? "**" : "")}");
            }

            embed.WithDescription(sb.ToString());
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("search", "Searches for a song in the database")]
        public async Task SearchSong([Summary("song-url", "URL of a song")] string songUrl) {
            Submission songData;
            
            songData = Music.Base.MusicSubmissions.FirstOrDefault(x => {
                if (!string.IsNullOrWhiteSpace(x.SongLinkUrl))
                    return x.SongLinkUrl.Contains(songUrl);
                if (!string.IsNullOrWhiteSpace(x.Services.AppleMusicTrackUrl))
                    return x.Services.AppleMusicTrackUrl.Contains(songUrl);
                if (!string.IsNullOrWhiteSpace(x.Services.DeezerTrackUrl))
                    return x.Services.DeezerTrackUrl.Contains(songUrl);
                if (!string.IsNullOrWhiteSpace(x.Services.PandoraTrackUrl))
                    return x.Services.PandoraTrackUrl.Contains(songUrl);
                if (!string.IsNullOrWhiteSpace(x.Services.SpotifyTrackUrl))
                    return x.Services.SpotifyTrackUrl.Contains(songUrl);
                if (!string.IsNullOrWhiteSpace(x.Services.TidalTrackUrl))
                    return x.Services.TidalTrackUrl.Contains(songUrl);
                if (!string.IsNullOrWhiteSpace(x.Services.YoutubeTrackUrl))
                    return x.Services.YoutubeTrackUrl.Contains(songUrl);
                return false;
            })!;
            
            var dataAsJson = System.Text.Json.JsonSerializer.Serialize(songData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var sb = new StringBuilder();
            sb.AppendLine($"{MarkdownUtils.ToBold("Artists:")} {songData.Artists}");
            sb.AppendLine($"{MarkdownUtils.ToBold("Title:")} {songData.Title}");
            sb.AppendLine($"{MarkdownUtils.ToBold("Song Link URL:")} <{songData.SongLinkUrl}>");
            sb.AppendLine($"{MarkdownUtils.ToBold("Submission Date:")} {songData.SubmissionDate.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}");
            sb.AppendLine(MarkdownUtils.ToBold("Services:"));
            if (!string.IsNullOrWhiteSpace(songData.Services.SpotifyTrackUrl))
                sb.AppendLine($"- Spotify: <{songData.Services.SpotifyTrackUrl}>");
            if (!string.IsNullOrWhiteSpace(songData.Services.TidalTrackUrl))
                sb.AppendLine($"- Tidal: <{songData.Services.TidalTrackUrl}>");
            if (!string.IsNullOrWhiteSpace(songData.Services.YoutubeTrackUrl))
                sb.AppendLine($"- Youtube: <{songData.Services.YoutubeTrackUrl}>");
            if (!string.IsNullOrWhiteSpace(songData.Services.DeezerTrackUrl))
                sb.AppendLine($"- Deezer: <{songData.Services.DeezerTrackUrl}>");
            if (!string.IsNullOrWhiteSpace(songData.Services.AppleMusicTrackUrl))
                sb.AppendLine($"- Apple: <{songData.Services.AppleMusicTrackUrl}>");
            if (!string.IsNullOrWhiteSpace(songData.Services.PandoraTrackUrl))
                sb.AppendLine($"- Pandora: <{songData.Services.PandoraTrackUrl}>");
            
            await RespondAsync(sb + "\n\n" + MarkdownUtils.ToCodeBlockMultiline(dataAsJson, CodingLanguages.json), ephemeral: true);
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

        [SlashCommand("regather", "Regathers song data from entry")]
        public async Task RegatherSongData([Summary("song", "Song Link Url")] string songLinkUrl) {
            // check if the songLinkUrl is valid
            if (!songLinkUrl.Contains("song.link")) {
                await RespondAsync("Please provide a valid song.link URL.", ephemeral: true);
                return;
            }
            
            // find entry in the database and remove it
            var songData = Music.Base.MusicSubmissions.FirstOrDefault(x => x.SongLinkUrl == songLinkUrl);
            if (songData == null) {
                await RespondAsync("No song data found for the provided link.", ephemeral: true);
                return;
            }

            var oldDate = songData.SubmissionDate;
            
            // remove the entry from the database
            Music.Base.MusicSubmissions.Remove(songData);
            
            // re-gather the song data
            var newSongData = await BangerListener.RegatherBangerData(songLinkUrl);
            if (newSongData is null) {
                await RespondAsync("Failed to extract data from the URL.", ephemeral: true);
                return;
            }
        
            var services = new Services();
            var songName = newSongData["title"];
            var songArtists = newSongData["artists"];
            var servicesRaw = newSongData["services"].Split(',');
        
            foreach (var s in servicesRaw) {
                var split = s.Split('`');
            
                switch (split[0]) {
                    case "spotify":
                        services.SpotifyTrackUrl = split[1];
                        break;
                    case "tidal":
                        services.TidalTrackUrl = split[1];
                        break;
                    case "youtube":
                        services.YoutubeTrackUrl = split[1];
                        break;
                    case "deezer":
                        services.DeezerTrackUrl = split[1];
                        break;
                    case "apple":
                        services.AppleMusicTrackUrl = split[1];
                        break;
                    case "pandora":
                        services.PandoraTrackUrl = split[1];
                        break;
                }
            }
            
            var data = new Submission {
                Artists = songArtists,
                Title = songName.Replace("&#x27;", "'").Replace("&amp;", "&"),
                Services = services,
                SongLinkUrl = songLinkUrl,
                SubmissionDate = oldDate
            };
            Music.Base.MusicSubmissions.Add(data);
            Music.Save();
            
            var dataAsJson = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await RespondAsync($"Successfully re-gathered song data for: **{songName}** by **{songArtists}**.\n" +
                               MarkdownUtils.ToSubText($"First submitted: {oldDate.ConvertToDiscordTimestamp(TimestampFormat.LongDateTime)}") +
                               "\nFull Data:\n" +
                               MarkdownUtils.ToCodeBlockMultiline(dataAsJson, CodingLanguages.json), ephemeral: true);
            
        }
        
        [SlashCommand("edit", "Edit a banger submission")]
        public async Task EditBangerSubmission(
            [Summary("song-link", "The song link to edit")] string songLinkUrl,
            [Summary("new-title", "The new title of the song")] string? newTitle = "",
            [Summary("new-artists", "The new artists of the song")] string? newArtists = "") {
            var isEditingTitle = !string.IsNullOrWhiteSpace(newTitle);
            var isEditingArtists = !string.IsNullOrWhiteSpace(newArtists);
            if (!isEditingTitle && !isEditingArtists) {
                await RespondAsync("You must provide at least one field to edit (title or artists).", ephemeral: true);
                return;
            }
            
            var submission = Music.Base.MusicSubmissions.FirstOrDefault(x => x.SongLinkUrl == songLinkUrl);
            if (submission is null) {
                await RespondAsync("No submission found with the provided song link.", ephemeral: true);
                return;
            }
            
            var finalTitle = (isEditingTitle ? newTitle : submission.Title)![..48];
            var finalArtists = (isEditingArtists ? newArtists : submission.Artists)![..48];

            var newSubmission = new Submission {
                Artists = finalArtists,
                Title = finalTitle,
                SongLinkUrl = submission.SongLinkUrl,
                Services = submission.Services,
                SubmissionDate = submission.SubmissionDate
            };
            Music.Base.MusicSubmissions.Remove(submission);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Music.Base.MusicSubmissions.Add(newSubmission);
            Music.Save();
            
            await RespondAsync($"Successfully updated the banger submission:\n" +
                               $"{MarkdownUtils.ToBold("New Title:")} {finalTitle}\n" +
                               $"{MarkdownUtils.ToBold("New Artists:")} {finalArtists}", ephemeral: true);
        }
    }
}