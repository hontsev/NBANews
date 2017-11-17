using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Data;

namespace NBAMatchDataGetter
{

    public class Getter
    {
        private static void saveMatchInfo(string id)
        {
            string url = string.Format("http://api.sports.sina.com.cn/?p=nba&s=live&a=matchInfo&format=json&id={0}&dpc=1", id);
            JsonSerializer serializer = new JsonSerializer();
            string jsonstr = WebHelper.getData(url, Encoding.UTF8);
            string cjsonstr= Regex.Unescape(jsonstr);
            JObject o = JObject.Parse(cjsonstr);
            string date = o["result"]["data"]["score"]["remain"]["date"].ToString();
            string time= o["result"]["data"]["score"]["remain"]["time"].ToString();
            
            int quarter = int.Parse(o["result"]["data"]["score"]["remain"]["quarter"].ToString());
            int minutes = int.Parse(o["result"]["data"]["score"]["remain"]["minutes"].ToString());
            int seconds = int.Parse(o["result"]["data"]["score"]["remain"]["seconds"].ToString());
            int period= int.Parse(o["result"]["data"]["score"]["remain"]["period"].ToString());
            int hostid= int.Parse(o["result"]["data"]["score"]["host"]["team_id"].ToString());
            int guestid = int.Parse(o["result"]["data"]["score"]["guest"]["team_id"].ToString());
            string team1 = o["result"]["data"]["matchInfo"]["team1_name"].ToString();
            int team1id = int.Parse(o["result"]["data"]["matchInfo"]["team1_id"].ToString());
            string team2 =o["result"]["data"]["matchInfo"]["team2_name"].ToString();
            int team2id = int.Parse(o["result"]["data"]["matchInfo"]["team2_id"].ToString());
            var guestscore = o["result"]["data"]["score"]["guest"]["scores"].ToArray();
            var hostscore = o["result"]["data"]["score"]["host"]["scores"].ToArray();

            // sort guest and host 
            string guestname = "";
            string hostname = "";
            if (guestid == team1id)
            {
                guestname = o["result"]["data"]["matchInfo"]["team1_name"].ToString();
                hostname = o["result"]["data"]["matchInfo"]["team2_name"].ToString();
            }
            else
            {
                hostname = o["result"]["data"]["matchInfo"]["team1_name"].ToString();
                guestname = o["result"]["data"]["matchInfo"]["team2_name"].ToString();
            }
            

            // set score
            int gscore = 0;
            int hscore = 0;
            foreach (var s in guestscore) gscore += int.Parse(s.ToString());
            foreach (var s in hostscore) hscore += int.Parse(s.ToString());

            // set date
            string begintime = date + " " + time;
            //DateTime dbegintime = Convert.ToDateTime(begintime);

            if(!MySqlHelper.ExistData(string.Format("SELECT id FROM nbamatch.team WHERE id='{0}'", guestid)))
            {
                MySqlHelper.Execute(string.Format("INSERT INTO nbamatch.team(id,name) VALUES('{0}','{1}')", guestid, guestname));
            }
            if (!MySqlHelper.ExistData(string.Format("SELECT id FROM nbamatch.team WHERE id='{0}'", hostid)))
            {
                MySqlHelper.Execute(string.Format("INSERT INTO nbamatch.team(id,name) VALUES('{0}','{1}')", hostid, hostname));
            }

            if (!MySqlHelper.ExistData(string.Format("SELECT id FROM nbamatch.match WHERE begin_time='{0}' and guest_team='{1}' and host_team='{2}' ", begintime, guestid, hostid)))
            {
                MySqlHelper.Execute(string.Format("INSERT INTO nbamatch.match(id,begin_time,guest_team,host_team,guest_score,host_score) VALUES('{0}','{1}','{2}','{3}','{4}','{5}')", 
                    id,begintime, guestid, hostid, gscore, hscore));
            }


            return;
        }
        /// <summary>  
        /// 时间戳Timestamp转换成日期  
        /// </summary>  
        /// <param name="timeStamp"></param>  
        /// <returns></returns>  
        private static DateTime GetDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime targetDt = dtStart.Add(toNow);
            return dtStart.Add(toNow);
        }

        private static DateTime GetDateTime(long timeStamp)
        {
            return GetDateTime(timeStamp.ToString());
        }

        private static void saveEventInfo(string id)
        {
            string url = string.Format("http://api.sports.sina.com.cn/pbp/?format=json&source=show&mid={0}&pid=&eid=0&dpc=1", id);
            JsonSerializer serializer = new JsonSerializer();
            string jsonstr = WebHelper.getData(url, Encoding.UTF8);
            string cjsonstr = Regex.Unescape(jsonstr);
            JObject o = JObject.Parse(cjsonstr);

            int num = int.Parse(o["result"]["data"]["last_eid"].ToString());
            var oe = o["result"]["data"]["pbp_msgs"].ToArray();
            for (int i = 0; i < oe.Length; i++)
            {
                JObject eo = JObject.Parse(oe[i].First().ToString());
                int period = int.Parse(eo["period"].ToString());
                string game_clock = eo["game_clock"].ToString();
                int home_score =int.Parse( eo["home_score"].ToString());
                int visitor_score = int.Parse(eo["visitor_score"].ToString());
                string description = eo["description"].ToString();
                int team_id = int.Parse(eo["team_id"].ToString());
                string last_name = eo["last_name"].ToString();
                long updated = long.Parse(eo["updated"].ToString());
                DateTime updatetime = GetDateTime(updated);
                int scored = int.Parse(eo["scored"].ToString());
                int personid = int.Parse(eo["person_id"].ToString());
                int event_num = int.Parse(eo["event_num"].ToString());

                int type = 0;
                if (!(personid==0) && !MySqlHelper.ExistData(string.Format("SELECT id FROM nbamatch.person WHERE name='{0}'", last_name)))
                {
                    MySqlHelper.Execute(string.Format("INSERT INTO nbamatch.person(id,name) VALUES('{0}','{1}')", personid, last_name));
                }

                if (!MySqlHelper.ExistData(string.Format("SELECT id FROM nbamatch.event WHERE match_id='{0}' and event_num='{1}' ", id, event_num)))
                {
                    MySqlHelper.Execute(string.Format("INSERT INTO nbamatch.event(match_id,period,time,game_time,type,team_id,person_id,description,gscore,hscore,event_num) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')",
                        id, period, updated, game_clock, type, team_id, personid, description, visitor_score, home_score, event_num));
                }
                else
                {

                }
                
            }
        }

        public static void getData(string id)
        {
            //id = "2017103005";
            try
            {
                saveMatchInfo(id);
                saveEventInfo(id);
            }
            catch
            {

            }

        }
    }
}
