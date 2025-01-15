using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Michiru.Configuration.Moderation;
using Michiru.Configuration.Moderation.Classes;
using Michiru.Utils;

namespace Michiru.Commands.Prefix;

[RequireContext(ContextType.Guild)]
public class Moderation : ModuleBase<SocketCommandContext> {
    [Command("ban"), RequireUserPermission(GuildPermission.BanMembers | GuildPermission.Administrator)]
    public async Task Ban(IGuildUser user, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var channel = await ModData.GetBanOrUnbanLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "User Banned",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        ModData.CurrentId++;
        var moderation = new Michiru.Configuration.Moderation.Classes.Moderation {
            Id = ModData.CurrentId,
            Type = "ban",
            IsPardoned = false,
            UserId = user.Id,
            GuildId = guild.Id,
            Reason = reason,
            DateTimeGiven = DateTime.Now
        };
        ModData.Base.Users.FirstOrDefault(x => x.Id == user.Id)?.Moderation.Add(moderation);
        ModData.Save();
        
        await guild.AddBanAsync(user, 7, reason);
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
    }
    
    [Command("preban"), RequireUserPermission(GuildPermission.BanMembers | GuildPermission.Administrator)]
    public async Task Preban(ulong userId, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var channel = await ModData.GetBanOrUnbanLogChannel(guild) as ITextChannel;
        var user = await Context.Client.Rest.GetUserAsync(userId);
        var embed = new EmbedBuilder {
            Title = "User Prebanned",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        ModData.CurrentId++;
        var moderation = new Michiru.Configuration.Moderation.Classes.Moderation {
            Id = ModData.CurrentId,
            Type = "ban",
            IsPardoned = false,
            UserId = user.Id,
            GuildId = guild.Id,
            Reason = reason,
            DateTimeGiven = DateTime.Now
        };
        ModData.Base.Users.FirstOrDefault(x => x.Id == user.Id)?.Moderation.Add(moderation);
        ModData.Save();
        
        await guild.AddBanAsync(userId, 7, reason);
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
    }
    
    [Command("unban"), RequireUserPermission(GuildPermission.BanMembers | GuildPermission.Administrator)]
    public async Task Unban(ulong userId, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var channel = await ModData.GetBanOrUnbanLogChannel(guild) as ITextChannel;
        var user = await Context.Client.Rest.GetUserAsync(userId);
        var embed = new EmbedBuilder {
            Title = "User Unbanned",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        await guild.RemoveBanAsync(userId);
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
    }
    
    [Command("kick"), RequireUserPermission(GuildPermission.KickMembers | GuildPermission.Administrator)]
    public async Task Kick(IGuildUser user, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var channel = await ModData.GetKickWarnTimeoutMuteLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "User Kicked",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        ModData.CurrentId++;
        var moderation = new Michiru.Configuration.Moderation.Classes.Moderation {
            Id = ModData.CurrentId,
            Type = "kick",
            IsPardoned = false,
            UserId = user.Id,
            GuildId = guild.Id,
            Reason = reason,
            DateTimeGiven = DateTime.Now
        };
        ModData.Base.Users.FirstOrDefault(x => x.Id == user.Id)?.Moderation.Add(moderation);
        ModData.Save();
        
        await user.KickAsync(reason);
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
    }
    
    /*[Command("mute"), RequireUserPermission(GuildPermission.MuteMembers | GuildPermission.Administrator)]
    public async Task Mute(IGuildUser user, [Remainder] string? reason = null) {
        //
    }
    
    [Command("unmute"), RequireUserPermission(GuildPermission.MuteMembers | GuildPermission.Administrator)]
    public async Task Unmute(IGuildUser user, [Remainder] string? reason = null) {
        //
    }*/
    
    [Command("warn"), RequireUserPermission(GuildPermission.KickMembers | GuildPermission.Administrator)]
    public async Task Warn(IGuildUser user, [Remainder] string? reason = null) {
        if (string.IsNullOrWhiteSpace(reason)) {
            await ReplyAsync("You must provide a reason.");
            return;
        }
        var guild = Context.Guild;
        var channel = await ModData.GetKickWarnTimeoutMuteLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "User Warned",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        ModData.CurrentId++;
        var moderation = new Michiru.Configuration.Moderation.Classes.Moderation {
            Id = ModData.CurrentId,
            Type = "warn",
            IsPardoned = false,
            UserId = user.Id,
            GuildId = guild.Id,
            Reason = reason,
            DateTimeGiven = DateTime.Now
        };
        ModData.Base.Users.FirstOrDefault(x => x.Id == user.Id)?.Moderation.Add(moderation);
        ModData.Save();
        
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
        
        if (ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id)!.DMOnWarn)
            await user.SendMessageAsync(embed: embed.Build());
    }
    
    [Command("pardon")] // permission check is done in the command
    public async Task Pardon(ulong moderationId) {
        var moderation = ModData.Base.Users.SelectMany(x => x.Moderation).FirstOrDefault(x => x.Id == (int)moderationId);
        if (moderation is null) {
            await ReplyAsync("No moderation data found for this ID.");
            return;
        }
        
        var isWarn = moderation.Type == "warn";
        var isBan = moderation.Type == "ban";
        var isKick = moderation.Type == "kick";
        var isMute = moderation.Type == "mute";
        var isTimeout = moderation.Type == "timeout";
        
        var hasWarnPermission = ((SocketGuildUser)Context.User).GuildPermissions.Has(GuildPermission.KickMembers);
        var hasBanPermission = ((SocketGuildUser)Context.User).GuildPermissions.Has(GuildPermission.BanMembers);
        var hasMutePermission = ((SocketGuildUser)Context.User).GuildPermissions.Has(GuildPermission.MuteMembers);
        if ((hasWarnPermission && (isWarn || isKick)) || (hasBanPermission && isBan) || (hasMutePermission && (isMute || isTimeout))) {
            await ReplyAsync("You do not have the required permissions to use this command.");
            return;
        }
        
        moderation.IsPardoned = true;
        ModData.Save();
        // create embed of moderation data
        var embed = new EmbedBuilder {
            Title = "Moderation Pardoned",
            Description = $"{MarkdownUtils.ToBold("Moderation ID")}: {moderation.Id}\n" +
                          $"{MarkdownUtils.ToBold("Moderation Type")}: {moderation.Type}\n" +
                          $"{MarkdownUtils.ToBold("User")}: {Context.Client.Rest.GetUserAsync(moderation.UserId).Result.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.Client.Rest.GetUserAsync(moderation.GuildId).Result.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {moderation.Reason ?? "No reason provided."}",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        await ReplyAsync(embed: embed.Build());
    }
    
    /*[Command("crossban"), RequireUserPermission(GuildPermission.BanMembers | GuildPermission.Administrator)]
    public async Task Crossban(IGuildUser user, [Remainder] string? reason = null) {
        //
    }
    
    [Command("scamban"), RequireUserPermission(GuildPermission.BanMembers | GuildPermission.Administrator)]
    public async Task Scamban(IGuildUser user, [Remainder] string? reason = null) {
        //
    }*/
    
    [Command("channelfreeze"), RequireUserPermission(GuildPermission.ManageChannels | GuildPermission.Administrator)]
    public async Task ChannelFreeze(IGuildChannel channel, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);

        if (config is null || !config.IsChannelFreezeEnabled) {
            await ReplyAsync("Channel freezing is not enabled in this server.");
            return;
        }
        
        var logChannel = await ModData.GetFreezeSlowmodeMasspurgeLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "Channel Frozen",
            Description = $"{MarkdownUtils.ToBold("Channel")}: <#{channel.Id}>\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        // get the channel's permissions
        var permissions = channel.GetPermissionOverwrite(guild.EveryoneRole);
        // if the channel is already frozen, unfreeze it
        if (permissions is { SendMessages: PermValue.Deny }) {
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Inherit));
            if (logChannel is not null)
                await logChannel.SendMessageAsync(embed: embed.Build());
            return;
        }
        
