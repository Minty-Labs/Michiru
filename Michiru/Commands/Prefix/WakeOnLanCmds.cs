using System.Net.NetworkInformation;
using System.Text;
using Discord.Commands;
using Michiru.Configuration._Base_Bot;
using Michiru.Configuration._Base_Bot.Classes;

namespace Michiru.Commands.Prefix;

[RequireContext(ContextType.Guild | ContextType.DM)]
public class WakeOnLanCmds : ModuleBase<SocketCommandContext> {
    [Command("wol"), RequireOwner]
    public async Task WakeOnLan(string deviceIdentifier) {
        var wol = Config.Base.WakeOnLan.FirstOrDefault(x => x.DeviceIdentifier == deviceIdentifier);
        if (wol is null) {
            await ReplyAsync("Device not found.");
            return;
        }
        
        var lanObject = new WakeOnLanCSharp.WakeOnLan(wol.PortNumber);

        var macAddress = WakeOnLanCSharp.WakeOnLan.ParseMacAddress(wol.MacAddress);
        await lanObject.SendMagicPacket(macAddress, wol.IpAddress);
        await ReplyAsync("Magic packet sent.");
        await ReplyAsync("Waiting a little bit before pinging.");
        await Task.Delay(TimeSpan.FromSeconds(10));
        await ReplyAsync("Pinging device.");
        var myPing = new Ping();
        var reply = myPing.Send(wol.IpAddress, 1000);
        await ReplyAsync(reply.Status == IPStatus.Success ? "Device is online." : "Device is offline.");
    }
    
    [Command("pingwol"), RequireOwner]
    public async Task PingWakeOnLan(string deviceIdentifier) {
        var isIp = deviceIdentifier.Contains('.');
        var wol = Config.Base.WakeOnLan.FirstOrDefault(x => isIp ? x.IpAddress == deviceIdentifier : x.DeviceIdentifier == deviceIdentifier);
        if (wol is null) {
            await ReplyAsync("Device not found.");
            return;
        }
        
        var myPing = new Ping();
        var reply = myPing.Send(wol.IpAddress, 1000);
        await ReplyAsync(reply.Status == IPStatus.Success ? "Device is online." : "Device is offline.");
    }
    
    [Command("addwol"), RequireOwner]
    public async Task AddWakeOnLan(string deviceIdentifier, int portNumber, string ipAddress, string macAddress) {
        if (string.IsNullOrWhiteSpace(deviceIdentifier) || portNumber <= 0 || string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(macAddress)) {
            await ReplyAsync("Invalid parameters.\nUsage: `addwol <deviceIdentifier> <portNumber> <ipAddress> <macAddress>`");
            return;
        }
        
        var wol = new WakeOnLanConf {
            DeviceIdentifier = deviceIdentifier,
            PortNumber = portNumber,
            IpAddress = ipAddress,
            MacAddress = macAddress
        };
        Config.Base.WakeOnLan.Add(wol);
        Config.Save();
        await ReplyAsync("Device added.");
    }
    
    [Command("removewol"), RequireOwner]
    public async Task RemoveWakeOnLan(string deviceIdentifier) {
        var wol = Config.Base.WakeOnLan.FirstOrDefault(x => x.DeviceIdentifier == deviceIdentifier);
        if (wol is null) {
            await ReplyAsync("Device not found.");
            return;
        }
        
        Config.Base.WakeOnLan.Remove(wol);
        Config.Save();
        Config.SaveFile();
        await ReplyAsync("Device removed.");
    }
    
    [Command("listwol"), RequireOwner]
    public async Task ListWakeOnLan() {
        var sb = new StringBuilder();
        foreach (var wol in Config.Base.WakeOnLan) {
            sb.AppendLine($"- Device: {wol.DeviceIdentifier}\n- Port: {wol.PortNumber}\n- IP: {wol.IpAddress}\n- MAC: {wol.MacAddress}\n");
        }
        await ReplyAsync(sb.ToString());
    }
}