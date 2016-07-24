using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTorrent
{
    class Program
    {
        static bool Running = true;

        static NetworkManager networkManager;
        static TorrentManager torrentManager;
        static Upnp UPnPManager;

        static void Main(string[] args)
        {
            networkManager = new NetworkManager();
            torrentManager = new TorrentManager();
            UPnPManager = new Upnp();

            Task testUPNP = new Task(UPnPManager.Test);
            testUPNP.Start();
            testUPNP.Wait();

            networkManager.TorrentListReceived += NetworkManager_TorrentListReceived;
            networkManager.NewClientConnected += NetworkManager_NewClientConnected;
            torrentManager.TorrentUpdateThresholdReached += TorrentManager_TorrentUpdateThresholdReached;

            Torrent[] loadedTorrents = Database.Load();
            if (loadedTorrents != null)
                torrentManager.torrentList.AddRange(loadedTorrents);

            if(Directory.Exists("bulk"))
            {
                Logman.Log("Found a bulk/ folder, import all torrents inside ? (y/*)");
                if (Console.ReadLine().ToLower() == "y")
                    BulkLoad();
            }

            Logman.Log("Waiting for input, type ? or help to see a list of commands.");
            while (Running)
            {
                //Console.Write("> ");
                string[] userInput = Console.ReadLine().Split(' ');
                switch (userInput[0])
                {
                    case "exit":
                    case "quit":
                    case "stop":
                        Running = false;
                        break;
                    case "?":
                    case "help":
                        Console.WriteLine("No help for now.");
                        break;
                    case "add":
                    case "send":
                        string torrentPath = string.Join(" ", userInput.Skip(1));
                        torrentManager.AddTorrent(torrentPath);
                        break;
                    case "manualupdate":
                        networkManager.SendTorrentListUpdate(torrentManager.newTorrentList.ToArray());
                        torrentManager.newTorrentList.Clear();
                        Logman.Log("Sent new torrent list.");
                        break;
                    case "debugconnect":
                        Logman.Log($"Trying to connect to {userInput[1]}:57300");
                        networkManager.Connect(userInput[1], 57300);
                        break;
                    case "list":
                    case "dir":
                        int torrentCount = torrentManager.torrentList.Count;
                        if (torrentCount == 0)
                        {
                            Logman.Log("No torrent found ! Try adding some or check your nodes.");
                            break;
                        }

                        int pageNumber = (userInput.Length > 1) ? int.Parse(userInput[1]) - 1 : 0;
                        Logman.Log($"Viewing page {pageNumber + 1} of {(torrentCount / 15) + 1}");

                        for (int i = 15 * pageNumber; i < 15 * (pageNumber + 1); i++)
                        {
                            if(i < torrentCount)
                                Logman.Log($"{i}: {torrentManager.torrentList[i].Name}");
                        }
                        break;
                    case "get":
                        int torrentID = int.Parse(userInput[1]);
                        if (torrentID > torrentManager.torrentList.Count)
                        {

                            Logman.Log("No corresponding torrent found", LOG_TYPE.WARNING);
                        }
                        else
                        {
                            Process.Start(torrentManager.torrentList[torrentID].GetMagnet());
                        }
                        break;
                    case "save":
                        Database.Save(torrentManager.torrentList.ToArray());
                        Logman.Log("Database successfully saved.");
                        break;
                }
                Thread.Sleep(1);
            }
            Logman.Log("Exited main loop. Press any key to exit.", LOG_TYPE.WARNING);
            Console.ReadLine();
        }

        private static void NetworkManager_NewClientConnected(Lidgren.Network.NetConnection con)
        {
            if (torrentManager.torrentList.Count == 0)
                return;
            Logman.Log("Sending torrent database to new client.");
            networkManager.SendTorrentListUpdate(torrentManager.torrentList.ToArray(), con);
        }

        private static void TorrentManager_TorrentUpdateThresholdReached(Torrent[] newTors)
        {
            Logman.Log("Hit automatic update threshold. Sending new torrent list.");
            networkManager.SendTorrentListUpdate(newTors);
        }

        private static void NetworkManager_TorrentListReceived(Torrent[] torrents)
        {
            torrentManager.UpdateTorrentList(torrents);
            Database.Save(torrentManager.torrentList.ToArray());
            Logman.Log("Received torrent list update.");
        }

        private static void BulkLoad()
        {
            foreach (string tF in Directory.EnumerateFiles("bulk"))
            {
                torrentManager.AddTorrent(tF);
                File.Delete(tF);
            }
            Directory.Delete("bulk");
        }
    }
}