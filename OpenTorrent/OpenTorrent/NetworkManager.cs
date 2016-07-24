using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lidgren.Network;
using System.Net;
using System.IO;

namespace OpenTorrent
{
    public enum MessageType : byte
    {
        UpdateTorrentList,
        NewClient
    }

    class NetworkManager
    {
        private NetPeer Peer;
        private NetPeerConfiguration Conf;

        private SynchronizationContext m_SyncContext;

        public event TorrentListReceivedHandler TorrentListReceived;
        public delegate void TorrentListReceivedHandler(Torrent[] torrents);

        public event NewClientConnectedHandler NewClientConnected;
        public delegate void NewClientConnectedHandler(NetConnection con);

        public NetworkManager()
        {
            Conf = new NetPeerConfiguration("P2P");
            Conf.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            Conf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Conf.AcceptIncomingConnections = true;
            Conf.Port = 57300;
            Peer = new NetPeer(Conf);

            m_SyncContext = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(m_SyncContext);

            Peer.RegisterReceivedCallback(new SendOrPostCallback(OnMessageReceived), SynchronizationContext.Current);

            Peer.Start();
            Logman.Log("Network manager done loading. Ready to receive messages.");
        }

        public void SendTorrentListUpdate(Torrent[] torList)
        {
            if (Peer.Connections.Count > 0)
            {
                NetOutgoingMessage newTorrentList = Peer.CreateMessage();
                byte[] torBytes = TorrentSerializer.Serialize(torList);
                newTorrentList.Write((byte)MessageType.UpdateTorrentList);
                newTorrentList.Write(torBytes);

                Peer.SendMessage(newTorrentList, Peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void SendTorrentListUpdate(Torrent[] torList, NetConnection target)
        {
            if (Peer.Connections.Count > 0)
            {
                NetOutgoingMessage newTorrentList = Peer.CreateMessage();
                byte[] torBytes = TorrentSerializer.Serialize(torList);
                newTorrentList.Write((byte)MessageType.UpdateTorrentList);
                newTorrentList.Write(torBytes);

                Peer.SendMessage(newTorrentList, target, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void Connect(string IP, int port)
        {
            Peer.Connect(IP, port, Peer.CreateMessage());
            Logman.Log("Connected");
        }

        public void BroadcastNewConnection(NetConnection con)
        {
            if (Peer.ConnectionsCount == 0)
                return;
            NetOutgoingMessage newMes = Peer.CreateMessage();
            byte[] ipBytes = con.RemoteEndPoint.Address.GetAddressBytes();
            newMes.Write((byte)MessageType.NewClient);
            newMes.Write(ipBytes.Length);
            newMes.Write(ipBytes);
            newMes.Write(con.RemoteEndPoint.Port);

            Peer.SendMessage(newMes, Peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }

        private void OnMessageReceived(object state)
        {
            NetPeer sender = (NetPeer)state;
            NetIncomingMessage newMes = sender.ReadMessage();
            Parse(newMes);
        }

        public void Parse(NetIncomingMessage mes)
        {
            switch (mes.MessageType)
            {
                case NetIncomingMessageType.ConnectionApproval:
                    mes.SenderConnection.Approve();
                    break;
                case NetIncomingMessageType.WarningMessage:
                    Logman.Log(mes.ReadString());
                    break;
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus stat = (NetConnectionStatus)mes.ReadByte();
                    Logman.Log(mes.SenderEndPoint.Address.ToString() + ": " + stat.ToString(), LOG_TYPE.WARNING);
                    if (stat == NetConnectionStatus.Connected)
                    {
                        Logman.Log("New client connected, broadcasting connection.");
                        BroadcastNewConnection(mes.SenderConnection);
                        if (NewClientConnected != null)
                            NewClientConnected.Invoke(mes.SenderConnection);
                    }
                    break;
                case NetIncomingMessageType.Data:
                    ParseData(mes);
                    break;
            }
        }

        private void ParseData(NetIncomingMessage mes)
        {
            MessageType messageType = (MessageType)mes.ReadByte();
            switch (messageType)
            {
                case MessageType.NewClient:
                    // mes.SenderEndPoint.Address.GetHashCode() != Peer.Configuration.LocalAddress.GetHashCode()
                    //    && mes.SenderEndPoint.GetHashCode() != Peer.Socket.RemoteEndPoint.GetHashCode()
                    //    &&
                    int ipLength = mes.ReadInt32();
                    IPAddress clientIP = new IPAddress(mes.ReadBytes(ipLength));
                    int clientPort = mes.ReadInt32();
                    IPEndPoint newClientEndpoint = new IPEndPoint(clientIP, clientPort);

                    if(newClientEndpoint != Peer.Socket.LocalEndPoint)
                        Peer.Connect(newClientEndpoint, Peer.CreateMessage());
                    Logman.Log("Successfully connected to a new client.");

                    break;
                case MessageType.UpdateTorrentList:
                    Logman.Log($"Received new torrent list from {mes.SenderEndPoint.ToString()}");
                    Torrent[] newTorrents = TorrentSerializer.Deserialize(mes.Data);
                    if (TorrentListReceived != null)
                        TorrentListReceived.Invoke(newTorrents);
                    break;
            }
        }
    }
}