﻿namespace Michiru.Configuration.Classes;

public class WakeOnLanConf {
    public string DeviceIdentifier { get; init; }
    public int PortNumber { get; init; }
    public string IpAddress { get; init; }
    public string MacAddress { get; init; }
}