        // freeze the channel
        await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
        if (logChannel is not null)
            await logChannel.SendMessageAsync(embed: embed.Build());
    }
    
    [Command("channelunfreeze"), RequireUserPermission(GuildPermission.ManageChannels | GuildPermission.Administrator)]
    public async Task ChannelUnfreeze(IGuildChannel channel, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);

        if (config is null || !config.IsChannelFreezeEnabled) {
            await ReplyAsync("Channel freezing is not enabled in this server.");
            return;
        }
        
        var logChannel = await ModData.GetFreezeSlowmodeMasspurgeLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "Channel Unfrozen",
            Description = $"{MarkdownUtils.ToBold("Channel")}: <#{channel.Id}>\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Green,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        // get the channel's permissions
        var permissions = channel.GetPermissionOverwrite(guild.EveryoneRole);
        // if the channel is already unfrozen, freeze it
        if (permissions is { SendMessages: PermValue.Inherit }) {
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
            if (logChannel is not null)
                await logChannel.SendMessageAsync(embed: embed.Build());
            return;
        }
        
        // unfreeze the channel
        await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Inherit));
        if (logChannel is not null)
            await logChannel.SendMessageAsync(embed: embed.Build());
    }
    
    [Command("tempban"), RequireUserPermission(GuildPermission.BanMembers | GuildPermission.Administrator)]
    public async Task Tempban(IGuildUser user, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);

        if (config is null || !config.IsScamBanEnabled) {
            await ReplyAsync("Temp/Scam banning is not enabled in this server.");
            return;
        }
        
        var channel = await ModData.GetBanOrUnbanLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "User Tempbanned",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        ModData.CurrentId++;
        var moderation = new Michiru.Configuration.Moderation.Classes.Moderation {
            Id = ModData.CurrentId,
            Type = "ban",
            IsPardoned = false,
            UserId = user.Id,
            GuildId = guild.Id,
            Reason = reason,
            DateTimeGiven = DateTime.Now
        };
        ModData.Base.Users.FirstOrDefault(x => x.Id == user.Id)?.Moderation.Add(moderation);
        ModData.Save();
        
        await guild.AddBanAsync(user, 7, reason);
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
        await Task.Delay(TimeSpan.FromSeconds(2));
        await guild.RemoveBanAsync(user);
    }
    
    [Command("Timeout"), RequireUserPermission(GuildPermission.MuteMembers | GuildPermission.Administrator)]
    public async Task Timeout(IGuildUser user, TimeSpan duration, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var channel = await ModData.GetKickWarnTimeoutMuteLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "User Timed Out",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        ModData.CurrentId++;
        var moderation = new Michiru.Configuration.Moderation.Classes.Moderation {
            Id = ModData.CurrentId,
            Type = "timeout",
            IsPardoned = false,
            UserId = user.Id,
            GuildId = guild.Id,
            Reason = reason,
            DateTimeGiven = DateTime.Now
        };
        ModData.Base.Users.FirstOrDefault(x => x.Id == user.Id)?.Moderation.Add(moderation);
        ModData.Save();

        var time = DateTime.Now.Add(duration);
        await user.ModifyAsync(x => x.TimedOutUntil = new Optional<DateTimeOffset?>(time));
        
        if (channel is not null)
            await channel.SendMessageAsync(embed: embed.Build());
    }
    
    /*[Command("masspurge"), RequireUserPermission(GuildPermission.ManageMessages | GuildPermission.Administrator)]
    public async Task Masspurge(IGuildUser user, IGuildChannel channel, [Remainder] string? reason = null) {
        var guild = Context.Guild;
        var logChannel = await ModData.GetFreezeSlowmodeMasspurgeLogChannel(guild) as ITextChannel;
        var embed = new EmbedBuilder {
            Title = "User Masspurged",
            Description = $"{MarkdownUtils.ToBold("User")}: {user.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Moderator")}: {Context.User.Mention}\n" +
                          $"{MarkdownUtils.ToBold("Reason")}: {reason ?? "No reason provided."}",
            Color = Color.Red,
            Footer = new EmbedFooterBuilder {
                Text = $"v{Vars.VersionStr}",
                IconUrl = Program.Instance.Client.CurrentUser.GetAvatarUrl()
            }
        };
        
        var _chan = channel as ITextChannel;
        var messages = _chan.GetMessagesAsync(200).ToListAsync().GetAwaiter().GetResult();
        foreach (var message in messages) {
            if (message.Author.Id == user.Id) {
                await message.DeleteAsync();
            }
        }
        
        if (logChannel is not null)
            await logChannel.SendMessageAsync(embed: embed.Build());
    }*/
    
    // =================================================================================================================

    [Command("setbanlogchannel"), RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    public async Task SetLogChannelBan(ITextChannel channel) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);
        if (guild is null) {
            var build = new Guild {
                Id = guild.Id,
                Channels = new Channel {
                    BanOrUnban = channel.Id,
                    FreezeSlowmodeMasspurge = 0,
                    KickWarnTimeoutMute = 0
                },
                IsCrossbanEnabled = false,
                IsScamBanEnabled = false,
                IsChannelFreezeEnabled = false
            };
            ModData.Base.Guilds.Add(build);
        }
        else {
            var channels = config!.Channels;
            channels.BanOrUnban = channel.Id;
        }
        ModData.Save();
        await ReplyAsync($"Set Ban/Unban log channel to {channel.Mention}.");
    }
    
    [Command("setkicklogchannel"), RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    public async Task SetLogChannelKick(ITextChannel channel) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);
        if (guild is null) {
            var build = new Guild {
                Id = guild.Id,
                Channels = new Channel {
                    BanOrUnban = 0,
                    FreezeSlowmodeMasspurge = 0,
                    KickWarnTimeoutMute = channel.Id
                },
                IsCrossbanEnabled = false,
                IsScamBanEnabled = false,
                IsChannelFreezeEnabled = false
            };
            ModData.Base.Guilds.Add(build);
        }
        else {
            var channels = config!.Channels;
            channels.KickWarnTimeoutMute = channel.Id;
        }
        ModData.Save();
        await ReplyAsync($"Set Kick/Warn/Timeout/~~Mute~~ log channel to {channel.Mention}.");
    }
    
    [Command("setfreezelogchannel"), RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    public async Task SetLogChannelFreeze(ITextChannel channel) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);
        if (guild is null) {
            var build = new Guild {
                Id = guild.Id,
                Channels = new Channel {
                    BanOrUnban = 0,
                    FreezeSlowmodeMasspurge = channel.Id,
                    KickWarnTimeoutMute = 0
                },
                IsCrossbanEnabled = false,
                IsScamBanEnabled = false,
                IsChannelFreezeEnabled = false
            };
            ModData.Base.Guilds.Add(build);
        }
        else {
            var channels = config!.Channels;
            channels.FreezeSlowmodeMasspurge = channel.Id;
        }
        ModData.Save();
        await ReplyAsync($"Set Freeze/Unfreeze/~~Slowmode/Masspurge~~ log channel to {channel.Mention}.");
    }

    [Command("togglecrossban"), RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    public async Task ToggleCrossban(bool toggle) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);
        if (guild is null) {
            var build = new Guild {
                Id = guild.Id,
                Channels = new Channel {
                    BanOrUnban = 0,
                    FreezeSlowmodeMasspurge = 0,
                    KickWarnTimeoutMute = 0
                },
                IsCrossbanEnabled = toggle,
                IsScamBanEnabled = false,
                IsChannelFreezeEnabled = false
            };
            ModData.Base.Guilds.Add(build);
        }
        else {
            config!.IsCrossbanEnabled = toggle;
        }
        ModData.Save();
        await ReplyAsync($"Crossban is now {(toggle ? "enabled" : "disabled")}.\n" +
                         MarkdownUtils.ToSubText("Crossbanning is not yet functional"));
    }
    
    [Command("togglescamban"), RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    public async Task ToggleScamBan(bool toggle) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);
        if (guild is null) {
            var build = new Guild {
                Id = guild.Id,
                Channels = new Channel {
                    BanOrUnban = 0,
                    FreezeSlowmodeMasspurge = 0,
                    KickWarnTimeoutMute = 0
                },
                IsCrossbanEnabled = false,
                IsScamBanEnabled = toggle,
                IsChannelFreezeEnabled = false
            };
            ModData.Base.Guilds.Add(build);
        }
        else {
            config!.IsScamBanEnabled = toggle;
        }
        ModData.Save();
        await ReplyAsync($"ScamBan is now {(toggle ? "enabled" : "disabled")}.");
    }
    
    [Command("togglechannelfreeze"), RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    public async Task ToggleChannelFreeze(bool toggle) {
        var guild = Context.Guild;
        var config = ModData.Base.Guilds.FirstOrDefault(x => x.Id == guild.Id);
        if (guild is null) {
            var build = new Guild {
                Id = guild.Id,
                Channels = new Channel {
                    BanOrUnban = 0,
                    FreezeSlowmodeMasspurge = 0,
                    KickWarnTimeoutMute = 0
                },
                IsCrossbanEnabled = false,
                IsScamBanEnabled = false,
                IsChannelFreezeEnabled = toggle
            };
            ModData.Base.Guilds.Add(build);
        }
        else {
            config!.IsChannelFreezeEnabled = toggle;
        }
        ModData.Save();
        await ReplyAsync($"Channel Freeze is now {(toggle ? "enabled" : "disabled")}.");
    }
}