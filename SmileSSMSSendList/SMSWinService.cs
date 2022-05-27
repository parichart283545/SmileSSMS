using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SmileSSMSSendList
{
    public class SMSWinService : ServiceBase
    {
        #region Declaration
        //
        public const string GeneralDatetimeFormat = "yyyy-MM-dd HH:mm:ss";
        private Timer Schedular;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool IsDisposed = false;
        //private bool IsTest = false;
        private string providerIdString = "3";
        private char[] MyChar = { '﻿' };//Trim start and end of string
                                        //private StreamWriter outputFile;
        private int delayMillisecond = 10; //Default
        private int taskCount = 10; //Default
        private static bool IsDebugMode = true;
        private bool IsRunning = false;
        private List<string> lstMessage = new List<string>();


        //Priority
        private const string SettingPriorityOnline = "online";
        private const string SettingPriorityNormal = "normal";
        private const string SettingPriorityOtp = "otp";
        //Configuration
        private string SettingUsername = "";
        private string SettingPassword = "";
        private const string SettingSenderName = "Siamsmile";
        //Language
        private const string SettingLanguageThaiI = "th";
        private const string SettingLanguageEng = "en";
        private const string SettingLanguageUnicode = "unicode";
        //SMS Type of massage
        private const string SettingMessageTypeSMS = "sms";
        private const string SettingMessageTypeURL = "url";
        //Operator code
        private const string SettingOperationAIS = "1";//AIS, GSM, ONE2CALL, DPC
        private const string SettingOperationDTAC = "2";
        private const string SettingOperationTRUE = "3";//TRUEMOVE H , TRUEMOVE, ORANGE
        private const string SettingOperationAll = "6";

        //Subject message
        private const string SettingSubjectMessage = "SSB";

        #endregion

        #region Event
        //
        protected override void OnStart(string[] args)
        {
            WriteLog("Service started || this program update version on 25/7/64 10.28 AM");
            IsRunning = true;
            InitailSMS();
        }

        protected override void OnStop()
        {
            WriteLog("Service stopped");
            if (!IsDisposed)
                cts.Cancel();
            this.Schedular.Dispose();
        }

        private void RestartService(string servicename)
        {
            WriteLog("Restart service");
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
                WriteLog("Cannot restart service " + servicename);
            }
        }

        private void SchedularCallback(object state)
        {
            WriteLog("Start Working");
            if (!IsRunning)
                InitailSMS();
            //cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        #endregion  

        #region Method
        //
        private void InitailSMS()
        {
            try
            {
                //Load data from app seting
                delayMillisecond = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadDelayMillisecond"].ToString());
                WriteLog("Service Thread delay : " + delayMillisecond.ToString());
                taskCount = Convert.ToInt32(ConfigurationManager.AppSettings["TaskCount"].ToString());
                WriteLog("Service Task Count : " + taskCount.ToString());
                IsDebugMode = ConfigurationManager.AppSettings["DebugMode"].ToString() == "0" ? false : true;
                WriteLog("Service Debug mode : " + (IsDebugMode == true ? "On" : "Off"));
                SettingUsername = ConfigurationManager.AppSettings["ShineeUsername"].ToString();
                SettingPassword = ConfigurationManager.AppSettings["ShineePassword"].ToString();

            }
            catch (Exception ex)
            {
                WriteLog("Read configuration fail: " + ex.Message + " | " + ex.StackTrace, true);
            }

            try
            {
                if (ConfigurationManager.AppSettings["CurrentProvider"] != "")
                {
                    providerIdString = ConfigurationManager.AppSettings["CurrentProvider"];
                }
                WriteLog($"Current provider id is {providerIdString}");

                Schedular = new Timer(new TimerCallback(SchedularCallback));
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                WriteLog("Service Mode: " + mode);

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd") + " " + ConfigurationManager.AppSettings["ScheduledTime"]);

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

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now).Duration();


                string schedule = string.Format("{0} day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                WriteLog("Service scheduled to run after: " + schedule);

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                Schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                WriteLog("ScheduleService Error on: " + ex.Message + ex.StackTrace, true);

                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("SMSSendListService"))
                {
                    serviceController.Stop();
                }
                RestartService("SMSSendListService");
            }
            finally { IsRunning = false; }
        }

        private void SendSMSList()
        {
            using (var db = new CommunicateV1Entities())
            {
                try
                {
                    //GET DATA AND CREATE TEXT FILE
                    var AllData = db.usp_SMSQueueDetail_Select().ToList();

                    //if set ProviderId is -1 then the program will be use "providerIdString" value from configuration file
                    var countDefault = AllData.Where(x => x.ProviderId == -1).Count();
                    List<usp_SMSQueueDetail_Select_Result> mktLst = new List<usp_SMSQueueDetail_Select_Result>();
                    List<usp_SMSQueueDetail_Select_Result> shineeLst = new List<usp_SMSQueueDetail_Select_Result>();
                    WriteLog($"Default sms get: {countDefault} messages", true);
                    if (countDefault > 0)
                    {
                        if (providerIdString == "2")
                        {
                            mktLst = AllData.Where(x => x.ProviderId == 2 || x.ProviderId == -1).ToList();
                            shineeLst = AllData.Where(x => x.ProviderId == 3).ToList();
                        }
                        else
                        {//providerIdString == "3" or other
                            mktLst = AllData.Where(x => x.ProviderId == 2).ToList();
                            shineeLst = AllData.Where(x => x.ProviderId == 3 || x.ProviderId == -1).ToList();
                        }
                    }
                    else
                    {
                        mktLst = AllData.Where(x => x.ProviderId == 2).ToList();
                        shineeLst = AllData.Where(x => x.ProviderId == 3).ToList();
                    }


                    //ClickNext process
                    if (mktLst.Count > 0)
                    {
                        WriteLog($"ClickNext sms get: {mktLst.Count} messages", true);
                        //SmsClicknext.SendSMSClickNextList(mktLst); // api old version
                        SmsClicknextV2.SendSMSClickNextList(mktLst);
                        return;
                    }


                    //Shinee Process
                    int pCount = shineeLst.Count;
                    if (pCount > 0)
                    {
                        WriteLog($"Shinee sms get: {shineeLst.Count} messages", true);
                        WriteLog("Shinee sms start");

                        var QueueSentId = shineeLst[0].SMSQueueSentId;
                        //Split list
                        decimal splitCount = pCount / taskCount;
                        int sepInt = (int)Math.Ceiling(splitCount);
                        if (splitCount <= 0) sepInt = 1;
                        var listInList = Split(shineeLst, sepInt);

                        if (IsDisposed)
                        {
                            cts = new CancellationTokenSource();
                        }
                        IsDisposed = false;
                        CancellationToken token = cts.Token;
                        var tasks = new List<Task>();
                        Object lockObj = new Object();

                        var watch = System.Diagnostics.Stopwatch.StartNew();

                        //WriteLog("Service Thread delay : " + delayMillisecond.ToString());
                        for (int i = 0; i < listInList.Count; i++)
                        {
                            int lstInd = i; //index temp
                            tasks.Add(Task.Factory.StartNew(() =>
                            {
                                foreach (var item in listInList[lstInd])
                                {
                                    using (var dbTask = new CommunicateV1Entities())
                                    {
                                        usp_SendSMSShinee_Insert_Result transactionHeaderId = new usp_SendSMSShinee_Insert_Result();
                                        try
                                        {
                                            //Insert database SMS
                                            transactionHeaderId = dbTask.usp_SendSMSShinee_Insert(QueueSentId,
                                                                                                                                                1,
                                                                                                                                                3,
                                                                                                                                                item.SMSTypeID,
                                                                                                                                                item.PhoneNo,
                                                                                                                                                item.Message,
                                                                                                                                                3).FirstOrDefault();
                                        }
                                        catch (Exception ex)
                                        {
                                            WriteLog($"usp_SendSMSShinee_Insert error on QueueDetailId:{item.SMSQueueDetailId} |Error detail: { ex.Message}");
                                            continue;
                                        }

                                        if (transactionHeaderId.IsResult == false)
                                        {
                                            WriteLog($"usp_SendSMSShinee_Insert fail on QueueDetailId:{item.SMSQueueDetailId} |Result: { transactionHeaderId.Msg}");
                                        }
                                        else
                                        {
                                            string shineeRef = string.Empty;
                                            int statusCode = 15;
                                            string message = string.Empty;

                                            Task<Tuple<string, int, string>> shineeResult = CallShineeAPISendAsync(item.PhoneNo, item.Message, transactionHeaderId.Result);
                                            shineeResult.Wait();
                                            shineeRef = shineeResult.Result.Item1;
                                            statusCode = shineeResult.Result.Item2;
                                            message = shineeResult.Result.Item3;

                                            usp_TransactionHeaderInstanceShinee_Update_Result resultTransactionUpdate = new usp_TransactionHeaderInstanceShinee_Update_Result();
                                            try
                                            {
                                                lock (lockObj)
                                                {
                                                    WriteLog(message);
                                                }
                                                resultTransactionUpdate = dbTask.usp_TransactionHeaderInstanceShinee_Update(Convert.ToInt32(transactionHeaderId.Result),
                                                                                                                                                                                               shineeRef,//reference id
                                                                                                                                                                                               statusCode,//status code
                                                                                                                                                                                               1).FirstOrDefault();
                                            }
                                            catch (Exception ex)
                                            {
                                                WriteLog($"usp_TransactionHeaderInstanceShinee_Update error |QueueDetailId:{item.SMSQueueDetailId} |ReferenceId:{shineeRef} |Error detail: { ex.Message}");
                                                continue;
                                            }

                                            // if save is success
                                            if (resultTransactionUpdate.IsResult == false)
                                            {
                                                WriteLog($"Update TransactionHeader fail |QueueDetailId:{item.SMSQueueDetailId} |ReferenceId:{shineeRef} |Result: { resultTransactionUpdate.Msg}");
                                            }

                                        }
                                    }
                                }
                            }, token));

                            Task.Delay(delayMillisecond).Wait();
                        }

                        try
                        {
                            Task.WaitAll(tasks.ToArray());//รอทูกTaskทำงานให้เสร็จ
                            watch.Stop();
                            var elapsedMs = watch.ElapsedMilliseconds;
                            TimeSpan t = TimeSpan.FromMilliseconds(elapsedMs);
                            string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                                                                t.Hours,
                                                                                t.Minutes,
                                                                                t.Seconds,
                                                                                t.Milliseconds);
                            WriteLog($"All task are done>> Total time using: {answer}");
                        }
                        catch (AggregateException e)
                        {
                            foreach (var ie in e.InnerExceptions)
                            {
                                if (ie is OperationCanceledException)
                                {
                                    WriteLog("The word scrambling operation has been cancelled.", true);
                                    break;
                                }
                                else
                                {
                                    WriteLog(ie.GetType().Name + ": " + ie.Message, true);

                                }
                            }
                        }
                        finally
                        {
                            cts.Dispose();
                            IsDisposed = true;
                        }

                    }
                    else
                    {
                        WriteLog("No Data[Data.Count = 0]", true);
                    }
                }
                catch (Exception e)
                {
                    WriteLog("SendSMSList Error:  " + e.Message, true);
                }
            }
        }

        private static List<List<T>> Split<T>(List<T> source, int num)
        {
            return source.Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / num).Select(x => x.Select(v => v.Value).ToList()).ToList();
        }

        private void SendSMSListNormal()
        {
            using (var db = new CommunicateV1Entities())
            {
                try
                {
                    //GET DATA AND CREATE TEXT FILE
                    var AllData = db.usp_SMSQueueDetail_Select().ToList();

                    //Begin Testing***
                    //List<usp_SMSQueueDetail_Select_Result> AllData = new List<usp_SMSQueueDetail_Select_Result>();
                    //usp_SMSQueueDetail_Select_Result item1 = new usp_SMSQueueDetail_Select_Result() {
                    //    Message = "test",
                    //    PhoneNo = "0969426936",
                    //    ProviderId = 3,
                    //    SMSQueueDetailId = 001,
                    //    SMSQueueHeaderId = 101,
                    //    SMSQueueSentId = 111,
                    //    SMSTypeID = 1,
                    //    UpdatedDate = DateTime.Now
                    //};
                    //usp_SMSQueueDetail_Select_Result item2 = new usp_SMSQueueDetail_Select_Result()
                    //{
                    //    Message = "test2",
                    //    PhoneNo = "0969426936",
                    //    ProviderId = 3,
                    //    SMSQueueDetailId = 001,
                    //    SMSQueueHeaderId = 101,
                    //    SMSQueueSentId = 111,
                    //    SMSTypeID = 1,
                    //    UpdatedDate = DateTime.Now
                    //};
                    //AllData.Add(item1);
                    //AllData.Add(item2);

                    //End Testing***


                    //if set ProviderId is -1 then the program will be use "providerIdString" value from configuration file
                    var countDefault = AllData.Where(x => x.ProviderId == -1).Count();
                    List<usp_SMSQueueDetail_Select_Result> mktLst = new List<usp_SMSQueueDetail_Select_Result>();
                    List<usp_SMSQueueDetail_Select_Result> shineeLst = new List<usp_SMSQueueDetail_Select_Result>();
                    WriteLog($"Default sms get: {countDefault} messages", true);
                    if (countDefault > 0)
                    {
                        if (providerIdString == "2")
                        {
                            mktLst = AllData.Where(x => x.ProviderId == 2 || x.ProviderId == -1).ToList();
                            shineeLst = AllData.Where(x => x.ProviderId == 3).ToList();
                        }
                        else
                        {//providerIdString == "3" or other
                            mktLst = AllData.Where(x => x.ProviderId == 2).ToList();
                            shineeLst = AllData.Where(x => x.ProviderId == 3 || x.ProviderId == -1).ToList();
                        }
                    }
                    else
                    {
                        mktLst = AllData.Where(x => x.ProviderId == 2).ToList();
                        shineeLst = AllData.Where(x => x.ProviderId == 3).ToList();
                    }


                    //ClickNext process
                    if (mktLst.Count > 0)
                    {
                        WriteLog($"ClickNext sms get: {mktLst.Count} messages", true);
                        SmsClicknext.SendSMSClickNextList(mktLst);
                    }


                    //Shinee Process
                    int pCount = shineeLst.Count;
                    if (pCount > 0)
                    {
                        WriteLog($"Shinee sms get: {shineeLst.Count} messages", true);
                        WriteLog("Shinee sms start");

                        var QueueSentId = shineeLst[0].SMSQueueSentId;
                        //WriteLog("Data[Data.Count = "+ String.Format("{0:n0}", pCount)+ "]");
                        if (IsDisposed)
                        {
                            cts = new CancellationTokenSource();
                        }
                        IsDisposed = false;
                        CancellationToken token = cts.Token;
                        var tasks = new List<Task>();
                        Object lockObj = new Object();

                        //WriteLog("Service Thread delay : " + delayMillisecond.ToString());

                        //แบ่ง task
                        foreach (var item in shineeLst)
                        {
                            //int x = i; //add order number to temporary
                            tasks.Add(Task.Factory.StartNew(() =>
                            {
                                //var dbTask = new CommunicateV1Entities();
                                using (var dbTask = new CommunicateV1Entities())
                                {
                                    //Insert database SMS
                                    var transactionHeaderId = dbTask.usp_SendSMSShinee_Insert(QueueSentId,
                                                                                                                                        1,
                                                                                                                                        3,
                                                                                                                                        item.SMSTypeID,
                                                                                                                                        item.PhoneNo,
                                                                                                                                        item.Message,
                                                                                                                                        3).FirstOrDefault();

                                    if (transactionHeaderId.IsResult == false)
                                    {
                                        WriteLog($"usp_SendSMSShinee_Insert fail PhoneNo:{ item.PhoneNo} |Message: {  item.Message} |Result: { transactionHeaderId.Msg}");
                                    }
                                    else
                                    {
                                        Task<Tuple<string, int, string>> shineeResult = CallShineeAPISendAsync(item.PhoneNo, item.Message, transactionHeaderId.Result);
                                        //Task<Tuple<string, int>> shineeResult = CallShineeAPISendAsync(item.PhoneNo, item.Message,"101");
                                        shineeResult.Wait();
                                        //WriteLog($"Simulate send sms Ref:{ transactionHeaderId.Result} PhoneNo:{ item.PhoneNo} |Message: {  item.Message} |Result: success");
                                        var resultTransactionUpdate = dbTask.usp_TransactionHeaderInstanceShinee_Update(Convert.ToInt32(transactionHeaderId.Result),
                                                                                                                                                                                       shineeResult.Result.Item1,//reference id
                                                                                                                                                                                      shineeResult.Result.Item2,//status code
                                                                                                                                                                                      1).FirstOrDefault();
                                        // if save is success
                                        if (resultTransactionUpdate.IsResult == false)
                                        {
                                            WriteLog($"Update TransactionHeader fail PhoneNo:{ item.PhoneNo} |Message: {  item.Message} |Result: { transactionHeaderId.Msg}");
                                        }

                                    }
                                }



                            }, token));

                            Task.Delay(delayMillisecond).Wait();

                        }//foreach

                        try
                        {
                            Task.WaitAll(tasks.ToArray());//รอทูกTaskทำงานให้เสร็จ
                        }
                        catch (AggregateException e)
                        {
                            foreach (var ie in e.InnerExceptions)
                            {
                                if (ie is OperationCanceledException)
                                {
                                    WriteLog("The word scrambling operation has been cancelled.", true);
                                    break;
                                }
                                else
                                {
                                    WriteLog(ie.GetType().Name + ": " + ie.Message, true);

                                }
                            }
                        }
                        finally
                        {
                            cts.Dispose();
                            IsDisposed = true;
                        }

                    }
                    else
                    {
                        WriteLog("No Data[Data.Count = 0]", true);
                    }
                }
                catch (Exception e)
                {
                    WriteLog("SendSMSList Error:  " + e.Message, true);
                }
            }
        }

        private async Task<string> CallSSSAPISend(string smsId, string createById, string phoneNumber, string message)
        {
            Uri u = new Uri("http://uat.siamsmile.co.th:9215/api/sms/v2/SendSms");
            var payload = "{\"SMSTypeId\": " + smsId + ",\"Message\": \"" + message + "\",\"PhoneNo\": \"" + phoneNumber + "\",\"CreatedById\": " + createById + ",\"TimeoutMinute\": 0}";
            //var payload =$"{\"SMSTypeId\": 1,\"Message\": \"{csc}\",\"PhoneNo\": \"0969426936\",\"CreatedById\": 1,\"TimeoutMinute\": 0}";
            HttpContent c = new StringContent(payload, Encoding.UTF8, "application/json");
            //WriteLog(payload);
            var response = string.Empty;
            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm9qZWN0aWQiOjl9.XX7R_Ik8pydv2e04ZvrDew8tlszSDrjvTEYKdgO4t7A");
                HttpResponseMessage result = await client.PostAsync(u, c);

                if (result.IsSuccessStatusCode)
                {
                    response = result.StatusCode.ToString();
                }
            }
            return response;
        }

        private async Task<Tuple<string, int, string>> CallShineeAPISendAsync(string phoneNumber, string message, string transHeadId)
        {
            string refTransactionId = string.Empty;
            int statusCode = 15;
            string logString = string.Empty;
            if (message is null) message = "";

            try
            {
                //Generate Reference ID
                refTransactionId = FillId(transHeadId);

                //Generate XML string
                string xmlString = InitailSms(new List<string> { phoneNumber }, message, refTransactionId, SettingMessageTypeSMS, SettingPriorityOnline);

                //Get response******** Bring it back
                var result = await SendSMSToShineeProvider(xmlString);

                //Deserialize string of xml to object******** Bring it back
                var response_result = (Sms_Response)DeserializeXML(result, typeof(Sms_Response));

                //For Test only*****
                //Sms_Response response_result = new Sms_Response()
                //{
                //    Transid = "refTransactionId",
                //    Status = new Status() { Code = "0", Detail = "Test" }
                //};//For Test only *****


                if (response_result != null)
                {
                    if (response_result.Status.Code == "0") logString = $"Send  |ReferenceId: {  refTransactionId} |PhoneNo:{ phoneNumber} |Result: {  response_result.Status.Detail}";
                    else logString = $"Send |ReferenceId: {  refTransactionId} |PhoneNo:{ phoneNumber} |Result: Code={response_result.Status.Code} |Detail={response_result.Status.Detail}";
                }
                else
                {
                    logString = $"Send |ReferenceId: {  refTransactionId} |PhoneNo:{ phoneNumber} |Result: cannot read xml from Shinee ";
                    //Return result
                    return Tuple.Create(refTransactionId, statusCode, logString);
                }

                //Mapping status code ****Bring back
                int shineeStatusResult = Convert.ToInt32(response_result.Status.Code);
                statusCode = MappingStatusResponse(shineeStatusResult);

                //Return result
                return Tuple.Create(refTransactionId, statusCode, logString);
            }
            catch (Exception ex)
            {
                logString = $"CallShineeAPISend process fail |ReferenceId: {  refTransactionId} |PhoneNo:{ phoneNumber} |Exception: {ex.Message} ";
                statusCode = 15;
                return Tuple.Create(refTransactionId, statusCode, logString);
            }
        }

        private int MappingStatusResponse(int shineeCode)
        {
            switch (shineeCode)
            {
                case 0://Success
                    return 1;//ส่งข้อมูลกลับมาสมบูรณ์ (OK)
                case 1://รูปแบบ XML ไม่ถูกต้อง
                    return 2;//ป้อนพารามิเตรอ์มาไม่ครบ
                case 3://Username หรือ Password ไม่ถูกต้อง
                    return 6;//ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง
                case 4://Sender name ไม่ถูกต้อง
                    return 7;//ไม่พบชื่อผู้ส่ง
                case 5://รูปแบบของหมายเลขโทรศัพท์ไม่ถูกต้อง
                    return 3;//ส่งออกไม่ได้ ไม่พบเบอร์โทรศัพท์
                case 6://รูปแบบของข้อความไม่ถูกต้อง
                    return 14;//ข้อความไม่ถูกต้อง เนื่องจากมีอักขระพิเศษ
                case 7://วันที่ตั้งเวลาไม่ถูกต้อง
                    return 12;//รูปแบบวันที่ไม่ถูกต้อง
                case 8://ใช้ Quota ในการส่งเกิน
                    return 8;//ส่งออกไม่ได้ จำนวนเครดิตของท่านไม่พอ
                default://other
                    return 15; //รอข้อมูล
            }
        }

        private string FillId(string smsId)
        {
            //length of id must be between 14-50
            string genIdString = "";
            if (smsId.ToString().Length < 14)
            {
                int Lengthleft = 14 - smsId.ToString().Length;
                //fill id to length 14
                genIdString = smsId.PadLeft(smsId.Length + Lengthleft, '0');
            }

            //length of id's do not over 50 (Shinee gateway limited)
            //Do something

            return genIdString;

        }

        private string InitailSms(List<string> phoneNumber, string message, string refId, string messageTypes = "", string prioSms = "")
        {
            string username = SettingUsername;
            string password = SettingPassword;
            string senderName = SettingSenderName;
            string operatorString = SettingOperationAll;//6 is unknow
            string smsType = messageTypes != "" ? messageTypes : SettingMessageTypeURL;//"sms","url";
            string messageLanguage = SettingLanguageThaiI;//th, en
            string prioritySms = prioSms != "" ? prioSms : SettingPriorityOnline;//otp,normal,online



            //account tag
            Account account = new Account();
            account.Username = username;
            account.Password = password;

            //source tag
            Source source = new Source();
            source.Sender = senderName;

            //destination tag
            Destination destination = new Destination();
            List<Msisdn> msisdn = new List<Msisdn>();
            //smsisdn attribute
            foreach (var item in phoneNumber)
            {
                Msisdn smsisdn = new Msisdn();
                smsisdn.Operator = operatorString;
                smsisdn.Text = item.Trim(MyChar);
                msisdn.Add(smsisdn);
            }
            destination.Msisdn = msisdn;

            //content tag
            Content content = new Content();
            content.Type = smsType;
            content.Language = messageLanguage;
            content.subject = SMS_AISDTAC_Encoding(SettingSubjectMessage);
            content.Message = SMS_AISDTAC_Encoding(message.Trim(MyChar));

            //sms tag
            var smssending = new Sms_Request();
            smssending.Transid = refId.Trim(MyChar);
            smssending.Account = account;
            smssending.Source = source;
            smssending.Destination = destination;
            smssending.Content = content;
            smssending.priority = prioritySms;

            //Convert to xml string
            string xmlString = Serialize(smssending).Trim(MyChar);
            return xmlString;
        }

        public static async void WriteLog(string message, bool IsErrorLog = false)
        {
            if (!IsDebugMode && !IsErrorLog)
            {
                return;
            }

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path)) //Create path if doesn't exist
            {
                Directory.CreateDirectory(path);
            }
            string currentFilePath = Path.Combine(path, $"{DateTime.Now.ToString("yyyy-MM-dd")} ServiceLog.log");

            try
            {
                UnicodeEncoding ue = new UnicodeEncoding();
                string messagestr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message;
                char[] charsToAdd = ue.GetChars(ue.GetBytes(messagestr));
                FileStream objFilestream = new FileStream(currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using (StreamWriter writer = new StreamWriter((Stream)objFilestream))
                {
                    await writer.WriteLineAsync(charsToAdd, 0, charsToAdd.Length);
                }

                //FileStream objFilestream = new FileStream(currentFilePath, FileMode.Append, FileAccess.Write);
                //StreamWriter objStreamWriter = new StreamWriter((Stream)objFilestream);
                //objStreamWriter.WriteLine(messagestr);
                //objStreamWriter.Close();
                //objFilestream.Close();
            }
            catch (Exception ex)
            {
                //
            }

        }

        private async Task addMessage(string message)
        {
            await Task.Run(() => {
                // Do lot of work here
                lstMessage.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message);
            });

        }

        private async Task writeLogList()
        {
            if (lstMessage.Count == 0) return;
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path)) //Create path if doesn't exist
            {
                Directory.CreateDirectory(path);
            }
            string currentFilePath = Path.Combine(path, $"{DateTime.Now.ToString("yyyy-MM-dd")} ServiceLog.log");

            try
            {
                UnicodeEncoding ue = new UnicodeEncoding();
                FileStream objFilestream = new FileStream(currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using (StreamWriter writer = new StreamWriter((Stream)objFilestream))
                {
                    foreach (var item in lstMessage)
                    {
                        char[] charsToAdd = ue.GetChars(ue.GetBytes(item));
                        await writer.WriteLineAsync(charsToAdd, 0, charsToAdd.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                //
            }
            finally
            {
                lstMessage.Clear();
            }

        }


        private string SMS_AISDTAC_Encoding(string text)
        {
            var builder = new StringBuilder(text.Length * 5);
            foreach (char chr in text)
            {

                builder.AppendFormat("{0:X4}", (ushort)chr);
            }
            return builder.ToString();
        }

        private string Serialize(dynamic details)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlWriterSettings xmlWriterSettings = new System.Xml.XmlWriterSettings()
                {
                    // If set to true XmlWriter would close MemoryStream automatically and using would then do double dispose
                    // Code analysis does not understand that. That's why there is a suppress message.
                    CloseOutput = false,
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false,
                    NewLineHandling = NewLineHandling.None,
                    Indent = false

                };
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);
                using (System.Xml.XmlWriter xw = System.Xml.XmlWriter.Create(ms, xmlWriterSettings))
                {
                    Type unknown = details.GetType();//Get type of dynamic object
                    XmlSerializer s = new XmlSerializer(unknown);
                    s.Serialize(xw, details, ns);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }

        }

        private async Task<string> SendSMSToShineeProvider(string requestXml, int mode = 0)
        {
            string broadcasturl = string.Empty;
            switch (mode)
            {
                case 1:
                    broadcasturl = "http://backoffice.shinee.com/bulk_sms_shinee/bulk_api/DeliveryReport.php";//mode request result
                    break;
                default://0
                    broadcasturl = ConfigurationManager.AppSettings["ShineeEndpoint"].ToString(); //mode send sms
                    break;
            }
            //send to Shinee API

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(broadcasturl);
            byte[] bytes;
            bytes = System.Text.Encoding.ASCII.GetBytes(requestXml);
            request.KeepAlive = true;
            request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.ConnectionLimit = 24;
            request.ContentType = "text/xml; encoding='utf-8'";
            request.ContentLength = bytes.Length;
            request.Method = "POST";
            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }
            HttpWebResponse response;
            response = (HttpWebResponse)await request.GetResponseAsync();

            using (Stream responseStream = response.GetResponseStream())
            {
                string responseStr = new StreamReader(responseStream).ReadToEnd();
                //Deserialize string of xml to object
                return responseStr;
            }



        }

        private object DeserializeXML(string xmlString, Type objResult)// dynamic objResult)
        {
            if (xmlString == "") return null;
            //Type unknown = objResult.GetType();//Get type of dynamic object
            XmlSerializer serializer = new XmlSerializer(objResult);
            using (TextReader reader = new StringReader(xmlString))
            {
                dynamic result = serializer.Deserialize(reader);
                return result;
            }
        }
        #endregion  
    }

    #region Provider Class

    public class InstanceData
    {
        public string TransactionCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public string ResponceStatusCode { get; set; }
        public string ResponseTransactionID { get; set; }
    }

    [XmlRoot(ElementName = "sms-dr")]
    public class SMS_RequestResult_drchk
    {
        [XmlElement(ElementName = "transid")]
        public string TransId { get; set; }
        [XmlElement(ElementName = "sender")]
        public string Sender { get; set; }
        [XmlElement(ElementName = "msisdn")]
        public string Phone { get; set; }
        [XmlElement(ElementName = "priority")]
        public string Priority { get; set; }
    }


    [XmlRoot(ElementName = "sms-dr")]
    public class SMS_ResponseResult_V2
    {
        [XmlElement(ElementName = "transid")]
        public string Transid { get; set; }

        [XmlElement(ElementName = "sender")]
        public string Sender { get; set; }

        [XmlElement(ElementName = "msisdn")]
        public string msisdn { get; set; }

        [XmlElement(ElementName = "status")]
        public string status { get; set; }

        [XmlElement(ElementName = "detail")]
        public string detail { get; set; }


    }

    [XmlRoot(ElementName = "account")]
    public class Account
    {
        [XmlElement(ElementName = "username")]
        public string Username { get; set; }

        [XmlElement(ElementName = "password")]
        public string Password { get; set; }
    }

    [XmlRoot(ElementName = "source")]
    public class Source
    {
        [XmlElement(ElementName = "sender")]
        public string Sender { get; set; }
    }

    [XmlRoot(ElementName = "msisdn")]
    public class Msisdn
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "destination")]
    public class Destination
    {
        [XmlElement(ElementName = "msisdn")]
        public List<Msisdn> Msisdn { get; set; }
    }

    [XmlRoot(ElementName = "content")]
    public class Content
    {
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "language")]
        public string Language { get; set; }

        [XmlElement(ElementName = "subject")]
        public string subject { get; set; }

        [XmlElement(ElementName = "message")]
        public string Message { get; set; }
    }

    [XmlRoot(ElementName = "sms")]
    public class Sms_Request
    {
        [XmlElement(ElementName = "transid")]
        public string Transid { get; set; }

        [XmlElement(ElementName = "account")]
        public Account Account { get; set; }

        [XmlElement(ElementName = "source")]
        public Source Source { get; set; }

        [XmlElement(ElementName = "destination")]
        public Destination Destination { get; set; }

        [XmlElement(ElementName = "content")]
        public Content Content { get; set; }

        [XmlElement(ElementName = "schedule")]
        public string schedule { get; set; }

        [XmlElement(ElementName = "priority")]
        public string priority { get; set; }

        [XmlElement(ElementName = "timeout")]
        public string timeout { get; set; }
    }

    [XmlRoot(ElementName = "status")]
    public class Status
    {
        [XmlElement(ElementName = "code")]
        public string Code { get; set; }

        [XmlElement(ElementName = "detail")]
        public string Detail { get; set; }
    }

    [XmlRoot(ElementName = "sms")]
    public class Sms_Response
    {
        [XmlElement(ElementName = "transid")]
        public string Transid { get; set; }

        [XmlElement(ElementName = "status")]
        public Status Status { get; set; }
    }

    [XmlRoot(ElementName = "sms-dr")]
    public class sms_dr
    {
        public string username { get; set; }
        public string password { get; set; }
        public string source { get; set; }
        public string transid { get; set; }
        public string sender { get; set; }
        public string msisdn { get; set; }
        public string sendtime { get; set; } = null;
        public string status { get; set; }
        public string detail { get; set; }
    }
    #endregion
}
