using Discord;
using Michiru.Utils;

namespace Michiru.Managers; 

public static class ErrorSending {
    private static EmbedBuilder? ErrorEmbed(object message, object? exception = null) {
        var msg = message.ToString();
        var finalMsg = msg!.Length > 2000 ? msg[..1990] + "..." : msg;
        if (finalMsg.AndContainsMultiple("unauthorized", "403"))
            return null;

        return new EmbedBuilder {
            Color = Color.Red,
            Description = 
                exception != null
                    ? $"{finalMsg}\n{MarkdownUtils.ToCodeBlockMultiline(exception.ToString() ?? "empty exception")}"
                    : finalMsg,
            Footer = new EmbedFooterBuilder {
                Text = Vars.VersionStr
            },
            Timestamp = DateTime.Now
        };
    }

    public static async Task SendErrorToLoggingChannelAsync(object message, MessageReference? reference = null) => await Program.Instance.ErrorLogChannel.SendMessageAsync(embed: ErrorEmbed(message)!.Build(), messageReference: reference);

    public static void SendErrorToLoggingChannel(object message, MessageReference? reference = null) => SendErrorToLoggingChannelAsync(message, reference).GetAwaiter().GetResult();
    
    public static async Task SendErrorToLoggingChannelAsync(object message, MessageReference? reference = null, object? obj = null) => await Program.Instance.ErrorLogChannel.SendMessageAsync(embed: ErrorEmbed(message, obj)!.Build(), messageReference: reference);

    public static void SendErrorToLoggingChannel(object message, MessageReference? reference = null, object? obj = null) => SendErrorToLoggingChannelAsync(message, reference, obj).GetAwaiter().GetResult();
}