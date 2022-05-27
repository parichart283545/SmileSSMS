using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Configuration;
using SmileSSMSSendList.SMSService;
using System.Xml;
using System.Text.RegularExpressions;

namespace SmileSSMSSendList
{
    public partial class SMSSendListService : ServiceBase
    {
        public SMSSendListService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.WriteToFile("Service started {0}");
            this.ScheduleService();
        }

        protected override void OnStop()
        {
            this.WriteToFile("Service stopped {0}");
            this.Schedular.Dispose();
        }

        private Timer Schedular;

        public void ScheduleService()
        {
            try
            {
                Schedular = new Timer(new TimerCallback(SchedularCallback));
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                this.WriteToFile("Service Mode: " + mode + " {0}");

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(ConfigurationManager.AppSettings["ScheduledTime"]);

                    //send sms list
                    SendSMSList();

                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Minutes from AppSettings.
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);

                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);

                    //send sms list
                    SendSMSList();

                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
                string schedule = string.Format("{0} day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                this.WriteToFile("Service scheduled to run after: " + schedule + " {0}");

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                Schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                WriteToFile("ScheduleService Error on: {0} " + ex.Message + ex.StackTrace);

                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("SMSSendListService"))
                {
                    serviceController.Stop();
                }
                RestartService("SMSSendListService");
            }
        }

        private void SchedularCallback(object e)
        {
            this.WriteToFile("Service Log: {0}");
            this.ScheduleService();
        }

        private void WriteToFile(string text)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog.txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    sw.Close();
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    sw.Close();
                }
            }
        }

        private void RestartService(string servicename)
        {
            ServiceController controller = new ServiceController(servicename);
            try
            {
                if ((controller.Status.Equals(ServiceControllerStatus.Running)) || (controller.Status.Equals(ServiceControllerStatus.StartPending)))
                {
                    controller.Stop();
                }
                controller.WaitForStatus(ServiceControllerStatus.Stopped);
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running);
            }
            catch
            {
                this.WriteToFile("Cannot restart service " + servicename);
            }
        }

        private void SendSMSList()
        {
            using (var db = new CommunicateV1Entities())
            {
                try
                {
                    //GET DATA AND CREATE TEXT FILE
                    var lstData = db.usp_SMSQueueDetail_Select().ToList();
                    if (lstData.Count > 0)
                    {
                        var QueueSentId = lstData[0].SMSQueueSentId;
                        //string path = @"c:\temp\MyTest.txt";
                        var strDatetime = DateTime.Now.ToString();
                        var name = Regex.Replace(strDatetime, @"[:/\, ]", "");
                        var path = AppDomain.CurrentDomain.BaseDirectory + "\\LogsFile";

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        string filepath = AppDomain.CurrentDomain.BaseDirectory + String.Format("\\LogsFile\\LOG_SendSMSList_QueueSent_{0}_DateTime_{1}.txt", QueueSentId, name);
                        var fi = new FileInfo(filepath);
                        // Check if file already exists. If yes, delete it.
                        if (System.IO.File.Exists(filepath))
                        {
                            System.IO.File.Delete(filepath);
                        }

                        // Create a file to write to.
                        using (StreamWriter sw = System.IO.File.CreateText(filepath))
                        {
                            foreach (var i in lstData)
                            {
                                var str = String.Format("{0},{1}", i.PhoneNo, i.Message);
                                sw.WriteLine(str);
                            }
                        }
                        var createBy = 1;
                        var senderId = 3; //3=SiamSmile
                        var sendResult = db.usp_SendSMS_Insert(QueueSentId, createBy, senderId).FirstOrDefault();

                        if (sendResult.IsResult.Value == true)
                        {
                            this.WriteToFile("Insert Success[usp_SendSMS_Insert] {0}");
                            //Send SMS
                            var PathFile = filepath;
                            var sr = new StreamReader(PathFile, System.Text.Encoding.UTF8);
                            var StrData = sr.ReadToEnd();
                            sr.Close();

                            var ByteData = System.Text.ASCIIEncoding.GetEncoding("TIS-620").GetBytes(StrData);
                            StrData = Convert.ToBase64String(ByteData);

                            var Username = ConfigurationManager.AppSettings["Username"];
                            var Password = ConfigurationManager.AppSettings["Password"];
                            var FileData = StrData;
                            var FileName = fi.Name;
                            var SenderName = "SiamSmile";
                            var SendDate = "";

                            var ObjSMS = new SMSServicePortTypeClient();
                            var Result = ObjSMS.sendSMSFile(Username, Password, FileData, FileName, SenderName, SendDate);

                            var XML = new XmlDocument();
                            XML.LoadXml(Result);
                            var Element = XML.DocumentElement;

                            var resultTranHeaderUpdate = db.usp_TransactionHeader_Update(Convert.ToInt32(sendResult.Result), Element.OuterXml).FirstOrDefault();
                            if (resultTranHeaderUpdate.IsResult == true)
                            {
                                this.WriteToFile("Service Sent SMS Success[ReferenceID=" + resultTranHeaderUpdate.Result + "] {0}");
                                this.WriteToFile("TransactionHeader_Update Success {0}");
                            }
                            else
                            {
                                this.WriteToFile("TransactionHeader_Update Fail {0}");
                            }
                        }
                        else
                        {
                            this.WriteToFile("Insert Fail[usp_SendSMS_Insert] {0}");
                        }
                    }
                    else
                    {
                        this.WriteToFile("No Data[Data.Count = 0] {0}");
                    }
                }
                catch (Exception e)
                {
                    this.WriteToFile("SendSMSList Error " + e.Message + " {0}");
                }
            }
        }
    }
}