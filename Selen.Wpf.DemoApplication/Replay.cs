using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airmech.Replays.ARM
{
    class Replay
    {
        public String version;
        public DateTime localtime;
        public String map;
        public String gameMode;
        public String filePath;
        public String fileName;
        public int maxPlayers;
        public int netPlayers;

        public Dictionary<String, String> p1Data = new Dictionary<string, string>();
        public Dictionary<String, String> p2Data = new Dictionary<string, string>();
        public Dictionary<String, String> p3Data = new Dictionary<string, string>();
        public Dictionary<String, String> p4Data = new Dictionary<string, string>();
        public int parseReplayInfo(String filename)
        {
            filePath = filename;
            fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
            fileName = fileName.Substring(0, fileName.LastIndexOf('.'));

            string[] lines = System.IO.File.ReadAllLines(filename);

            bool p1 = false, p2 = false, p3 = false, p4 = false;

            foreach (string line in lines)
            {

                if (line.StartsWith("player 5 {"))
                {
                    break;
                }


                if (line.StartsWith("version"))
                {
                    version = line.Split(' ')[1];
                }else if (line.StartsWith("maxPlayers"))
                {
                    maxPlayers = int.Parse(line.Split(' ')[1])/2;
                }
                else if (line.StartsWith("netPlayers"))
                {
                    netPlayers = int.Parse(line.Split(' ')[1])/ 2;
                }
                else if (line.StartsWith("localtime"))
                {
                    String lineDate = line.Replace("localtime ", "");
                    String[] dateData = lineDate.Replace("  ", " ").Split(' ');
                    localtime = DateTime.Parse(dateData[2] + " " + dateData[1] + " " + dateData[4] + " " + dateData[3]);
                    //DateTime localtime = DateTime.Parse(lineDate);
                }
                else if ((line.StartsWith("map")) && (!(line.StartsWith("mapCfg"))))
                {
                    String[] mapData = line.Split(' ');
                    map = mapData[1];
                }
                else if (line.StartsWith("gameMode"))
                {
                    gameMode = line.Split(' ')[1];
                }

                else if (((((p1 || p2)) || (p3 || p4)) && (!line.EndsWith("{"))) && (line.Length > 2))
                {
                    String[] lineData = line.Split(' ');
                    if (p1)
                    {
                        p1Data.Add(lineData[2], lineData[3]);
                    }
                    else if (p2)
                    {
                        p2Data.Add(lineData[2], lineData[3]);
                    }
                    else if (p3)
                    {
                        p3Data.Add(lineData[2], lineData[3]);
                    }
                    else if (p4)
                    {
                        p4Data.Add(lineData[2], lineData[3]);
                    }
                }
                else if (line.StartsWith("player 1"))
                {
                    p1 = true;
                    p2 = false;
                    p3 = false;
                    p4 = false;
                }
                else if (line.StartsWith("player 2"))
                {
                    p1 = false;
                    p2 = true;
                    p3 = false;
                    p4 = false;
                }
                else if (line.StartsWith("player 3"))
                {
                    p1 = false;
                    p2 = false;
                    p3 = true;
                    p4 = false;
                }
                else if (line.StartsWith("player 4"))
                {
                    p1 = false;
                    p2 = false;
                    p3 = false;
                    p4 = true;
                }


            }
            return 0;
        }
    }

    public class ReplayComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            Replay replayX = x as Replay;
            Replay replayY = y as Replay;

            return replayX.localtime.CompareTo(replayY.localtime);
        }
    }
}
