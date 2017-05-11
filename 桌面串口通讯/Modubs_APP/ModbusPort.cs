using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Timers;
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
        //单例模式，只有一个实例
        private int readCommand_i;
        private static object obj = new object();//用于程序加锁
        private static ModbusPort _modbusport;
        public static ModbusPort Modbus
        {
            get
            {
                if (_modbusport == null)
                {
                    _modbusport = new ModbusPort();
                }
                    return _modbusport;
            }
        }
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
        private Timer timer;//初始化定时器
        public string Signal { get; set; }//通信状态字
        private ushort PlcAddress { get; set; }//PLC地址属性
        private List<byte[]> writeCommand = new List<byte[]>();//及时写入命令队列
        private List<byte[]> readCommand = new List<byte[]>();//自动读取命令队列
        public ObservableCollection<ushort> PlcWord = new ObservableCollection<ushort>(new List<ushort>(new ushort[65535]));//带通知集合的16位正整数寄存器的值     
        public ModbusPort()//构造函数
        {
            port = new SerialPort();//实例化串口
            timer = new Timer();//实例化计时器
            timer.Elapsed += Timer_Elapsed;//注册定时器事件
            port.DataReceived += Port_DataReceived;//注册串口接收事件
        }
        
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)//串口触发事件
        {
            
        }  

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)//定时器触发事件
        {
            try
            {
                lock (obj)
                {
                    if (writeCommand.Count > 0)
                    {
                        port.Write(writeCommand[0], 0, writeCommand[0].Length);//如果及时写命令个数大于0，则立即发送写命令，并移除队列中的命令
                        writeCommand.RemoveAt(0);

                    }
                    else
                    {
                        if (readCommand.Count > 0)
                        {
                            port.Write(readCommand[readCommand_i], 0, readCommand[readCommand_i].Length);//循环发送读取命令
                            readCommand_i++;
                            if (readCommand_i >= readCommand.Count)
                            {
                                readCommand_i = 0;
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {

                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
           
        }

        /// <summary>
        /// 打开串口操作方法
        /// </summary>
        /// <param name="portName">通信端口名称</param>
        /// <param name="baudRate">通信波特率</param>
        /// <param name="parity">奇偶校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="commandTime">命令间隔时间,默认60毫秒</param>
        /// <returns></returns>
        public bool OpenPort(ushort plcAddress, string portName,int baudRate, Parity parity,int dataBits,StopBits stopBits,int commandTime)
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
                timer.Interval = commandTime + 60;
                port.Open();
                timer.Start();//启动定时器
                return true;//打开成功
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
                return false;//打开失败
            }
        }

        public void ClosePort()
        {
            try
            {
                timer.Stop();
                port.Close();
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
           
        }//关闭串口

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
        }//读多个保持型寄存器
    }
}
