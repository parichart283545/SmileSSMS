using System;
using System.Collections.Generic;
using System.Linq;
using SmileSSMSSendList.SMSService;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace SmileSSMSSendList
{
    public static class SmsClicknextV2
    {
        #region Decraration
        private static string endPoint = ConfigurationManager.AppSettings["ClicknextEndpoint"];
        private static string apiKey = ConfigurationManager.AppSettings["ClicknextApiKey"];
        private static string secretKey = ConfigurationManager.AppSettings["ClicknextSecretKey"];
        #endregion
        #region Method
        public static void SendSMSClickNextList(List<usp_SMSQueueDetail_Select_Result> lstQueueDetailData)
        {
            using (var db = new CommunicateV1Entities())
            {
                try
                {
                    SMSWinService.WriteLog("ClickNext sms start");
                    //GET DATA AND CREATE TEXT FILE
                    var lstData = lstQueueDetailData;
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
                            SMSWinService.WriteLog("Insert Success[usp_SendSMS_Insert] ");
                            //Send SMS
                            var PathFile = filepath;
                            var sr = new StreamReader(PathFile, System.Text.Encoding.UTF8);
                            var StrData = sr.ReadToEnd();
                            sr.Close();

                            var ByteData = System.Text.Encoding.UTF8.GetBytes(StrData);
                            StrData = Convert.ToBase64String(ByteData);

                            //CALL SEND FILE MESSAGE API
                            var client = new RestClient(endPoint);
                            var request = new RestRequest("sms-file", Method.POST);

                            //Header
                            request.AddHeader("api_key", apiKey);
                            request.AddHeader("secret_key", secretKey);


                            //Body payload
                            var data = new SendSMSText_Request();
                            data.file_data = StrData;
                            data.file_name = PathFile;
                            data.sender_name = "SiamSmile";
                            data.schedule = "";


                            var jsonSender = JsonConvert.SerializeObject(data);
                            request.AddJsonBody(jsonSender);

                            //Post API
                            var response = client.Post(request);
                            var result = JsonConvert.DeserializeObject<SendSMSText_Respone_Detail>(response.Content);

                            var resultTranHeaderUpdate = db.usp_TransactionHeader_UpdateV2(Convert.ToInt32(sendResult.Result), response.Content).FirstOrDefault();
                            if (resultTranHeaderUpdate.IsResult == true)
                            {
                                SMSWinService.WriteLog("Service Sent SMS Success[ReferenceID=" + resultTranHeaderUpdate.Result + "] ");
                                SMSWinService.WriteLog("Update TransactionHeader_Update Success ");
                            }
                            else
                            {
                                SMSWinService.WriteLog("Update TransactionHeader_Update Fail | Message: " + resultTranHeaderUpdate.Msg, true);
                            }
                        }
                        else
                        {
                            SMSWinService.WriteLog("Insert usp_SendSMS_Insert Fail | Message: " + sendResult.Msg, true);
                        }
                    }
                    else
                    {
                        //SMSWinService.WriteLog("No Data[Data.Count = 0] ");
                    }
                }
                catch (Exception e)
                {
                    SMSWinService.WriteLog("SendSMSList Error " + e.Message, true);
                }
            }
        }

        private static void WriteToFile2(string text)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = Path.Combine(path, $"{DateTime.Now.ToString("yyyy-MM-dd")} ServiceLog.log");

            if (!File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt") + " " + text);
                    // sw.WriteLine(string.Format(text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt")));
                    sw.Close();
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(string.Format(text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt")));
                    sw.Close();
                }
            }
        }

        #endregion


        //SMS Text Request obj
        public class SendSMSText_Request
        {
            public string file_data { get; set; }
            public string file_name { get; set; }
            public string sender_name { get; set; }
            public string schedule { get; set; }
        }

        //SMS Text Response obj
        public class SendSMSText_Respone_Detail
        {
            public string code { get; set; }
            public string detail { get; set; }

            public SendSMSText_Respone_Result result;
            public string transaction_id { get; set; }
        }
        //SMS Text Response obj
        public class SendSMSText_Respone_Result
        {
            public string credits { get; set; }
            public string numbers { get; set; }
            public string total_among { get; set; }
        }
    }
}
