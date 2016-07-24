using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OpenTorrent
{
    static class TorrentSerializer
    {
        public static byte[] Serialize(Torrent[] tList)
        {
            using (MemoryStream mS = new MemoryStream())
            {
                using (BinaryWriter bW = new BinaryWriter(mS))
                {
                    foreach(Torrent t in tList)
                    {
                        bW.Write(t.Infohash);
                        bW.Write(t.Name);
                        bW.Write(t.Trackers.Length);
                        foreach(string track in t.Trackers)
                        {
                            bW.Write(track);
                        }
                    }
                }
                return mS.ToArray();
            }
        }

        public static Torrent[] Deserialize(byte[] data)
        {
            List<Torrent> retArr = new List<OpenTorrent.Torrent>();
            data = data.Skip(1).ToArray();
            using(MemoryStream mS = new MemoryStream(data))
            {
                using (BinaryReader bR = new BinaryReader(mS))
                {
                    long torrentCount = 0;
                    while(torrentCount < data.Length)
                    {
                        string infohash = bR.ReadString();
                        string name = bR.ReadString();
                        int trackCount = bR.ReadInt32();
                        string[] trackers = new string[trackCount];
                        for (int i = 0; i < trackCount; i++)
                        {
                            trackers[i] = bR.ReadString();
                        }
                        retArr.Add(new Torrent(infohash, name, trackers));
                        torrentCount = bR.BaseStream.Position;
                    }
                }
            }
            return retArr.ToArray();
        }
    }
}
