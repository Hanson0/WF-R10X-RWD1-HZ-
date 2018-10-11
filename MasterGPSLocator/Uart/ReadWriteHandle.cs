using MasterGPSLocator.Config;
using Production.SerialPortNS;
using Production.Windows;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterGPSLocator.Uart
{
    class ReadWriteHandle
    {
        private MainForm frmMain;

        private SerialPort sp;
        private string recive;                      //接收字符串

        private bool flagCyclic;                    //开关循环检测模块上掉电的标志位

        private bool flagDisplayUart;               //是否显示串口输出信息

        private static int checkDeviceInterval;
        private bool isTimeOut = false;
        private string poweronFlag;
        private string powerDownFlag;

        private string testItemFlag;

        private const string strSendData = "5a 00 28 00 00 13 61 62 63 64 65 66 67 68 69 6a 6b 6c 6d 6e 00 00 61 62 63 64 65 31 32 33 34 35 00 00 00 00 00 00 d2 5b";

        public string PoweronFlag
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                Win32API.GetPrivateProfileString("KeyWords", "PowerOnFlag", "", stringBuilder, 256, ConfigInfo.ConfigPath);
                poweronFlag = stringBuilder.ToString().Trim();
                return poweronFlag;
            }
        }
        public string PowerDownFlag
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                Win32API.GetPrivateProfileString("KeyWords", "PowerDownFlag", "", stringBuilder, 256, ConfigInfo.ConfigPath);
                powerDownFlag = stringBuilder.ToString().Trim();
                powerDownFlag = powerDownFlag.Replace("\"", "");
                return powerDownFlag;
            }
        }

        public string TestItemFlag
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                Win32API.GetPrivateProfileString("KeyWords", "TestItemFlag", "", stringBuilder, 256, ConfigInfo.ConfigPath);
                testItemFlag = stringBuilder.ToString().Trim();
                return testItemFlag;
            }
        }

        public bool IsTimeOut
        {
            get { return isTimeOut; }
            set { isTimeOut = value; }
        }
        public bool FlagDisplayUart
        {
            get { return flagDisplayUart; }
            set { flagDisplayUart = value; }
        }

        public ReadWriteHandle(MainForm frmMain)
        {
            ReadWriteHandInfo.ReadConfig();
            checkDeviceInterval = ReadWriteHandInfo.CheckDeviceInterval;

            this.frmMain = frmMain;
            flagDisplayUart = true;
            sp = SerialPortFactory.GetSerialPort();
            sp.DataReceived += Sp_DataReceived;
            SpOpen();
        }
        /// <summary>
        /// 静态构造函数
        /// </summary>
        //static ReadWriteIdHandle()
        //{
        //    //atSnRead = new ATReadCmd(ATReadCmd.ReadIdType.SnRead, "AT+CBSN\r\n", "+CBSN:");
        //    //atIccidRead = new ATReadCmd(ATReadCmd.ReadIdType.IccidRead, "AT+CCID\r\n", "+CCID:");
        //    //atImeiRead = new ATReadCmd(ATReadCmd.ReadIdType.ImeiRead, "AT+CGSN\r\n", "+CGSN:");
        //    //atEidRead = new ATReadCmd(ATReadCmd.ReadIdType.EidRead, "AT+CEID\r\n", "+CEID:");
        //    //atVersonRead = new ATReadCmd(ATReadCmd.ReadIdType.VersonRead, "AT+CGMR\r\n", VersonStart);

        //    //atImeiWrite = new ATWriteCmd(ATWriteCmd.WriteIdType.ImeiWrite, "AT+EGMR=1,7,\"", "\"\r\n");
        //    //atSnWrite = new ATWriteCmd(ATWriteCmd.WriteIdType.SnWrite, "AT+EGMR=1,5,\"", "\"\r\n");
        //}


        public void SpOpen()
        {
            try
            {
                sp.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 串口接收处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int n = sp.BytesToRead;
            byte[] buf = new byte[n];
            sp.Read(buf, 0, n);
            //转16进制
            StringBuilder sb = new StringBuilder();
            foreach (var itemData in buf)
            {
                sb.AppendFormat("{0:x2} ", itemData);
            }
            string reciveRaw = sb.ToString();
            if (FlagDisplayUart)
            {
                frmMain.DisplayUart(reciveRaw);
            }
            recive += reciveRaw;

            //string reciveRaw = Encoding.ASCII.GetString(buf);

            //if (FlagDisplayUart)
            //{
            //    frmMain.DisplayUart(reciveRaw);
            //}
            //recive += reciveRaw;
        }
        /// <summary>
        /// 检测模块上电状态
        /// </summary>
        /// <returns></returns>
        public void CheckModulePowerOn()
        {
            //string readEid = atEidRead.Cmd;
            flagCyclic = true;
            //sp.DiscardInBuffer();
            //recive = string.Empty;

            do
            {
                //sp.Write(readEid);
                Console.WriteLine("正在检测上电");
                Thread.Sleep(checkDeviceInterval);
            } while (!frmMain.IsPressSpace && flagCyclic);



            //do
            //{
            //    Console.WriteLine("监测到模块已上电，正在采集可用数据");
            //    Thread.Sleep(checkDeviceInterval);
            //} while (!recive.Contains("$TDINF,Techtotop Multi-GNSS Receiver") && flagCyclic && !isTimeOut);

            //return 
        }
        /// <summary>
        /// 检测是否有回复功能
        /// </summary>
        public void CheckModuleIsOk()
        {
            //string readEid = atEidRead.Cmd;
            flagCyclic = true;
            sp.DiscardInBuffer();
            recive = string.Empty;
            byte[] bytesSendData = null;
            bytesSendData = strToHexByte(strSendData);
            SendData(bytesSendData);
            do
            {
                recive = string.Empty;
                //sp.Write()
                Console.WriteLine("正在检测是否有回复功能");
                Thread.Sleep(checkDeviceInterval);
            } while (!recive.Contains("5a") && flagCyclic && !isTimeOut);
        }

        public bool CheckUart()
        {
            if (!sp.IsOpen)
            {
                try
                {
                    sp.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                    return false;
                }
            }
            return true;
        }
        public bool SendData(byte[] data)
        {
            if (sp.IsOpen)
            {
                try
                {
                    sp.Write(data, 0, data.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("串口未打开", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            return false;
        }
        /// <summary>
        /// 字符串转16进制
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private byte[] strToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
            {
                hexString += " ";
            }
            byte[] returnBytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length / 2; i++)
            {
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Replace(" ", ""), 16);
            }
            return returnBytes;
        }

        /// <summary>
        /// 检测模块掉电
        /// </summary>
        public void CheckModulePowerOff()
        {
            //string readEid = atEidRead.Cmd;
            flagCyclic = true;
            sp.DiscardInBuffer();
            recive = string.Empty;

            do
            {
                recive = string.Empty;
                //sp.Write(readEid);
                Console.WriteLine("正在检测掉电");
                Thread.Sleep(checkDeviceInterval);
            } while (!recive.Contains("\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0") && flagCyclic);
        }


        /// <summary>
        /// 读取模块ID
        /// </summary>
        /// <param name="atReadCmd"></param>
        /// <returns></returns>
        public List<string> ReadId()
        {
            string keyWord = TestItemFlag;
            string[] spit = { "\r\n" };
            int pos;
            string[] line;
            string temp;
            //string dataValue;
            List<string> listValue = new List<string>();
            //    Console.WriteLine("监测到模块已上电，正在采集可用数据");
            do
            {
                //借助关键字符串提取出相应的ID
                line = recive.Split(spit, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < line.Length; i++)
                {
                    if ((pos = line[i].IndexOf(keyWord)) >= 0)
                    {
                        temp = line[i].Substring(pos + keyWord.Length);
                        if (temp.Length > 14)
                        {
                            //dataValue = temp;
                            listValue.Add(temp);
                        }
                        //id = System.Text.RegularExpressions.Regex.Replace(temp, @"[^0-9A-Z]", "");
                    }
                }
                Console.WriteLine("监测到模块已上电，正在采集可用数据");
                Thread.Sleep(checkDeviceInterval);
            } while (!(listValue.Count > 3) && flagCyclic && !isTimeOut);

            return listValue;

        }




    }
}
