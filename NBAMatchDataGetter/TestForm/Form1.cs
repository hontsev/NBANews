using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBAMatchDataGetter;
using System.IO;
using System.Diagnostics;

namespace TestForm
{
    public partial class Form1 : Form
    {
        public Generater gen;

        public delegate void sendStringDelegate(string str);

        public void log(string str)
        {
            if (textBox2.InvokeRequired)
            {
                sendStringDelegate me = new sendStringDelegate(log);
                Invoke(me, (object)str);
            }
            else
            {
                textBox2.AppendText(str + "\r\n");
            }
        }

        public void print(string str)
        {
            if (textBox3.InvokeRequired)
            {
                sendStringDelegate me = new sendStringDelegate(print);
                Invoke(me, (object)str);
            }
            else
            {
                textBox3.Text=str;
            }
        }

        public Form1()
        {
            InitializeComponent();
            gen = new Generater();
        }

        private void button1_Click(object sender, EventArgs e)
        {
           
            string input = textBox1.Text;
            log(input + " 开始获取比赛信息");
            Getter.getData(input);
            log(input + " 获取完毕");
        }

        private void drawImage(int[] data)
        {
            if (data.Length <= 0) return;
            int sampleLength = data.Length;

            int w = sampleLength * 2;
            
            int hscale = 4;
            int h = hscale * 2 * 40;
            //h = w / 6;
            Bitmap pic = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(pic);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            
            //draw x,y
            g.DrawLine(new Pen(Color.Black, 1), new Point(0, 0), new Point(0, h));
            g.DrawLine(new Pen(Color.Black, 1), new Point(0, h / 2), new Point(w, h / 2));

            //draw wave
            for (int i =  1; i < sampleLength; i++)
            {
                Pen p = new Pen(Color.Green, 1);
                g.DrawLine(p,
                    new Point((int)((i - 1) * w / sampleLength), h / 2 + data[i - 1]* hscale),
                    new Point((int)(i * w / sampleLength), h / 2 + data[i]* hscale));

            }

            //draw cut
            int[] res = gen.getCutQuarterByTime();
            for (int i = 0; i < res.Length; i++)
            {
                if (res[i] > 0.5)
                {
                    g.DrawLine(new Pen(Color.Black, 5),
                    new Point((int)(((double)res[i]) * w / sampleLength), 0),
                    new Point((int)(((double)res[i]) * w / sampleLength), h));
                }
            }

            
            res = gen.getCutSlice1ByTime();
            for (int i = 0; i < res.Length; i++)
            {
                if (res[i] > 0.5)
                {
                    g.DrawLine(new Pen(Color.Gray, 1),
                    new Point((int)(((double)res[i]) * w / sampleLength), 0),
                    new Point((int)(((double)res[i]) * w / sampleLength), h));
                }
            }


            res = gen.getCutSlice2ByTime();
            for (int i = 0; i < res.Length; i++)
            {
                if (res[i] > 0.5)
                {
                    g.DrawLine(new Pen(Color.Red, 3),
                    new Point((int)(((double)res[i]) * w / sampleLength), 0),
                    new Point((int)(((double)res[i]) * w / sampleLength), h));
                }
            }


            g.Dispose();
            pictureBox1.Image = pic;
            pictureBox1.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            string input = textBox1.Text;
            ////get
            //log(input + " 开始获取比赛信息");
            //Getter.getData(input);
            //stopwatch.Stop();
            //log(input + " 获取完毕  用时" + stopwatch.ElapsedMilliseconds + " ms");
            //stopwatch.Restart();
            
            //generate
            log(input + " 开始生成新闻");
            gen.init(input);
            string output = gen.getNews();
            stopwatch.Stop();
            log(input + " 新闻生成完毕  用时"+ stopwatch.ElapsedMilliseconds+" ms");
            print(output);

            drawImage(gen.getDataByTime());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] templates = textBox3.Text.Replace("\n", "").Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in templates)
            {
                var tmp = t.Split('\t');
                MySqlHelper.Execute(string.Format("INSERT INTO nbamatch.template(pattern,sentence) VALUES('{0}','{1}')", tmp[0], tmp[1]));
            }
            textBox3.Clear();
            log("模板添加完毕");
        }
    }
}
