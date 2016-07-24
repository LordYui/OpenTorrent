using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BencodeNET;
using System.IO;
using BencodeNET.Objects;

namespace OpenTorrent
{
    class TorrentManager
    {
        public delegate void TorrentUpdateThresholdReachedHandler(Torrent[] newTors);
        public event TorrentUpdateThresholdReachedHandler TorrentUpdateThresholdReached;

        public List<Torrent> torrentList;
        public List<Torrent> newTorrentList;
        public TorrentManager()
        {
            torrentList = new List<Torrent>();
            newTorrentList = new List<Torrent>();
        }

        public void UpdateTorrentList(Torrent[] torrents)
        {
            string[] listHashes = torrentList.Select(t => t.Infohash).ToArray();
            foreach (Torrent t in torrents)
            {
                if (!listHashes.Contains(t.Infohash))
                    torrentList.Add(t);
            }
        }

        public void AddTorrent(string path)
        {
            string ext = Path.GetExtension(path);
            if (!File.Exists(path) || Path.GetExtension(path) != ".torrent")
            {
                Logman.Log("Incorrect torrent file specified. Check the path and extension of the file.", LOG_TYPE.WARNING);
                return;
            }
            TorrentFile torrent = Bencode.DecodeTorrentFile(path);
            Torrent newTorrent = new Torrent(torrent.CalculateInfoHash(), Path.GetFileNameWithoutExtension(path), torrent.Announce);

            torrentList.Add(newTorrent);
            newTorrentList.Add(newTorrent);

            Logman.Log("Torrent added to database.");
            Database.Save(torrentList.ToArray());
            if (TorrentUpdateThresholdReached != null)
                TorrentUpdateThresholdReached.Invoke(newTorrentList.ToArray());
        }
    }

    class Torrent
    {
        public string Infohash;
        public string Name;
        public string[] Trackers;
        public Torrent(string infohash, string name, string[] trackers)
        {
            Infohash = infohash;
            Name = name;
            Trackers = trackers;
        }

        public Torrent(string infohash, string name, string trackers)
        {
            Infohash = infohash;
            Name = name;
            Trackers = new string[] { trackers };
        }

        public string GetMagnet()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("magnet:?xt=urn:btih:");
            sb.Append(Infohash);
            sb.Append("&dn=" + Name);
            foreach(string track in Trackers)
            {
                sb.Append("&tr=" + track);
            }

            return sb.ToString();
        }
    }
}