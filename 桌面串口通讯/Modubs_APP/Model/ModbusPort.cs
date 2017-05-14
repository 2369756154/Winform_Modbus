using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;

namespace Modubs_APP
{
    public sealed class ModbusPort
    {

        /*
public string protName { get; set; }//串口名属性
public int baudRate { get; set; }//波特率属性
public Parity parity { get; set; }//奇偶校验属性
public int dataBits { get; set; }//数据位属性
public StopBits stopBits { get; set; }//停止位属性   
*/
       
        private int readCommand_i;//发送队列计数
        private int start;//PLC读取起始地址
        private int commandTime = 0;//通讯延迟
        private System.Timers.Timer timer;//检测超时
        private List<byte> bytesBuffer = new List<byte>();//接收缓冲区数据池
        private static object obj = new object();//用于程序加锁
        #region Modbus Master(RTU)命令功能码
        private struct FunCode
        {
            public const byte READCOIL = 0X01;//读多个线圈的功能
            public const byte READWORDS = 0X03;//读多个字功能码
            public const byte WRITECOIL = 0X05;//写单个线圈
            public const byte WRITEWORD = 0X06;//写单个寄存器
            public const byte WRITECOILS = 0X0F;//写多个线圈
            public const byte WRITEWORDS = 0X10;//写多个寄存器
        }
        #endregion
        private SerialPort port;//初始化串口
        private string _signal="无通讯";
        private List<byte[]> writeCommand = new List<byte[]>();//及时写入命令队列
        private List<byte[]> readCommand = new List<byte[]>();//自动读取命令队列
        private bool signalLight = false; //通信指示灯
        private byte PlcAddress { get; set; }//PLC地址属性
        /// <summary>
        /// 通信状态灯
        /// </summary>
        public bool SignalLight { get => signalLight; }
        /// <summary>
        /// 通信状态字
        /// </summary>
        public string Signal { get { return _signal; } } 
        /// <summary>
        /// 正整数地址集合取值范围0-65535
        /// </summary>
        public ObservableCollection<byte> PlcWord { get; set; }  
       
        //带通知集合的16位正整数寄存器的值     
        /// <summary>
        /// 构造函数
        /// </summary>
        /// 

            /*
        public ModbusPort(Form form)
        {
            port = new SerialPort();//实例化串口
            timer = new System.Timers.Timer();
            port.DataReceived += Port_DataReceived;//注册串口接收事件
            timer.Elapsed += Timer_Elapsed;
            PlcWord = new ObservableCollection<string>(new List<string>(new string[65535]));
          
        }
        */

