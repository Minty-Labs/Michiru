namespace WakeOnLanCSharp;

public class WakeOnLan {
    public int PortNumber { get; private set; }
    private static UDPSender _udpSender;
    
    public WakeOnLan(int portNumber) {
        PortNumber = portNumber;
        _udpSender = new UDPSender(portNumber);
    }

    public async Task SendMagicPacket(byte[] macAddress, string ipAddress, int tries = 3, int intervalMilliSeconds = 100) {
        var magic = BuildMagicPacket(macAddress);
        for (var i = 0; i < tries; i++) {
            await _udpSender.SendAsync(ipAddress, magic.Length, magic);
            if (intervalMilliSeconds > 0) await Task.Delay(intervalMilliSeconds);
        }
    }
    
    private static byte[] BuildMagicPacket(byte[] macAddress) {
        if (macAddress.Length != 6) {
            var exception = new ArgumentException {
                HelpLink = null,
                HResult = 0,
                Source = null
            };
            throw exception;
        }

        var magic = new List<byte>();
        for (var i = 0; i < 6; i++) 
            magic.Add(0xff);

        for (var i = 0; i < 16; i++) {
            for (var j = 0; j < 6; j++)
                magic.Add(macAddress[j]);
        }
        return magic.ToArray();
    }
    
    public static byte[] ParseMacAddress(string text, char[]? separator = null) {
        separator ??= [ ':', '-' ];
        var tokens = text.Split(separator);
        var bytes = new byte[6];
        for (var i = 0; i < 6; i++)
            bytes[i] = Convert.ToByte(tokens[i], 16);
        return bytes;
    }
}