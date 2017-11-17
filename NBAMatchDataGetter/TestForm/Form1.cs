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
            Getter.getData(input);
            log(input + " 获取完毕");
        }

        private void button2_Click(object sender, EventArgs e)
        {


            string input = textBox1.Text;
            gen.init(input);
            string output = gen.getNews();
            log(input + " 新闻生成完毕");
            print(output);
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