        public ModbusPort()
        {
           
            port = new SerialPort();//实例化串口
            timer = new System.Timers.Timer();
          port.DataReceived += Port_DataReceived;//注册串口接收事件
            timer.Elapsed += Timer_Elapsed;
            PlcWord = new ObservableCollection<byte>(new List<byte>(new byte[131070]));
        }

      
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)//发送命令
        {
            if (port.IsOpen)
            {
                signalLight = false;
                try
                {
                    lock (obj)
                    {
                        if (writeCommand.Count > 0)
                        {
                            System.Threading.Thread.Sleep(commandTime);
                            port.Write(writeCommand[0], 0, writeCommand[0].Length);//如果及时写命令个数大于0，则立即发送写命令，并移除队列中的命令
                            start = (int)(writeCommand[0][2] << 8) + writeCommand[0][3];
                            writeCommand.RemoveAt(0);
                            signalLight = false;
                            
                        }
                        else
                        {
                            if (readCommand.Count > 0)
                            {
                                if (readCommand_i >= readCommand.Count)
                                {
                                    readCommand_i = 0;
                                }
                                System.Threading.Thread.Sleep(commandTime);
                                port.Write(readCommand[readCommand_i], 0, readCommand[readCommand_i].Length);//循环发送读取命令
                                start = (int)(readCommand[readCommand_i][2] << 8) + readCommand[readCommand_i][3];
                                readCommand_i++;                             
                                signalLight = false; 
                              
                            }
                        }


                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }
        }
       
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)//串口接收事件
        {
           
            if (((SerialPort)sender).IsOpen)
            {
             System.Threading.Thread.Sleep(10);
            signalLight = true;
            try
            {
                SerialPort port1 = sender as SerialPort;
                int lenght = port1.BytesToRead;
                byte[] buffer = new byte[lenght];
                port1.Read(buffer, 0, lenght);//读取缓冲区中的数据
                bytesBuffer.AddRange(buffer);//存入数据池
                while (bytesBuffer.Count>3)//当数据池里面的数据大于3个的时候开始处理数据
                {
                    if (bytesBuffer[0]==PlcAddress)
                    {
                        if (bytesBuffer[1]==FunCode.READWORDS)
                        {
                            int len = bytesBuffer[2];//返回的数据长度,3个帧头加两个校验码
                            if (bytesBuffer.Count>=len+5)
                            {
                                List<byte> L = bytesBuffer.ToList();//将数据池的数据复制到临时存放区
                                L.RemoveAt(L.Count - 1);
                                L.RemoveAt(L.Count - 1);
                                byte[] crc = CRC_16(L.ToArray());//计算返回的校验码
                                if (crc[0] == bytesBuffer[len+3] && crc[1] == bytesBuffer[len+4])
                                {
                                        for (int i = 0; i < len; i++)
                                        {
                                            PlcWord[start * 2 + i] = bytesBuffer[3 + i];
                                        }
                                    bytesBuffer.RemoveRange(0, len+5);
                                   
                                }
                                else
                                {
                                    bytesBuffer.Clear();
                                }
                            }
                           

                        }
                        bytesBuffer.Clear();
                      

                    }
                    else
                    {
                       
                        bytesBuffer.RemoveAt(0);
                    }
                }
                

              
            }
            catch
            {
                  // MessageBox.Show(ex.ToString());
                    return;
            }
            }

        }
    
        /// <summary>
        /// 打开串口操作方法
        /// </summary>
        /// /// <param name="plcAddress">PLC站号</param>
        /// <param name="portName">通信端口名称</param>
        /// <param name="baudRate">通信波特率</param>
        /// <param name="parity">奇偶校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="commandTime">设置命令延迟，单位为毫秒</param>
        /// <returns></returns>
        public bool OpenPort(byte plcAddress, string portName,int baudRate, Parity parity,int dataBits,StopBits stopBits,int commandTime)
        {
            try
            {
                if (port.IsOpen==true)
                {
                    port.Close();
                }
                PlcAddress = plcAddress;
                port.PortName = portName;
                port.BaudRate = baudRate;
                port.Parity = parity;
                port.DataBits = dataBits;
                port.StopBits = stopBits;
                port.Open();//打开串口
                timer.Interval = commandTime+60;//设置命令延迟 
                //System.Threading.Thread.Sleep(80);
                timer.Start();//启动定时器
                return true;//打开成功
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
                return false;//打开失败
            }
        }
        /// <summary>
        /// 关闭串口
        /// </summary>
        public void ClosePort()
        {
            try
            {
                timer.Stop();//停止线程
                port.DiscardOutBuffer();//丢弃缓冲区数据
                port.DiscardInBuffer();//丢弃缓冲区数据
                port.Close();//关闭串口
                readCommand.Clear();
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
           
        }
        /// <summary>
        /// 16位CRC校验码运算，返回结果，高字节为[0]，低字节在后[1]
        /// </summary>
        /// <param name="data">要校验的字节数组</param>
        /// <returns></returns>
        public  byte[] CRC_16(byte[] data)
        {
            uint IX, IY;
            ushort crc = 0xFFFF;//set all 1

            int len = data.Length;
            if (len <= 0)
                crc = 0;
            else
            {
                len--;
                for (IX = 0; IX <= len; IX++)
                {
                    crc = (ushort)(crc ^ (data[IX]));
                    for (IY = 0; IY <= 7; IY++)
                    {
                        if ((crc & 1) != 0)
                            crc = (ushort)((crc >> 1) ^ 0xA001);
                        else
                            crc = (ushort)(crc >> 1); //
                    }
                }
            }
            byte[] modbus_crc = new byte[2];
            modbus_crc[1] = (byte)((crc & 0xff00) >> 8);//高位置
            modbus_crc[0] = (byte)(crc & 0x00ff); //低位置
            return modbus_crc;
        }//Modbus RTU CRC16校验码算法
        /// <summary>
        /// //读多个保持型寄存器功能码03
        /// </summary>
        /// <param name="start">起始地址</param>
        /// <param name="number">读取个数</param>
        public void ReadWords(short start ,short number)
        {
            byte[] plc_start = BitConverter.GetBytes(start);
            byte[] plc_number = BitConverter.GetBytes(number);
            byte[] datas = { Convert.ToByte(PlcAddress), FunCode.READWORDS, plc_start[1], plc_start[0], plc_number[1], plc_number[0] };
            List<byte> l = new List<byte>();
            l.AddRange(datas);
            l.AddRange(CRC_16(datas));
            byte[] dataFrames = l.ToArray();
            readCommand.Add(dataFrames);
        }
    }
}
