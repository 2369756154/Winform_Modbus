using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modubs_APP
{
    class Verify
    {
        /// <summary>
        /// 设置软件试用期限
        /// </summary>
        /// <param name="form1">主窗口Name</param>
        /// <param name="maturityTime">试用时长单位为分钟</param>
        /// <returns></returns>
        public string RunDatetime(Form form1,double maturityTime)
        {
           try
           {
                if (File.Exists(@"C:\Windows\System32\maturity.dll"))
                {
                    using (FileStream fileStream = new FileStream(@"C:\Windows\System32\maturity.dll", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        BinaryReader binaryReader = new BinaryReader(fileStream);
                        string time = binaryReader.ReadString();
                        binaryReader.Close();
                        DateTime maturityDate = Convert.ToDateTime(time);
                        if (DateTime.Now.CompareTo(maturityDate) >= 0)
                        {
                            MessageBox.Show("试用时间已到期,请联系开发者");
                            form1.Close();
                        }
                        return time;
                    }
                }
                else
                {

                    using (FileStream fileStream = new FileStream(@"C:\Windows\System32\maturity.dll", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))

                    {
                        DateTime dataTime = DateTime.Now.AddMinutes(maturityTime);
                        string time = dataTime.ToString("yyyy年MM月dd日 HH:mm:ss");
                        BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                        binaryWriter.Write(time);
                        binaryWriter.Close();
                        MessageBox.Show(String.Format("该软件试用时间{0}分钟",maturityTime.ToString()));
                        return dataTime.ToString("yyyy年MM月dd日 HH:mm:ss");
                    }
                }
            }
            catch(Exception ex)
           {
               MessageBox.Show(ex.ToString());
                form1.Close();
               return string.Empty;
            }
        }
    }
}
