using Discord;
using Discord.Commands;
using Michiru.Configuration;
using Michiru.Configuration._Base_Bot;
using Michiru.Utils;
using static Michiru.Commands.Preexecution.UserExtensions;

namespace Michiru.Commands.Prefix;

[RequireContext(ContextType.Guild)]
public class HelpCmd : ModuleBase<SocketCommandContext> {
    [Command("help")]
    public async Task Help() {
        var embed = new EmbedBuilder {
                Title = "Help",
                Description = "Commands, what else did you expect?",
                Color = Colors.HexToColor("9fffe3"),
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Footer = new EmbedFooterBuilder {
                    Text = $"v{Vars.VersionStr}"
                },
                Timestamp = DateTime.Now
            }
            .AddField("Basic Commands (prefix: `-`)", MarkdownUtils.ToCodeBlockMultiline("ping, stats"))
            .AddField("Basic Slash Commands", MarkdownUtils.ToCodeBlockMultiline("/serverinfo"));
        if (Context.User.IsBotOwner()) {
            embed.AddField("Owner Commands", MarkdownUtils.ToCodeBlockMultiline(
                                                 "-exec <command>\n" +
                                                 "/setapikey\n") +
                                             "*Only available in Minty Labs guild*\n" +
                                             MarkdownUtils.ToCodeBlockMultiline("/config rotatingstatus <enable/disable/list/next>\n" +
                                                                                    "/config modifyrotatingstatus <add/update/remove> <activityType> <userStatus> <activityText>\n" +
                                                                                    "-wol <deviceIdentifier>\n" +
                                                                                    "-addwol <deviceIdentifier> <portNumber> <ipAddress> <macAddress>\n" +
                                                                                    "-pingwol <deviceIdentifier>\n" +
                                                                                    "-listwol\n" +
                                                                                    "-remove <deviceIdentifier>"));
        }

        if (Config.Base.Banger.Any(x => x.GuildId == Context.Guild.Id) && Context.User.IsSpecial(Context.Guild) || Context.User.IsBotOwner()) {
            embed.AddField("Banger Commands", MarkdownUtils.ToCodeBlockMultiline("/banger leaderboard - Lists the top guilds with the most bangers\n" +
                                                                                 "/banger getbangercount - Gets the number of bangers submitted in this guild\n"));
            
            // banger admin commands with descriptions
            embed.AddField("Banger Admin Commands", MarkdownUtils.ToCodeBlockMultiline("/bangeradmin toggle <true|false> - Toggles the banger system\n" +
                                                                                           "/bangeradmin setchannel <channel> - Sets the banger channel\n" +
                                                                                           "/bangeradmin addurl <url> - Adds a URL to the banger whitelist\n" +
                                                                                           "/bangeradmin removeurl <url> - Removes a URL from the banger whitelist\n" +
                                                                                           "/bangeradmin addext <ext> - Adds a file extension to the banger whitelist\n" +
                                                                                           "/bangeradmin removeext <ext> - Removes a file extension from the banger whitelist\n" +
                                                                                           "/bangeradmin addupvote <true|false> - Adds an upvote reaction to a banger post\n" +
                                                                                           "/bangeradmin adddownvote <true|false> - Adds a downvote reaction to a banger post\n" +
                                                                                           "/bangeradmin usecustomupvote <true|false> - Use a custom upvote emoji\n" +
                                                                                           "/bangeradmin usecustomdownvote <true|false> - Use a custom downvote emoji\n" +
                                                                                           "/bangeradmin setcustomupvote <name> <id> - Sets a custom upvote emoji\n" +
                                                                                           "/bangeradmin setcustomdownvote <name> <id> - Sets a custom downvote emoji\n" +
                                                                                           "/bangeradmin speakfreely <true|false> - Allow users to talk freely in the banger channel\n" +
                                                                                           "/bangeradmin listeverything - Lists all URLs and file extensions\n" +
                                                                                           "/bangeradmin embedsuppression <true|false> - Suppresses the original poster's embed instead of deleting the message\n" +
                                                                                           $"{(Context.User.IsBotOwner() ? "/bangeradmin modifybangercount - Modifies banger count\n" : "")}" +
                                                                                           $"{(Context.User.IsBotOwner() ? "/bangeradmin clearbangerinteractiondata - Clears a select interaction data\n" : "")}"));
        }

        if (Config.Base.PersonalizedMember.Any(x => x.Guilds!.Any(y => y.GuildId == Context.Guild.Id)) || Context.User.IsBotOwner()) {
            embed.AddField("Personalized Member Commands", MarkdownUtils.ToCodeBlockMultiline("/personalization createrole - Creates a personalized role for you\n" +
                                                                                                  "/personalization updaterole - Updates your personalized role\n" +
                                                                                                  "/personalization deleterole - Removes your personalized role"));

            if (Context.User.IsSpecial(Context.Guild) || Context.User.IsBotOwner()) {
                embed.AddField("Personalized Members Admin Commands", MarkdownUtils.ToCodeBlockMultiline("/personalizationadmin toggle <true|false> - Toggles the personalized members system\n" +
                                                                                                             "/personalizationadmin setchannel <channel> - Sets a channel to only allow personalized member commands\n" +
                                                                                                             "/personalizationadmin setdefaultrole <role> - Sets the default role for users to be granted when they remove their personalized role\n" +
                                                                                                             "/personalizationadmin removedefaultrole - Removes the default role for your personalized role system\n" +
                                                                                                             "/personalizationadmin setresettime <number> - Sets the time in seconds for when a user's personalized role is reset\n" +
                                                                                                             "/personalizationadmin addroleto <user> <role> - Adds a role to the user as well as the personalized role system\n" +
                                                                                                             "/personalizationadmin removerolefrom <user> - Removes a role from the user as well as the personalized role system"));
            }
        }

        await ReplyAsync(embed: embed.Build());
    }
}