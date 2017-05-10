using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
namespace Modubs_APP
{
  public sealed class ModbusPort
    {
        //单例模式，只有一个实例
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
        private SerialPort port;//串口
        public string Signal { get; set; }//通信状态字
        public string protName { get; set; }//串口名属性
        public int baudRate { get; set; }//波特率属性
        public Parity parity { get; set; }//奇偶校验属性
        public int dataBits { get; set; }//数据位属性
        public StopBits stopBits { get; set; }//停止位属性
        public ushort plcAddress { get; set; }//PLC地址属性
        private List<byte[]> writeCommand = new List<byte[]>();//及时写入命令队列
        private List<byte[]> readCommand = new List<byte[]>();//自动读取命令队列
        public ObservableCollection<ushort> plcWord = new ObservableCollection<ushort>(new List<ushort>(new ushort[65535]));//带通知集合的16位正整数寄存器的值     
        public ModbusPort()//构造函数
        {
            port = new SerialPort();
        }

    }
}
