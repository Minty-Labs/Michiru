using System.Net.Sockets;

namespace WakeOnLanCSharp;

public class UDPSender {
    public const int DefaultBufferSize = 8192;
    private readonly int _portNum;
    private UdpClient _udpClient;
    private readonly SemaphoreSlim _locker = new (1,1);

    public UDPSender(int portNum, int bufferSize = DefaultBufferSize) {
        _portNum = portNum;
        _udpClient = new UdpClient();
        _udpClient.Client.SendBufferSize = bufferSize;
        _udpClient.Client.ReceiveBufferSize = bufferSize;
    }

    ~UDPSender() => Close();

    public void Close() {
        _udpClient.Close();
        _udpClient = null;
    }

    public async Task SendAsync(string ip, int size, byte[] data) {
        try {
            await _locker.WaitAsync();
            await _udpClient.SendAsync(data, size, ip, _portNum);
        }
        finally {
            _locker.Release();
        }
    }
}