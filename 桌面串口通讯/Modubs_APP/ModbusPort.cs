using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
namespace Modubs_APP
{
    class ModbusPort
    {
        //单例模式，只有一个实力
        private static ModbusPort _modbusport;
        public static ModbusPort Modbus
        {
            get
            {
                if (_modbusport == null)
                    return new ModbusPort();
                else
                    return _modbusport;
            }
        }
        private SerialPort port;//串口
        public String protName { get; set; }//串口名属性
        public int baudRate { get; set; }//波特率属性
        public Parity parity { get; set; }//奇偶校验属性
        public int dataBits { get; set; }//数据位属性
        public StopBits stopBits { get; set; }//停止位属性
        public int plcAddress { get; set; }//PLC地址属性
        private List<byte[]> writeCommand = new List<byte[]>();//及时写入命令队列
        private List<byte[]> readCommand = new List<byte[]>();//自动读取命令队列
        public ObservableCollection<int> plcWord = new ObservableCollection<int>(new List<int>(new int[65535]));//带通知集合的16位寄存器的值
       
        public ModbusPort()//构造函数
        {
            port = new SerialPort();
        }
    }
}
