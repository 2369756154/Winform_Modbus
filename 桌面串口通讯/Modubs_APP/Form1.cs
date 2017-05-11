using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modubs_APP
{
    public partial class frm_setting : Form
    {
        Timer timer;
        private string _datatime;
        public frm_setting()
        {
            InitializeComponent();
            timer = new Timer() { Interval = 500 };
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.CompareTo(Convert.ToDateTime(_datatime))>=0)
            {
                timer.Stop();
                MessageBox.Show("使用期限已到，请联系开发者");
                this.Close();
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            ModbusPort.Modbus.OpenPort(1,"COM1", 9600, System.IO.Ports.Parity.Even, 8, System.IO.Ports.StopBits.One,0);
            ModbusPort.Modbus.ReadWords(0, 1);
        }

        private void Form_Load(object sender, EventArgs e)
        {
           Verify verify = new Verify();
           _datatime= verify.RunDatetime(this, 8);
            textBox1.Text = "到期时间：" + _datatime;
            timer.Start();
        }
    }
}
