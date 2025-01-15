using System.Text;
using Discord;
using Discord.Interactions;
using Michiru.Commands.Preexecution;
using Michiru.Configuration._Base_Bot;
using Michiru.Events;
using Michiru.Managers;
using Michiru.Utils;
using Serilog;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Michiru.Commands.Slash;

public class Banger : InteractionModuleBase<SocketInteractionContext> {
    [Group("banger", "Banger Commands"),
     RequireUserPermission(GuildPermission.SendMessages),
     IntegrationType(ApplicationIntegrationType.GuildInstall),
     CommandContextType(InteractionContextType.Guild)]
    public class Commands : InteractionModuleBase<SocketInteractionContext> {
        [SlashCommand("lookupspotifyonyoutube", "YT top result of Spotify link"), RateLimit(30, 5)]
        public async Task LookupSpotifyOnYouTubeCommand([Summary("shareLink", "Spotify Track")] string spotifyUrl) {
            var sluLogger = Log.ForContext("SourceContext", "COMMAND:SpotifyLookup");
            if (!spotifyUrl.Contains("http")) {
                await RespondAsync("No URL found in message", ephemeral: true);
                return;
            }

            IUserMessage? socketUserMessage = null;

            await DeferAsync();

            var conf = Config.GetGuildBanger(Context.Guild.Id);
            string? theActualUrl = null;
            var yt = new YoutubeClient();
            var sb = new StringBuilder().AppendLine("Top Result");
            theActualUrl ??= spotifyUrl;
            var isUrlGood = theActualUrl.Contains("spotify.com");

            // check if url is whitelisted
            if (isUrlGood) {
                // try to get album data from spotify
                var didSpotifyAlbumLookup = theActualUrl.AndContainsMultiple("spotify.com", "album");
                if (didSpotifyAlbumLookup) {
                    await ModifyOriginalResponseAsync(x => x.Content = "This command only supports Spotify Track URLs.").DeleteAfter(5);
                    return;
                }

                // try get spotify track if no album
                if (theActualUrl.AndContainsMultiple("spotify.com", "track")) {
                    sluLogger.Information("Found URL to be a Spotify Track");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?'))
                        finalId = theActualUrl.Split('?')[0];

                    Utils.MusicProviderApis.Spotify.Root? track;
                    try {
                        track = await Utils.MusicProviderApis.Spotify.GetTrackResults.GetTrackData(finalId.Split('/').Last());
                    }
                    catch (Exception ex) {
                        sluLogger.Error("Failed to get track data from Spotify API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Spotify API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Spotify API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Spotify API");
                        return; // fail command out right
                    }
                    
                    var videos = yt.Search.GetVideosAsync($"{track!.artists[0].name} {track.name}").GetAwaiter().GetResult();

                    var firstEntry = videos[0];
                    var title = firstEntry.Title;
                    var author = firstEntry.Author.ChannelTitle.Replace(" - Topic", "");
                    
                    var tidalTrackUrl = string.Empty;
                    try {
                        tidalTrackUrl = await Utils.MusicProviderApis.Tidal.GetSearchResults.SearchForUrl($"{track.artists[0].name} {track.name}");
                    }
                    catch {/*silent fail*/}

                    sb.AppendLine(title.Contains(author) ? MarkdownUtils.ToBold(title) : MarkdownUtils.ToBold($"{author} - {title}"));
                    sb.AppendLine(
                        MarkdownUtils.ToSubText(
                            MarkdownUtils.MakeLink("Original Spotify Link", theActualUrl, true) + " \u2197 \u2219 " +
                            MarkdownUtils.MakeLink("YouTube Link", firstEntry.Url)) + " \u2197 \u2219 " + // will show YouTube embed
                            ((tidalTrackUrl != "[t404] ZERO RESULTS" || !string.IsNullOrWhiteSpace(tidalTrackUrl)) ?
                                MarkdownUtils.MakeLink("Tidal Link", tidalTrackUrl, true) + " \u2197" /* + " \u2219 " + */ : "")
                          //MarkdownUtils.MakeLink("Deezer Link", "https://deezer.page.link/" + "", true) + " \u2197"
                    );

                    socketUserMessage = await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                    conf!.SubmittedBangers++;
                    Config.Save();
                }

                if (Context.Channel.Id == conf.ChannelId) {
                    var upVote = conf.CustomUpvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId) : Emote.Parse(conf.CustomUpvoteEmojiName) ?? Emote.Parse(":thumbsup:");
                    var downVote = conf.CustomDownvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId) : Emote.Parse(conf.CustomDownvoteEmojiName) ?? Emote.Parse(":thumbsdown:");

                    if (socketUserMessage is not null) {
                        if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                            await socketUserMessage.AddReactionAsync(upVote);
                        if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                            await socketUserMessage.AddReactionAsync(downVote);
                    }

                    conf.SubmittedBangers++;
                    Config.Save();
                }
            }
        }

        [SlashCommand("lookuptidalonyoutube", "YT top result of Tidal link"), RateLimit(30, 5)]
        public async Task LookupTidalOnYouTubeCommand([Summary("shareLink", "Tidal Track")] string tidalUrl) {
            var tluLogger = Log.ForContext("SourceContext", "COMMAND:TidalLookup");
            if (!tidalUrl.Contains("http")) {
                await RespondAsync("No URL found in message", ephemeral: true);
                return;
            }

            IUserMessage? socketUserMessage = null;

            await DeferAsync();

            var conf = Config.GetGuildBanger(Context.Guild.Id);
            string? theActualUrl = null;
            var yt = new YoutubeClient();
            var sb = new StringBuilder().AppendLine("Top Result");
            theActualUrl ??= tidalUrl;
            var isUrlGood = theActualUrl.Contains("tidal.com");

            // check if url is whitelisted
            if (isUrlGood) {
                if (theActualUrl.AndContainsMultiple("tidal.com", "track")) {
                    tluLogger.Information("Found URL to be a Tidal Track");
                    var finalId = theActualUrl;
                    if (theActualUrl.Contains('?'))
                        finalId = theActualUrl.Split('?')[0];

                    Utils.MusicProviderApis.Tidal.Root? track;
                    try {
                        track = await Utils.MusicProviderApis.Tidal.GetTrackResults.GetData(finalId.Split('/').Last());
                    }
                    catch (Exception ex) {
                        tluLogger.Error("Failed to get track data from Tidal API");
                        await ErrorSending.SendErrorToLoggingChannelAsync($"Failed to get track data from Tidal API in <#{Context.Channel.Id}>", obj: ex.StackTrace);
                        await ModifyOriginalResponseAsync(x => x.Content = "Failed to get track data from Tidal API\n*this message will be deleted in 5 seconds, Lily has been notified of error*")
                            .DeleteAfter(5, "Failed to get track data from Tidal API");
                        return; // fail command out right
                    }
                    
                    var videos = yt.Search.GetVideosAsync($"{track!.resource.artists[0]} {track!.resource.title}").GetAwaiter().GetResult();

                    var firstEntry = videos[0];
                    var title = firstEntry.Title;
                    var author = firstEntry.Author.ChannelTitle.Replace(" - Topic", "");
                    
                    var spotifyTrackUrl = string.Empty;
                    try {
                        spotifyTrackUrl = await Utils.MusicProviderApis.Spotify.GetSearchResults.SearchForUrl($"{track.resource.artists[0]} {track.resource.title}"); 
                    }
                    catch {/*silent fail*/}

                    sb.AppendLine(title.Contains(author) ? MarkdownUtils.ToBold(title) : MarkdownUtils.ToBold($"{author} - {title}"));
                    sb.AppendLine(
                        MarkdownUtils.ToSubText(
                            MarkdownUtils.MakeLink("Original Tidal Link", theActualUrl, true) + " \u2197 \u2219 " +
                            MarkdownUtils.MakeLink("YouTube Link", firstEntry.Url)) + " \u2197 \u2219 " + // will show YouTube embed
                            ((spotifyTrackUrl != "[s404] ZERO RESULTS" || !string.IsNullOrWhiteSpace(spotifyTrackUrl)) ?
                                MarkdownUtils.MakeLink("Spotify Link", spotifyTrackUrl, true) + " \u2197" /* + " \u2219 " + */ : "")
                          //MarkdownUtils.MakeLink("Deezer Link", "https://deezer.page.link/" + "", true) + " \u2197"
                    );

                    socketUserMessage = await ModifyOriginalResponseAsync(x => x.Content = sb.ToString().Trim());
                    conf!.SubmittedBangers++;
                    Config.Save();
                }
            }

            if (Context.Channel.Id == conf.ChannelId) {
                var upVote = conf.CustomUpvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomUpvoteEmojiName, conf.CustomUpvoteEmojiId) : Emote.Parse(conf.CustomUpvoteEmojiName) ?? Emote.Parse(":thumbsup:");
                var downVote = conf.CustomDownvoteEmojiId != 0 ? EmojiUtils.GetCustomEmoji(conf.CustomDownvoteEmojiName, conf.CustomDownvoteEmojiId) : Emote.Parse(conf.CustomDownvoteEmojiName) ?? Emote.Parse(":thumbsdown:");

                if (socketUserMessage is not null) {
                    if (conf is { AddUpvoteEmoji: true, UseCustomUpvoteEmoji: true })
                        await socketUserMessage.AddReactionAsync(upVote);
                    if (conf is { AddDownvoteEmoji: true, UseCustomDownvoteEmoji: true })
                        await socketUserMessage.AddReactionAsync(downVote);
                }

                conf.SubmittedBangers++;
                Config.Save();
            }
        }
        
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
        public async Task AddUrl([Summary("url", "URL to whitelist")] string url) {
            // await RespondAsync("This command is disabled, please contact Lily to add a URL to the whitelist.", ephemeral: true);

            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedUrls ??= [];
            if (_doesItExist(url, configBanger.WhitelistedUrls)) {
                await RespondAsync("URL already exists in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedUrls.Add(url);
            Config.Save();
            await RespondAsync($"Added {url} to the whitelist.");
        }

        [SlashCommand("removeurl", "Removes a URL from the whitelist")]
        public async Task RemoveUrl([Summary("url", "URL to remove from the whitelist")] string url) {
            // await RespondAsync("This command is disabled, please contact Lily to remove a URL from the whitelist.", ephemeral: true);

            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedUrls ??= [];
            if (!_doesItExist(url, configBanger.WhitelistedUrls)) {
                await RespondAsync("URL does not exist in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedUrls.Remove(url);
            Config.Save();
            await RespondAsync($"Removed {url} from the whitelist.");
        }

        [SlashCommand("addext", "Adds a file extension to the whitelist")]
        public async Task AddExt([Summary("extension", "File extension to whitelist")] string ext) {
            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedFileExtensions ??= [];
            if (ext.StartsWith('.'))
                ext = ext[1..];
            if (_doesItExist(ext, configBanger.WhitelistedFileExtensions)) {
                await RespondAsync("File extension already exists in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedFileExtensions.Add(ext);
            Config.Save();
            await RespondAsync($"Added {ext} to the whitelist.");
        }

        [SlashCommand("removeext", "Removes a file extension from the whitelist")]
        public async Task RemoveExt([Summary("extension", "File extension to remove from the whitelist")] string ext) {
            var configBanger = Config.GetGuildBanger(Context.Guild.Id);
            configBanger.WhitelistedFileExtensions ??= [];
            if (ext.StartsWith('.'))
                ext = ext[1..];
            if (!_doesItExist(ext, configBanger.WhitelistedFileExtensions)) {
                await RespondAsync("File extension does not exist in the whitelist.", ephemeral: true);
                return;
            }

            configBanger.WhitelistedFileExtensions.Remove(ext);
            Config.Save();
            await RespondAsync($"Removed {ext} from the whitelist.");
        }

        [SlashCommand("listeverything", "Lists all URLs and file extens"), RequireUserPermission(GuildPermission.SendMessages)]
        public async Task ListUrls([Summary("ephemeral", "Ephemeral response")] bool ephemeral = true) {
            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("Whitelisted URLs (Plain form):");
            Config.GetGuildBanger(Context.Guild.Id).WhitelistedUrls!.ForEach(s => sb.AppendLine($"- {s}"));
            // BangerListener.WhitelistedUrls!.ForEach(s => sb.AppendLine($"- {s}"));
            sb.AppendLine();
            sb.AppendLine("Whitelisted File Extensions:");
            Config.GetGuildBanger(Context.Guild.Id).WhitelistedFileExtensions!.ForEach(s => sb.AppendLine($"- .{s}"));
            sb.AppendLine("```");
            await RespondAsync(sb.ToString(), ephemeral: ephemeral);
        }

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

        [SlashCommand("offertoreplace", "Offer Spotify to YouTube replacement")]
        public async Task OfferReplace([Summary("toggle", "Enable or disable")] bool enabled) {
            Config.GetGuildBanger(Context.Guild.Id).OfferToReplaceSpotifyTrack = enabled;
            Config.Save();
            await RespondAsync($"Offer to replace Spotify track with YouTube {(enabled ? "enabled" : "disabled")}.");
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

        [SlashCommand("clearbangerinteractiondata", "(Bot Owner Only) Clears a select interaction data"), RequireOwner]
        public async Task ClearBangerInteractionData(
            [Summary("data-entry-id", "ID of the Data Entry")]
            string randomId = "",
            [Summary("ephemeral", "Ephemeral response")]
            bool ephemeral = false) {
            await DeferAsync(ephemeral);
            if (string.IsNullOrWhiteSpace(randomId)) {
                foreach (var iData in BangerListener.TheBangerInteractionData) {
                    if (iData.LookupMessage is not null)
                        await iData.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "Bot Owner forcefully deleted the Lookup Asking message." });
                    BangerListener.TheBangerInteractionData.Remove(iData);
                    await Task.Delay(TimeSpan.FromSeconds(0.25f));
                }

                return;
            }

            var data = BangerListener.TheBangerInteractionData.FirstOrDefault(x => x.RandomId == randomId);
            if (data is null) {
                await ModifyOriginalResponseAsync(x => x.Content = "Data not found.");
                return;
            }

            if (data.LookupMessage is not null)
                await data.LookupMessage.DeleteAsync(new RequestOptions { AuditLogReason = "Bot Owner forcefully deleted the Lookup Asking message." });
            BangerListener.TheBangerInteractionData.Remove(data);
        }
    }
}