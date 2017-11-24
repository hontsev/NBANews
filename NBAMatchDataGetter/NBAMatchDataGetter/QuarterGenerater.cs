using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBAMatchDataGetter
{
    public class EventInfo
    {
        public string description;
        public int hscore;
        public int gscore;
        public int period;
        public string personid;
        public string teamid;
        public string gameTime;
    }

    public class Slice
    {
        public int beginIndex;
        public int endIndex;
        public bool increase;
        public double energy;
        public Slice(int bindex, int eindex, bool inc, double engy)
        {
            beginIndex = bindex;
            endIndex = eindex;
            increase = inc;
            energy = engy;
        }
    }

    public class Performance:IComparable<Performance>
    {
       

        public string player;
        public string team;
        public int score1;
        public int score2;
        public int score3;
        /// <summary>
        /// 篮板球
        /// </summary>
        public int rebound;
        public int shoot2;
        public int shoot3;
        //public int gai;
        /// <summary>
        /// 失误
        /// </summary>
        public int foul;
        /// <summary>
        /// 犯规
        /// </summary>
        public int fault;

        public int CompareTo(Performance other)
        {
            return (score1 + score2 + score3).CompareTo(other.score1 + other.score2 + other.score3) * -1;
        }

        public int score()
        {
            return score1 + score2 + score3;
        }
    }

    public class QuarterGenerater
    {
        Dictionary<string, List<string>> templates;
        public List<EventInfo> data;
        public List<Slice> slices;
        public List<Slice> orislices;
        public int[] scorechange;
        public int quarter;
        //string template = "";
        MatchInfo info;
        Random ran = new Random();

        public QuarterGenerater(MatchInfo minfo,List<EventInfo> alldata, int quarter,Dictionary<string,List<string>> temp)
        {
            templates = temp;
            info = minfo;
            this.quarter = quarter;
            data = new List<EventInfo>();
            foreach(var d in alldata)
            {
                if (d.period == quarter)
                {
                    data.Add(d);
                }
            }
        }

        /// <summary>
        /// 随机选取一个模板
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public string getTemplate(string pattern)
        {
            if (templates.ContainsKey(pattern))
            {
                var temps = templates[pattern];
                if (temps.Count <= 0) return "";
                int index = ran.Next(0, temps.Count - 1);
                return temps[index];
            }
            else
            {
                return "";
            }
        }

        private int getScoreChangeValue(int index)
        {
            if (index < 0 || index >= data.Count) return 0;
            if (index == 0) return data[index].hscore - data[index].gscore;

            return (data[index].hscore - data[index].gscore) - (data[index - 1].hscore - data[index - 1].gscore);
        }

        private void getScoreChange()
        {
            List<int> value = new List<int>();

            int val = 0;
            for (int i = 0; i < data.Count; i++)
            {
                val +=getScoreChangeValue(i);
                value.Add(val);
            }

            this.scorechange = value.ToArray();
        }

        private void getSlice()
        {
            slices = new List<Slice>();
            orislices = new List<Slice>();

            bool lastIncrase=true;
            int lastIndex = 0;
            bool begin = true;
            double energy = 0;
            for (int i = 1; i < scorechange.Length; i++)
            {
                
                int thisChange = scorechange[i]-scorechange[i-1];
                energy += Math.Pow(thisChange,2);
                if (thisChange == 0) { continue; }
                else if (begin) { begin = false; lastIncrase = thisChange > 0; continue; }
                if (thisChange > 0)
                {
                    if (!lastIncrase)
                    {

                        slices.Add(new Slice(lastIndex, i - 1, lastIncrase,energy));
                        energy = 0;
                        lastIndex = i;
                        lastIncrase = !lastIncrase;
                    }
                }
                else if (thisChange < 0)
                {
                    if (lastIncrase)
                    {

                        slices.Add(new Slice(lastIndex, i - 1, lastIncrase,energy));
                        energy = 0;
                        lastIndex = i;

                        lastIncrase = !lastIncrase;
                    }
                }

            }
            // last part
            if (lastIndex < data.Count - 1)
                slices.Add(new Slice(lastIndex, data.Count - 1, lastIncrase,energy));

            foreach (var s in slices) orislices.Add(s);
        }

        private void resetSlice2()
        {
            List<Slice> newslices = new List<Slice>();
            List<double> ep = new List<double>();
            int beginindex = 1;
            int endindex = slices[0].endIndex;
            double energy = slices[0].energy;
            bool increase = slices[0].increase;

            for(int i = 1; i < slices.Count; i++)
            {
                Slice tslice = slices[i];
                TimeSpan ttime2 = timeSub(data[tslice.beginIndex - 1].gameTime, data[tslice.endIndex].gameTime);
                double energyp = (tslice.energy) / (ttime2.TotalMinutes);
                ep.Add(energyp);
            }

            for (int i = 1; i < slices.Count - 1; i++)
            {
                Slice tslice = slices[i];
                //已merge部分长度
                TimeSpan ttime1 = timeSub(data[beginindex-1].gameTime, data[tslice.endIndex].gameTime);
                //当前段长度
                TimeSpan ttime2 = timeSub(data[tslice.beginIndex-1].gameTime, data[tslice.endIndex].gameTime);
                bool merge = true;
                if (increase != tslice.increase)
                {
                    
                    double energyp = (tslice.energy) / (ttime2.TotalMinutes);
                    
                    //if (ttime2.TotalMinutes < 0.2 || energyp < 20)
                    if (ttime1.TotalMinutes < 0.3 || ttime2.TotalMinutes < 0.5 || tslice.energy < 10)
                    {
                        merge = true;
                    }
                    else
                    {
                        merge = false;
                    }
                }

                if (merge)
                {
                    endindex = tslice.endIndex;
                    energy += tslice.energy;
                    increase = (data[tslice.endIndex].hscore - data[tslice.endIndex].gscore) - (data[beginindex].hscore - data[beginindex].gscore) >= 0;
                }
                else
                {
                    newslices.Add(new Slice(beginindex, endindex, increase, energy));
                    beginindex = slices[i].beginIndex;
                    endindex = slices[i].endIndex;
                    energy = slices[i].energy;
                    increase = slices[i].increase;
                }


            }
            // add last part
            if (endindex < slices[slices.Count - 1].endIndex)
                newslices.Add(new Slice(
                    beginindex, data.Count - 1, 
                    (data[slices[slices.Count - 1].endIndex].hscore - data[slices[slices.Count - 1].endIndex].gscore) - (data[beginindex].hscore - data[beginindex].gscore) >= 0, 
                    energy));

            slices = newslices;
        }

        private void resetSlice()
        {
            List<Slice> newslices = new List<Slice>();

            int beginindex = slices[0].beginIndex;
            int endindex = slices[0].endIndex;
            bool findIncrease = slices[0].increase;
            double energy = slices[0].energy;
            for (int i = 1; i < slices.Count - 1; i++)
            {
                if (slices[i].increase == findIncrease)
                {
                    // try merge
                    if (
                        (findIncrease == true && scorechange[slices[i].endIndex] < scorechange[endindex])
                        ||
                        (findIncrease == false && scorechange[slices[i].endIndex] > scorechange[endindex])
                    )
                    {
                        // no merge
                        newslices.Add(new Slice(beginindex, endindex, findIncrease, energy));
                        beginindex = slices[i - 1].beginIndex;
                        endindex = slices[i - 1].endIndex;
                        energy = slices[i - 1].energy;
                        findIncrease = !findIncrease;
                    }
                    else
                    {
                        // merge
                        endindex = slices[i].endIndex;
                        energy += slices[i].energy;
                    }
                }
            }
            // add last part
            if (endindex < slices[slices.Count - 1].endIndex)
                newslices.Add(new Slice(beginindex, data.Count - 1, findIncrease, energy));

            slices = newslices;
        }

        public void prepare()
        {
            getScoreChange();
            getSlice();
            resetSlice2();
        }

        /// <summary>
        /// 生成本节之前的回顾文本
        /// </summary>
        private string gBefore()
        {
            string res = "";

            //template= template.Replace("{之前情况}", res);
            return res;
        }

        /// <summary>
        /// 生成节的称谓
        /// </summary>
        /// <returns></returns>
        private string gQuarter()
        {
            string res = "";
            if (quarter == 1) res = "首节";
            else if (quarter == 2) res = "第二节";
            else if (quarter == 3) res = "第三节";
            else if (quarter == 4) res = "第四节";

            //template=template.Replace("{节名}", res);
            return res;
        }

        /// <summary>
        /// 获取球员全名。用-连接姓和名
        /// </summary>
        /// <param name="einfo"></param>
        /// <returns></returns>
        private string getPlayer(EventInfo einfo)
        {
            string res ="";

            foreach(var player in info.players)
            {
                if (einfo.description.IndexOf(player) == 0)
                {
                    //begin with player name
                    return player.Replace(" ","-");
                }
            }

            return res;
        }

        /// <summary>
        /// 根据一条直播语句获取球员的表现数据
        /// </summary>
        /// <param name="einfo"></param>
        /// <param name="pf"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        private Performance getPerformance(EventInfo einfo, Performance pf = null, string team = "")
        {
            if (pf == null) pf = new Performance();
            pf.team = team;
            pf.player = getPlayer(einfo);
            string tmp = einfo.description.Substring(pf.player.Length).Trim();
            if (tmp.StartsWith("两分球进"))
            {
                pf.shoot2 += 1;
                pf.score2 += 2;
            }
            else if (tmp.StartsWith("三分球进"))
            {
                pf.shoot3 += 1;
                pf.score3 += 3;
            }
            else if (tmp.StartsWith("罚球命中"))
            {
                pf.score1 += 1;
            }
            else if (tmp.StartsWith("篮板"))
            {
                pf.rebound += 1;
            }
            else if (tmp.StartsWith("两分不中"))
            {
                pf.shoot2 += 1;
            }
            else if (tmp.StartsWith("三分不中"))
            {
                pf.shoot3 += 1;
            }
            else if (tmp.StartsWith("失误"))
            {
                pf.foul += 1;
            }
            else if (tmp.Contains("犯规"))
            {
                pf.fault += 1;
            }
            return pf;

        }
        
        /// <summary>
        /// 获取某一时段内的某队的全部球员表现数据
        /// 以时段内所得分总分倒序排列
        /// </summary>
        /// <param name="teamid"></param>
        /// <param name="team"></param>
        /// <param name="beginIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public List<Performance> getPerformance(string teamid,string team,int beginIndex,int endIndex)
        {
            Dictionary<string, Performance> p = new Dictionary<string, Performance>();

            for(int i = beginIndex; i <= endIndex; i++)
            {
                if (data[i].teamid == teamid)
                {
                    string player= getPlayer(data[i]);
                    if (string.IsNullOrWhiteSpace(player)) continue;
                    Performance pf;
                    if (p.ContainsKey(player))
                    {
                        pf = p[player];
                    }
                    else
                    {
                        pf = new Performance();
                        p[player] = pf;
                    }
                    getPerformance(data[i], pf, team);
                    
                }
            }
            var list =p.Values.ToList();
            list.Sort();
            return list;
        }

        /// <summary>
        /// 生成时刻描述
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private string gTimeSpan(string time)
        {
            string res = "";

            DateTime dt1 = DateTime.Parse("1:" + time.Trim());
            DateTime dt2 = DateTime.Parse("1:12:00");

            DateTime dt = dt1;
            TimeSpan dtstart = dt2 - dt1;
            if (dt.Minute < dtstart.Minutes)
            {
                if (dt.Minute > 0)
                    res = dt.ToString("本节还有m分s秒时，");
                else
                    res = dt.ToString("本节还有s秒时，");
            }
            else
            {
                if (dtstart.Minutes > 0)
                    res = string.Format("本节开始第{0}分钟，", dtstart.Minutes);
                else
                    res = string.Format("本节开始刚刚{0}秒，", dtstart.Seconds); 
            }


            return res;
        }

        public static string gTimeSub(string time1,string time2)
        {
            TimeSpan ts = timeSub(time1, time2);
            if (ts.Minutes >= 1)
            {
                if(ts.Seconds>20) return string.Format("{0}分{1}秒", ts.Minutes, ts.Seconds);
                else return string.Format("{0}分钟", ts.Minutes);
            }
                
            else return string.Format("{0}秒", ts.Seconds);
        }

        /// <summary>
        /// 计算时间差
        /// </summary>
        /// <param name="time1"></param>
        /// <param name="time2"></param>
        /// <returns></returns>
        public static TimeSpan timeSub(string time1, string time2)
        {
            DateTime dt1 = DateTime.Parse("1:" + time1.Trim());
            DateTime dt2 = DateTime.Parse("1:" + time2.Trim());

            TimeSpan dt = dt1 - dt2;
            return dt;
        }

        /// <summary>
        /// 生成球员称呼，避免使用全名
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private string gNickName(string player)
        {
            var namepart = player.Trim().Replace(' ', '-').Split('-');
            if (namepart.Length <= 0) return player;
            return namepart[namepart.Length - 1];
        }

        /// <summary>
        /// 生成关键反超语
        /// </summary>
        /// <param name="teamid"></param>
        /// <param name="team"></param>
        /// <param name="sindex"></param>
        /// <returns></returns>
        private string gFocus(string teamid,string team,int sindex)
        {
            string res = "";

            for(int i = slices[sindex].endIndex; i >= slices[sindex].beginIndex; i--)
            {
                var p = getPerformance(data[i]);
                if (p.score3 + p.score2 > 0)
                {
                    if (p.score3 > 0)
                    {
                        //三分球
                        res = getTemplate("{focus-score3}");
                        //res = "{时刻}{球员}一记三分让{球队}重新领先。";

                    }
                    else if (p.score2 > 0)
                    {
                        res = getTemplate("{focus-score2}");
                        //res = "随着{时刻}{球员}的进球，{球队}扳回劣势。";
                    }

                    res = res.Replace("{时刻}", gTimeSpan(data[i].gameTime));
                    res = res.Replace("{球员}", gNickName(p.player));
                    res = res.Replace("{球队}", team);
                }
            }

            return res;
        }

        /// <summary>
        /// 生成本节总结语
        /// </summary>
        private string gConclusion()
        {
            string res = "";

            int hscore1 = data[slices.First().beginIndex].hscore;
            int gscore1 = data[slices.First().beginIndex].gscore;
            int hscore2 = data[slices.Last().endIndex].hscore;
            int gscore2 = data[slices.Last().endIndex].gscore;

            string ateam, dteam;
            int ascore1, ascore2, dscore1, dscore2;


            if (hscore1 - gscore1 >= 0)
            {
                //主队领先
                ateam = info.hname;
                dteam = info.gname;
                ascore1 = hscore1;
                ascore2 = hscore2;
                dscore1 = gscore1;
                dscore2 = gscore2;
            }
            else
            {
                ateam = info.gname;
                dteam = info.hname;
                ascore1 = gscore1;
                ascore2 = gscore2;
                dscore1 = hscore1;
                dscore2 = hscore2;
            }

            if (quarter == 1)
            {
                //第一节
                if (ascore2 - dscore2 > 0)
                {
                    //领先
                    res = getTemplate("{conclusion-quarter1}");
                    //res = "{节名}过后，{领先队}砍下{领先队本节得分},以{领先比分}暂时领先。";
                }
                else if (ascore2 - dscore2 < 0)
                {
                    //另一队领先。因为是首节，所以优劣转换而模板不变
                    var tmp = ascore1;
                    ascore1 = dscore1;
                    dscore1 = tmp;
                    tmp = ascore2;
                    ascore2 = dscore2;
                    dscore2 = tmp;
                    var tmpteam = ateam;
                    ateam = dteam;
                    dteam = tmpteam;
                    res = getTemplate("{conclusion-quarter1}");
                    //res = "{节名}过后，{领先队}砍下{领先队本节得分},以{领先比分}暂时领先。";
                }
                else
                {
                    //被追平
                    res = getTemplate("{conclusion-quarter1-draw}");
                    //res = "{节名}战况激烈，双方战至{领先比分}平。";
                }
            }
            else if (quarter == 4)
            {
                //最后一节
                if (ascore2 - dscore2 > 0)
                {
                    //依然领先
                    res = getTemplate("{conclusion-quarter4}");
                    //res = "随着{节名}{领先队}砍下{领先队本节得分},断了{落后队}的希望。";
                    if (ascore2 - dscore2 > 20)
                    {
                        res += getTemplate("{conclusion-quarter4-great}");
                        //res += "双方差距达到20分以上，胜负失去悬念。";
                    }
                }
                else if (ascore2 - dscore2 < 0)
                {
                    //被反超
                    res = getTemplate("{conclusion-quarter4-reverse}");
                    //res = "{节名}紧要关头，{落后队}以{落后比分}反超，惊险拿下比赛。";
                }
            }
            else
            {
                if (ascore2 - dscore2 > 0)
                {
                    //依然领先
                    res = getTemplate("{conclusion-quarter-draw}");
                    //res = "{节名}过后，{领先队}砍下{领先队本节得分},依旧以{领先比分}领先。";
    
                }
                else if (ascore2 - dscore2 < 0)
                {
                    //被反超
                    res = getTemplate("{conclusion-quarter-reverse}");
                    //res = "到{节名}结束，{落后队}以{落后比分}反超。";
                }
                else
                {
                    //被追平
                    res = getTemplate("{conclusion-quarter-draw}");
                    //res = "到{节名}结束时，{落后队}以{落后比分}追平{领先队}。";
                }
            }
            
           res = res.Replace("{节名}", gQuarter());
            res = res.Replace("{领先队}", ateam);
            res = res.Replace("{落后队}", dteam);
            res = res.Replace("{领先比分}", string.Format("{0}-{1}",ascore2,dscore2));
            res = res.Replace("{落后比分}", string.Format("{0}-{1}", dscore2, ascore2));
            res = res.Replace("{领先队本节得分}", string.Format("{0}分", ascore2 - ascore1));
            res = res.Replace("{落后队本节得分}", string.Format("{0}分", dscore2 - dscore1));
            //template = template.Replace("{本节总结}", res);
            return res;
        }

        private string getNumberHan(int num)
        {
            string[] hans = new string[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            if (num >= 0 && num <= 10) return hans[num];
            else return "几";
        }

        /// <summary>
        /// 生成球员的表现叙述
        /// </summary>
        /// <param name="pers"></param>
        /// <param name="maxnum"></param>
        /// <returns></returns>
        private string gPerformance(List<Performance> pers,int maxnum=1)
        {
            string res = "";
            int score3 = 0;
            foreach (var p in pers) score3 += p.score3;
            if (score3 > 2)
            {
                // 两个以上三分
                int score3num = 0;
                List<string> player = new List<string>();
                foreach(var p in pers)
                {
                    if (p.score3 >= 1)
                    {
                        score3num = p.score3;
                        player.Add(gNickName(p.player));
                    }
                }
                if (player.Count == 1)
                {
                    //一个人投中多个三分
                    res += string.Format("{0}接连投中{1}个三分", player.First(),getNumberHan(score3num));
                }
                else
                {
                    //多个人投中三分
                    res += string.Format("{0}和{1}相继三分命中", player[0], player[1]);
                }


            }
            else
            {
                int num = Math.Min(maxnum, pers.Count);

                if (num >= 1)
                {
                    res = string.Format("{1}的{0}远投命中", gNickName(pers[0].player), pers[0].team);
                }
                if (num >= 2)
                {
                    res += string.Format("，{0}也为{1}赢得了得分", gNickName(pers[1].player), pers[1].team);
                }
            }
            if (!string.IsNullOrWhiteSpace(res)) res += "。";
            return res;
        }

        private string gScore(int num)
        {
            if (num == 0) return "一分未得";
            else if (num < 5) return string.Format("仅得{0}分", num);
            else if (num > 20) return string.Format("狂砍{0}分", num);
            else return string.Format("连得{0}分", num);
        }

        /// <summary>
        /// 生成某一节的节内情况描述
        /// </summary>
        private string gInfo()
        {
            string res = "";
            for(int i = 0; i < slices.Count; i++)
            {
                int ascore1, ascore2, dscore1, dscore2;
                List<Performance> aper;
                List<Performance> dper;
                string ateam;
                string dteam;
                string aid;
                string did;
                string begintime= data[slices[i].beginIndex].gameTime;
                string endtime= data[slices[i].endIndex].gameTime;
                if (slices[i].increase)
                {
                    //报导主队
                    ateam = info.hname;
                    dteam = info.gname;
                    aid = info.hid;
                    did = info.gid;
                    ascore1 = data[slices[i].beginIndex].hscore;
                    ascore2 = data[slices[i].endIndex].hscore;
                    dscore1 = data[slices[i].beginIndex].gscore;
                    dscore2 = data[slices[i].endIndex].gscore;
                    
                }
                else
                {
                    //报导客队
                    ateam = info.gname;
                    dteam = info.hname;
                    aid = info.gid;
                    did = info.hid;
                    dscore1 = data[slices[i].beginIndex].hscore;
                    dscore2 = data[slices[i].endIndex].hscore;
                    ascore1 = data[slices[i].beginIndex].gscore;
                    ascore2 = data[slices[i].endIndex].gscore;
                }

                aper = getPerformance(aid, ateam, slices[i].beginIndex, slices[i].endIndex);
                dper = getPerformance(did, dteam, slices[i].beginIndex, slices[i].endIndex);

                string tmp = "";

               if (aper.Count < 1 || string.IsNullOrWhiteSpace(aper[0].player))
                {
                    // 球员表现情况数据获取有误，跳过该句描述。
                    continue;
                }

                int scoreDif2 = ascore2 - dscore2;
                int scoreDif1 = ascore1 - dscore1;
                int dif = scoreDif2 - scoreDif1;
                if (scoreDif1 > 0 && dif>5)
                {
                    //领先，并拉大比分差
                    tmp = getTemplate("{info-adv}");
                    //tmp = "{领先队}在{领先球员1}的带领下，打出一波{得分比}的攻势，取得了{领先分数}的领先优势。{领先表现}";
                }
                else if (ascore2 - dscore2 < -5)
                {
                    //落后，反追
                    tmp = getTemplate("{info-dis}");
                    //tmp = "{领先队}奋起直追，依靠{领先球员1}的一记进球，将比分拉回至{分数比}。";
                    //tmp = tmp.Replace("{领先分数}", (ascore2 - dscore2).ToString() + "分");
                }else if (ascore2 - dscore2 > 0)
                {
                    // 反超
                    tmp = getTemplate("{info-reverse}");
                    //tmp = "{领先队}紧追不舍。{反超情形}{落后表现}";
                }
                else
                {
                    //得分差距不大
                    tmp = getTemplate("{info-draw}");
                    //tmp = "双方打得相当胶着, ";
                }
                tmp = tmp.Replace("{时间}", gTimeSub(begintime, endtime));
                tmp = tmp.Replace("{开始时刻}", gTimeSpan(begintime));
                tmp = tmp.Replace("{结束时刻}", gTimeSpan( endtime));
                tmp = tmp.Replace("{节名}", gQuarter());
                tmp = tmp.Replace("{领先队}", ateam);
                tmp = tmp.Replace("{落后队}", dteam);
                if (aper.Count >= 1) tmp = tmp.Replace("{领先球员1}", gNickName(aper[0].player));
                if (aper.Count >= 2) tmp = tmp.Replace("{领先球员2}", gNickName(aper[1].player));
                if (aper.Count >= 3) tmp = tmp.Replace("{领先球员3}", gNickName(aper[2].player));
                if (aper.Count >= 4) tmp = tmp.Replace("{领先球员4}", gNickName(aper[3].player));
                if (aper.Count >= 5) tmp = tmp.Replace("{领先球员5}", gNickName(aper[4].player));
                if (dper.Count >= 1) tmp = tmp.Replace("{落后球员1}", gNickName(dper[0].player));
                if (dper.Count >= 2) tmp = tmp.Replace("{落后球员2}", gNickName(dper[1].player));
                if (dper.Count >= 3) tmp = tmp.Replace("{落后球员3}", gNickName(dper[2].player));
                if (dper.Count >= 4) tmp = tmp.Replace("{落后球员4}", gNickName(dper[3].player));
                if (dper.Count >= 5) tmp = tmp.Replace("{落后球员5}", gNickName(dper[4].player));
                tmp = tmp.Replace("{分数比}", string.Format("{0}-{1}", dscore2, ascore2));
                tmp = tmp.Replace("{得分比}", string.Format("{0}-{1}", ascore2-ascore1, dscore2-dscore1));
                tmp = tmp.Replace("{落后得分比}", string.Format("{0}-{1}", dscore2 - dscore1,ascore2 - ascore1 ));
                tmp = tmp.Replace("{领先分数}", string.Format("{0}分",ascore2 - dscore2));
                tmp = tmp.Replace("{落后分数}", string.Format("{0}分", dscore2 - ascore2));
                tmp = tmp.Replace("{领先得分}", gScore(ascore2 - ascore1));
                tmp = tmp.Replace("{落后得分}", gScore(dscore2 - dscore1));
                tmp = tmp.Replace("{反超情形}", gFocus(aid, ateam, i));
                tmp = tmp.Replace("{领先表现}", gPerformance(aper, 2));
                tmp = tmp.Replace("{落后表现}", gPerformance(dper, 2));
                res += tmp;
            }
            return res;
            //template = template.Replace("{各片情况}", res);
        }


        public string generate()
        {
           // StringBuilder output = new StringBuilder();

            string template = "{之前情况}{节名}，{各片情况}{本节总结}";
            template = template.Replace("{之前情况}", gBefore());
            template = template.Replace("{节名}", gQuarter());
            template = template.Replace("{各片情况}", gInfo());
            template = template.Replace("{本节总结}", gConclusion());
            //template = "";
            //foreach (var v in slices)
            //{
            //    template += v.endIndex + " " + v.increase + "\r\n";
            //}

            //template = "";
            //for (int i = 0; i < slices.Count; i++)
            //{
            //    for (int j = slices[i].beginIndex; j < slices[i].endIndex; j++) template += j + "\t" + scorechange[j] + "\t" + -30 + "\r\n";
            //    template += slices[i].endIndex  + "\t" + scorechange[slices[i].endIndex] + "\t" + 11 * (slices[i].increase ? 1 : -1) + "\r\n";
            //}
            //foreach(var v in scorechange)
            //{
            //    template += v + "\r\n";
            //}

            return template;
        }

    }
}
