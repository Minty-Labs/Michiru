using Discord;
using Discord.Commands;
using Michiru.Configuration;
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
                    Text = $"v{Vars.Version}"
                },
                Timestamp = DateTime.Now
            }
            .AddField("Basic Commands", "```-ping\n-stats\n-minecraft```");
        if (Context.User.IsBotOwner()) {
            embed.AddField("Owner Commands", "```-setapikey <api> <key>\n-exec <command>```\n" +
                                             "*Only available in Minty Labs guild*\n" +
                                             "```/config rotatingstatus <enable/disable/list/next>\n" +
                                             "/config modifyrotatingstatus <add/update/remove> <activityType> <userStatus> <activityText>```");
        }

        if (Config.Base.Banger.Any(x => x.GuildId == Context.Guild.Id) && Context.User.IsSpecial(Context.Guild) || Context.User.IsBotOwner()) {
            embed.AddField("Banger Admin Commands", "```/banger toggle <true|false> - Toggles the banger system\n" +
                                                    "/banger setchannel <channel> - Sets the banger channel\n" +
                                                    "/banger addurl <url> - Adds a URL to the banger whitelist\n" +
                                                    "/banger removeurl <url> - Removes a URL from the banger whitelist" +
                                                    "/banger addext <ext> - Adds a file extension to the banger whitelist\n" +
                                                    "/banger removeext <ext> - Removes a file extension from the banger whitelist\n" +
                                                    "/banger addupvote <true|false> - Adds an upvote reaction to a banger post\n" +
                                                    "/banger adddownvote <true|false> - Adds a downvote reaction to a banger post\n" +
                                                    "/banger usecustomupvote <true|false> - Use a custom upvote emoji\n" +
                                                    "/banger usecustomdownvote <true|false> - Use a custom downvote emoji\n" +
                                                    "/banger setcustomupvote <name> <id> - Sets a custom upvote emoji\n" +
                                                    "/banger setcustomdownvote <name> <id> - Sets a custom downvote emoji\n" +
                                                    "/banger speakfreely <true|false> - Allow users to talk freely in the banger channel\n" +
                                                    "/banger listeverything - Lists all URLs and file extensions\n" +
                                                    "/banger getbangercount - Gets the number of bangers submitted in this guild```");
        }

        if (Config.Base.PersonalizedMember.Any(x => x.Guilds!.Any(y => y.GuildId == Context.Guild.Id)) || Context.User.IsBotOwner()) {
            embed.AddField("Personalized Member Commands", "```/personalization createrole <hexColor> - Creates a personalized role for you\n" +
                                                           "/personalization deleterole - Removes your personalized role\n" +
                                                           "/personalization updatecolor <newHexColor> - Updates your personalized role's color\n" +
                                                           "/personalization updatename <newName> - Updates your personalized role's name```");
            
            if (Context.User.IsSpecial(Context.Guild) || Context.User.IsBotOwner()) {
                embed.AddField("Personalized Members Admin Commands", "```/personalizationadmin toggle <true|false> - Toggles the personalized members system\n" +
                                                                      "/personalizationadmin setchannel <channel> - Sets a channel to only allow personalized member commands\n" +
                                                                      "/personalizationadmin setdefaultrole <role> - Sets the default role for users to be granted when they remove their personalized role\n" +
                                                                      "/personalizationadmin removedefaultrole - Removes the default role for your personalized role system\n" +
                                                                      "/personalizationadmin setresettime <number> - Sets the time in seconds for when a user's personalized role is reset\n" +
                                                                      "/personalizationadmin addroleto <user> <role> - Adds a role to the user as well as the personalized role system\n" +
                                                                      "/personalizationadmin removerolefrom <user> - Removes a role from the user as well as the personalized role system```");
            }
        }
        
        // TODO: GiveAway Commands

        await ReplyAsync(embed: embed.Build());
    }
}