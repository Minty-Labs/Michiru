using Discord.WebSocket;
using Serilog;

namespace Michiru.Events;

public static class MemberUpdated {
    internal static async Task MemberJoin(SocketThreadUser user) {
        var logger = Log.ForContext("SourceContext", "EVENT:MemberJoin");
        // logger.Information("User {User} joined the server.", user.Username);
        if (user.Guild.Id != 1083619886980403272) return;
        var channel = user.Guild.GetChannel(1084581543009333298);
        if (channel is not SocketTextChannel textChannel) return;
        await textChannel.SendMessageAsync($"{user.Mention} ({user.Id}) joined the server.");
    }
    
    internal static async Task MemberLeave(SocketThreadUser user) {
        var logger = Log.ForContext("SourceContext", "EVENT:MemberLeave");
        // logger.Information("User {User} left the server.", user.Username);
        if (user.Guild.Id != 1083619886980403272) return;
        var channel = user.Guild.GetChannel(1084581543009333298);
        if (channel is not SocketTextChannel textChannel) return;
        await textChannel.SendMessageAsync($"{user.Mention} ({user.Id}) left the server.");
    }
}