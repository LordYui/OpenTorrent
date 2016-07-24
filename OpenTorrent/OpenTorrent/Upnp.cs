using System.Threading.Tasks;
using Open.Nat;
using System.Net;
using System;

namespace OpenTorrent
{
    class Upnp
    {
        NatDiscoverer m_Discoverer;
        public Upnp()
        {
            m_Discoverer = new NatDiscoverer();
        }

        public async Task<NatDevice> DiscoverDevice()
        {
            return await m_Discoverer.DiscoverDeviceAsync();
        }

        public async void Test()
        {
            try
            {
                NatDevice dev = await DiscoverDevice();
                IPAddress ip = await dev.GetExternalIPAsync();
                Logman.Log($"Found NAT device on {ip.ToString()}.");
                Mapping oldMap = await dev.GetSpecificMappingAsync(Protocol.Udp, 57300);
                if (oldMap?.PrivatePort != 57300)
                {
                    await dev.CreatePortMapAsync(new Mapping(Protocol.Udp, 57300, 57300, "OpenTorrent"));
                    Logman.Log($"Opened port 57300");
                }
                else
                {
                    Logman.Log("Already opened");
                }
            }
            catch(NatDeviceNotFoundException ex)
            {
                Logman.Log("Could not find a suitable UPnP device. Please enable UPnP or forward port 57300 manually.", LOG_TYPE.ERROR);
            }
            catch(Exception e)
            {
                Logman.Log("Unknown UPnP error, kys.", LOG_TYPE.ERROR);
            }
        }
    }
}