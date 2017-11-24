using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NBAMatchDataGetter
{

    public class MatchInfo
    {
        public DateTime time;
        public string gname;
        public string gid;
        public string hname;
        public string hid;
        public List<string> hplayer;
        public List<string> gplayer;
        public List<string> players;
        public int hscore;
        public int gscore;
    }



    public enum ItemType
    {
        score2,score3,
    }

    //public class GenerateItem
    //{
    //    public string sentence = "";
    //    public int hscore;
    //    public int gscore;
    //    public 
    //}

    public class Generater
    {
        Dictionary<string, List<string>> templates;
        List<EventInfo> data;
        public List<QuarterGenerater> quarters;
        public MatchInfo info;


        
        public int[] getData()
        {
            List<int> res = new List<int>();

            foreach(var q in quarters)
            {
                foreach(var s in q.scorechange)
                {
                    res.Add(s);
                }
            }

            return res.ToArray();
        }

        public int[] getCutQuarter()
        {
            List<int> res = new List<int>();

            int begin = 0;
            foreach (var q in quarters)
            {
                begin += q.scorechange.Length;
                res.Add(begin);
            }

            return res.ToArray();
        }

        public int[] getCutSlice1()
        {
            List<int> res = new List<int>();

            int begin = 0;
            foreach (var q in quarters)
            {
                foreach (var s in q.orislices)
                {
                    res.Add(s.endIndex + begin);
                }
                begin += q.scorechange.Length;
            }

            return res.ToArray();
        }

        public int[] getCutSlice2()
        {
            List<int> res = new List<int>();

            int begin = 0;
            foreach (var q in quarters)
            {
                foreach(var s in q.slices)
                {
                    res.Add(s.endIndex + begin);
                }
                begin += q.scorechange.Length;
            }

            return res.ToArray();
        }


        public int[] getDataByTime()
        {
            int[] res = new int[12 * 60 * 4+1];

            int begin = 0;
            int lasttime = 0;
            foreach (var q in quarters)
            {
                for(int i = 0; i < q.scorechange.Length; i++)
                {
                    int timeindex=(int)(QuarterGenerater.timeSub("12:00", q.data[i].gameTime).TotalSeconds)+begin;
                    for(int j= lasttime+1; j <= timeindex; j++)
                    {
                        res[j] = q.scorechange[i];
                    }
                    lasttime = timeindex;
                }
                begin += 12 * 60;
            }

            return res.ToArray();
        }


        public int[] getCutQuarterByTime()
        {
            return new int[] { 12 * 60, 12 * 60 * 2, 12 * 60 * 3 };
        }

        public int[] getCutSlice1ByTime()
        {
            List<int> res = new List<int>();

            foreach (var q in quarters)
            {
                foreach (var s in q.orislices)
                {
                    res.Add((int)QuarterGenerater.timeSub("12:00", q.data[s.endIndex].gameTime).TotalSeconds + (q.quarter - 1) * 12 * 60);
                }
            }

            return res.ToArray();
        }

        public int[] getCutSlice2ByTime()
        {
            List<int> res = new List<int>();

            foreach (var q in quarters)
            {
                foreach (var s in q.slices)
                {
                    res.Add((int)QuarterGenerater.timeSub("12:00", q.data[s.endIndex].gameTime).TotalSeconds + (q.quarter - 1) * 12 * 60);
                   // res.Add(s.endIndex + begin);
                }
            }

            return res.ToArray();
        }

        public int[] getEnergyByTime()
        {
            int[] res = new int[12 * 60 * 4 + 1];

            int begin = 0;
            int lasttime = 0;
            foreach (var q in quarters)
            {
                for (int i = 0; i < q.slices.Count; i++)
                {
                    int timeindex = (int)(QuarterGenerater.timeSub("12:00", q.data[i].gameTime).TotalSeconds) + begin;
                    for (int j = lasttime + 1; j <= timeindex; j++)
                    {
                        res[j] = q.scorechange[i];
                    }
                    lasttime = timeindex;
                }
                begin += 12 * 60;
            }
            return res.ToArray();
        }


        public Generater()
        {
            
        }

        public Generater(string id)
        {
            init(id);
        }

        public void init(string id)
        {
            templates = new Dictionary<string, List<string>>();
            quarters = new List<QuarterGenerater>();
            info = new MatchInfo();
            data = new List<EventInfo>();

            // match data
            var res = MySqlHelper.GetData(string.Format("SELECT begin_time,host_team,guest_team FROM nbamatch.match WHERE id='{0}'", id));
            DataRow row = res[0];
            info.time = DateTime.Parse(row["begin_time"].ToString());
            info.hid = row["host_team"].ToString();
            info.gid = row["guest_team"].ToString();

            res = MySqlHelper.GetData(string.Format("SELECT name FROM nbamatch.team WHERE id='{0}'", info.hid));
            row = res[0];
            info.hname = row["name"].ToString();

            res = MySqlHelper.GetData(string.Format("SELECT name FROM nbamatch.team WHERE id='{0}'", info.gid));
            row = res[0];
            info.gname = row["name"].ToString();

            // players data
            info.players = new List<string>();
            res = MySqlHelper.GetData("SELECT * FROM nbamatch.person");
            foreach (DataRow r in res)
            {
                info.players.Add(r["name"].ToString());
            }

            // event data
            res = MySqlHelper.GetData(string.Format("SELECT description,period,game_time,team_id,person_id,gscore,hscore FROM nbamatch.event WHERE match_id='{0}' ORDER BY event_num", id));
            foreach(DataRow r in res)
            {
                EventInfo e = new EventInfo();
                e.description = r["description"].ToString();
                e.hscore = int.Parse(r["hscore"].ToString());
                e.gscore = int.Parse(r["gscore"].ToString());
                e.period = int.Parse(r["period"].ToString());
                e.teamid = r["team_id"].ToString();
                e.personid =r["person_id"].ToString();
                e.gameTime = r["game_time"].ToString();
                data.Add(e);
            }

            //templates
            res = MySqlHelper.GetData(string.Format("SELECT pattern,sentence FROM nbamatch.template", id));
            foreach (DataRow r in res)
            {

                string pattern = r["pattern"].ToString();
                string sentence = r["sentence"].ToString();
                if (!templates.ContainsKey(pattern))
                {
                    templates[pattern] = new List<string>();
                }
                templates[pattern].Add(sentence);
            }

        }
        

        public string gTime()
        {
            string res = info.time.ToString("北京时间yyyy年M月d日，");

            //string tmp1 = "";
            //if (info.time.Hour < 5) tmp1 = "凌晨";
            //else if (info.time.Hour < 12) tmp1 = "上午";
            //else if (info.time.Hour < 18) tmp1 = "下午";
            //else tmp1 = "傍晚";

            //res = res + tmp1;

            //res = res + info.time.ToString("H时");
            //if (info.time.Minute!=0) res = res + info.time.ToString("m分");

            return res;
        }

        public string gMatchEx()
        {
            string tmp;
            tmp = "{}";

            tmp = tmp.Replace("{}", "");

            return tmp;
        }

        public string gMatchResult()
        {
            string tmp;
            int scoreDif = info.hscore - info.gscore;
            if (scoreDif > 0)
            {
                //主队赢了
                if (scoreDif <= 5)
                {
                    //险胜
                    tmp = "{主队}主场以{主队在前比分}击败{客队}，惊险过关。";
                }
                else if (scoreDif > 10)
                {
                    //大比分胜出
                    tmp = "{主队}主场以{主队在前比分}力克{客队}。";

                }
                else
                {
                    //普通
                    tmp = "{主队}主场{主队在前比分}击退{客队}。";
                }

            }
            else
            {
                //客队赢了
                if (scoreDif > -5)
                {
                    //惜败
                    tmp = "{主队}主场{主队在前比分}不敌{客队}。";
                }
                else if (scoreDif <= -10)
                {
                    //大比分失败
                    tmp = "{客队}客场以{客队在前比分}击败{主队}。";

                }
                else
                {
                    //普通
                    tmp = "{主队}主场{主队在前比分}不敌{客队}。";
                }
            }


            tmp = tmp.Replace("{主队}", info.hname);
            tmp = tmp.Replace("{客队}", info.gname);
            tmp = tmp.Replace("{主队在前比分}", string.Format("{0}-{1}", info.hscore, info.gscore));
            tmp = tmp.Replace("{客队在前比分}", string.Format("{0}-{1}", info.gscore, info.hscore));

            return tmp;
        }

        private string gPerformance(string teamid,string team)
        {
            Dictionary<string,Performance> pers=new Dictionary<string, Performance>();
            foreach(var q in quarters)
            {
                var tmp=q.getPerformance(teamid, team, 0, q.data.Count - 1);
                foreach(var p in tmp)
                {
                    if (!pers.ContainsKey(p.player)) pers[p.player] = p;
                    else
                    {
                        pers[p.player].fault += p.fault;
                        pers[p.player].foul += p.foul;
                        pers[p.player].rebound += p.rebound;
                        pers[p.player].score1 += p.score1;
                        pers[p.player].score2 += p.score2;
                        pers[p.player].score3 += p.score3;
                        pers[p.player].shoot2 += p.shoot2;
                        pers[p.player].shoot3 += p.shoot3;
                    }
                }
            }

            List<Performance> perres = pers.Values.ToList();
            perres.Sort();

            string res = string.Format("{0}：",team);

            for(int i = 0; i < 5; i++)
            {
                var player = perres[i];
                if (player.score() < 5) continue;
                res += player.player;
                res += string.Format("{0}分", player.score());
                if (player.rebound > 3) res += string.Format("{0}个篮板，", player.rebound);
                else res += "，";
            }
            res = res.Substring(0, res.Length - 1) + "。";

            
            return res;
        }

        public string generateHead()
        {
            string template = "{时间}{场次情况}{比分情况}\r\n{胜队表现}\r\n{败队表现}";
            string ateamid = (info.hscore > info.gscore ? info.hid : info.gid);
            string dteamid= (info.hscore > info.gscore ? info.gid : info.hid);
            string ateam= (info.hscore > info.gscore ? info.hname : info.gname);
            string dteam = (info.hscore > info.gscore ? info.gname : info.hname);
            template = template.Replace("{胜队表现}", gPerformance(ateamid,ateam));
            template = template.Replace("{败队表现}", gPerformance(dteamid, dteam));
            template = template.Replace("{时间}", gTime());
            template = template.Replace("{场次情况}", gMatchEx());
            template = template.Replace("{比分情况}", gMatchResult());

            return template;
        }

        private void prepare()
        {
            for(int i = 1; i <= 4; i++)
            {
                QuarterGenerater q = new QuarterGenerater(info, data, i,templates);
                q.prepare();
                this.quarters.Add(q);
            }

            // now score
            info.hscore = data.Last().hscore;
            info.gscore = data.Last().gscore;
        }

        public string getNews(int quarter = 0)
        {
            string output = "";

            try
            {
                prepare();
            }
            catch
            {
                output = "(数据分析出错)";
                return output;
            }

            
            //try
            //{
                
                if (quarter < 1 || quarter > 4)
                {
                    // all

                    output += generateHead() + "\r\n";
                    output += quarters[0].generate() + "\r\n";
                    output += quarters[1].generate() + "\r\n";
                    output += quarters[2].generate() + "\r\n";
                    output += quarters[3].generate() + "\r\n";
                }
                else
                {
                // one quarter
                output += quarters[quarter].generate()+ "\r\n";
                }
                
            //}
            //catch
            //{
            //    output = "(生成出错)";
            //    return output;
            //}
            
            //output.Clear();
            //for (int i=0;i<data.Count;i++)
            //{
            //    output.Append(string.Format("{0}\r\n", scorechange[i]));
            //}

            //output.Clear();
            //for (int i = 0; i < slices.Count; i++)
            //{
            //    output.Append(string.Format("{0}\t{1}\r\n", slices[i].endIndex,slices[i].increase));
            //}

            return output;
        }
    }
}
