using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OpenTorrent
{
    static class Database
    {
        public static void Save(Torrent[] tList)
        {
            if (!File.Exists("torrents.dat"))
                File.Create("torrents.dat").Close();

            using (MemoryStream mS = new MemoryStream())
            {
                using (BinaryWriter bW = new BinaryWriter(mS))
                {
                    bW.Write(tList.Length);
                    foreach (Torrent t in tList)
                    {
                        bW.Write(t.Infohash);
                        bW.Write(t.Name);
                        bW.Write(t.Trackers.Length);
                        foreach (string track in t.Trackers)
                        {
                            
                            bW.Write(track);
                        }
                    }
                    File.WriteAllBytes("torrents.dat", mS.ToArray());
                }
            }
        }

        public static Torrent[] Load()
        {
            if (!File.Exists("torrents.dat"))
                return null;
            List<Torrent> retArr;
            using (MemoryStream mS = new MemoryStream(File.ReadAllBytes("torrents.dat")))
            {
                using (BinaryReader bR = new BinaryReader(mS))
                {
                    int tCount = bR.ReadInt32();
                    retArr = new List<Torrent>(tCount);
                    for (int i = 0; i < tCount; i++)
                    {
                        string hash = bR.ReadString();
                        string name = bR.ReadString();
                        int trackLen = bR.ReadInt32();
                        string[] trackers = new string[trackLen];
                        for (int j = 0; j < trackLen; j++)
                        {
                            trackers[j] = bR.ReadString();
                        }
                        Torrent retTor = new Torrent(hash, name, trackers);
                        retArr.Add(retTor);
                    }
                    
                }
            }
            Logman.Log($"Loaded local database containing {retArr.Count} torrents.");
            return retArr.ToArray();
        }
    }
}