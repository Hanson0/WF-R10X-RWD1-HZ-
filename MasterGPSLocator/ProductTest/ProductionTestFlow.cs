using MasterGPSLocator.Config;
using MasterGPSLocator.Uart;
using Production.ProductionTest;
using Production.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterGPSLocator.ProductTest
{
    class ProductionTestFlow
    {
        private MainForm frmMain;
        private int StandardCN0;

        private string configPath = ConfigInfo.ConfigPath;            //配置文件路径
        private bool flagCreateNewRow;

        private bool runState;//运行状态     true为运行中，false为未运行
        private ReadWriteHandle readWriteIdHandle;
        private string labelSn;
        public int StandardCN01
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                Win32API.GetPrivateProfileString("Standard", "Parameter", "", stringBuilder, 256, configPath);
                try
                {
                    StandardCN0 = int.Parse(stringBuilder.ToString().Trim());
                }
                catch (Exception)
                {
                    StandardCN0 = 40;
                }
                return StandardCN0;
            }
        }
        public ProductionTestFlow(MainForm frmMain)
        {
            this.frmMain = frmMain;

            runState = false;
            readWriteIdHandle = new ReadWriteHandle(frmMain);

            InitTimeOutTimer();
        }
        private System.Timers.Timer timeoutTimer = new System.Timers.Timer();
        private void InitTimeOutTimer()
        {
            timeoutTimer.AutoReset = true;
            timeoutTimer.Interval = ProductionInfo.TimeOut;
            timeoutTimer.Enabled = false;
            timeoutTimer.Elapsed += TimeoutTimer_Elapsed;
        }
        private void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            readWriteIdHandle.IsTimeOut = true;

        }

        public void StartTimeoutTimer()
        {
            readWriteIdHandle.IsTimeOut = false;
            timeoutTimer.Start();
        }
        public void StopTimeOutTimer()
        {
            timeoutTimer.Stop();
        }

        /// <summary>
        /// 检测模块上电
        /// </summary>
        public void CheckModulePowerOn()
        {
            readWriteIdHandle.CheckModulePowerOn();
        }
        /// <summary>
        /// 检测模块掉电
        /// </summary>
        public void CheckModulePowerOff()
        {
            readWriteIdHandle.CheckModulePowerOff();
        }

        /// <summary>
        /// 任务入口及任务流执行后处理
        /// </summary>
        public void TestTaskMain(string labelSn)
        {
            this.labelSn = labelSn;
            runState = true;
            flagCreateNewRow = false;

            readWriteIdHandle.FlagDisplayUart = true;

            //执行流程
            int ret = TestTaskFlow();

            //frmMain.DisplayLog(string.Format("标签IMEI：{0}\r\n", labelImei));
            //结果判断
            Production.Result.ResultJudge resultJudge = Production.Result.ResultJudge.GetResultJudge(frmMain);
            if (ProductionInfo.Type == ProductionInfo.SystemType.iMES || ProductionInfo.Type == ProductionInfo.SystemType.GSMMES)
            {
                resultJudge.Sn = labelSn;
                //resultJudge.Sn = snRead;
                //resultJudge.Eid = eidRead;
                //resultJudge.Iccid = iccidRead;
            }
            resultJudge.PutResult(labelSn, ret);

            runState = false;
            readWriteIdHandle.FlagDisplayUart = false;
            frmMain.IsPressSpace = false;

        }

        /// <summary>
        /// 任务流
        /// </summary>
        /// <returns></returns>
        private int TestTaskFlow()
        {
            int ret = -1;
            //string pattern = null;
            //eidRead = "NULL";
            frmMain.DisplayLog("已检测到按键，测试中...\r\n");

            if (!readWriteIdHandle.CheckUart())
            {
                return ret;
            } 

            readWriteIdHandle.CheckModuleIsOk();
            //do
            //{

            //    frmMain.DisplayLog("正在发送...\r\n");


            //    //frmMain.DisplayLog(string.Format("已获取模块EID：{0}\r\n", eidRead));
            //    //frmMain.SetText(AllForms.EnumControlWidget.txtEid.ToString(), eidRead, false);



            //} while (false);


            //两种情况进入此处:1、超时跳出了do while,2、获取想要到 value
            if (readWriteIdHandle.IsTimeOut)
            {
                frmMain.DisplayLog("测试超时\r\n");
                return ret;
            }

            ProductionInfo.SystemType systemType = ProductionInfo.Type;

            //使用do..while(false)的原因，是为了当测试流程为fail时，使用break跳出该结构，仍然执行结果上报
            ret = 0;
            return ret;
        }








    }
}
