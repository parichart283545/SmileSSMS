using SmileSCommunicateRESTfulService.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using static SmileSCommunicateRESTfulService.Models.Model;
using RestSharp;
using System.Text.RegularExpressions;

namespace SmileSCommunicateRESTfulService.Class_Lib
{
    public class SmsShineeClass
    {
        #region Fields
        //
        // Flag: Has Dispose already been called?
        private bool disposed = false;
        //Priority
        private const string SettingPriorityOnline = "online";
        private const string SettingPriorityNormal = "normal";
        private const string SettingPriorityOtp = "otp";
        //Configuration SMS
        private static string SettingUsername = Properties.Settings.Default.Shinee_SmsUsername;
        private static string SettingPassword = Properties.Settings.Default.Shinee_SmsPassword;
        private const string SettingSenderName = "Siamsmile";
        //Configuration OTP
        private static string SettingOTPUsername = Properties.Settings.Default.Shinee_OtpUsername;
        private static string SettingOTPPassword = Properties.Settings.Default.Shinee_OtpPassword;
        private static string SettingKeyAuthen = Properties.Settings.Default.Shinee_KeyAuth;

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

        //Uri API
        private const string uriDeliveryReport = "http://backoffice.shinee.com/bulk_sms_shinee/bulk_api/DeliveryReport.php";
        private static string uriSendSms = Properties.Settings.Default.Shinee_SmsEndPoint;
        private static string uriRequestOTP = Properties.Settings.Default.Shinee_OtpEndPoint;
        private static string uriVerifyOTP = Properties.Settings.Default.Shinee_VerifyEndPoint;
        #endregion
        #region Constructors
        //
        //public SmsShineeClass()
        //{
        //    //initailize
        //    //_smsFunc = new SmsCentreFunction();
        //}

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
            }

            disposed = true;
        }

        #endregion
        #region Properties
        //
        #endregion
        #region Private static methods
        //
        private static  SMS_DetailResult DoSendSMS(SMSSingle_DetailRequest sMSSingle_DetailRequest, string messageType , string priorityString, DateTime schedule = default(DateTime),int SmstimeOut = 0)
        {
            //Declaration parameter
            SMS_DetailResult result = new SMS_DetailResult() // define default of result
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = SmsCentreFunction.statusDetail_waiting,
                Language = "th",
                SumPhone = "1",
                ReferenceId = null
            };
            string RefTransactionId = string.Empty;
            string phoneNumber = sMSSingle_DetailRequest.PhoneNo;
            string message = sMSSingle_DetailRequest.Message.Trim(SmsCentreFunction.MyChar);

            try
            {
                //validate parameter special for shinee
                Tuple<bool, string> validResult = ValidationParameter(message, schedule, SmstimeOut);
                if (!validResult.Item1)
                {
                    result.Detail = validResult.Item2;
                    return result;
                }

                //Insert database SMS
                var result_SMSInstance2 = SmsCentreFunction.InstanceInsert(sMSSingle_DetailRequest.SMSTypeId,
                                                                                                           phoneNumber,
                                                                                                           message,
                                                                                                           sMSSingle_DetailRequest.CreatedById,
                                                                                                           sMSSingle_DetailRequest.SenderId,
                                                                                                           sMSSingle_DetailRequest.ProviderId);

                if (result_SMSInstance2.IsResult == false)
                {
                    result.Detail = result_SMSInstance2.Msg;
                    return result;
                }

                //Generate Reference ID
                RefTransactionId = FillId(result_SMSInstance2.Result);
                
                //Generate XML string
                string xmlString = InitailSms(new List<string> { phoneNumber }, message, RefTransactionId, messageType, priorityString, schedule, SmstimeOut);

                //Get response
                var response = SendSMSToShineeProvider(xmlString);

                //Deserialize string of xml to object
                var response_result = (Sms_Response)DeserializeXML(response, typeof(Sms_Response));
                //string responseStatusCode = "1"; //set defalt 1 = รูปแบบ XML ไม่ถูกต้อง
                TransactionStatu transactionStatu = null;
                if (response_result == null)
                {
                    transactionStatu = SmsCentreFunction.MatchStatusCodeInDB("1");
                }
                else //deserailize response class success
                {
                    int shineeResponseCode = Convert.ToInt32(response_result.Status.Code);
                    transactionStatu = SmsCentreFunction.MatchStatusCodeInDB(MappingStatusResponse(shineeResponseCode).ToString());
                }
                
                //save result
                var result_TransactionHeader = SmsCentreFunction.TransactionHeaderInstanceUpdate(Convert.ToInt32(result_SMSInstance2.Result),
                                                                                                                                                                      response_result.Transid,
                                                                                                                                                                      transactionStatu.TransactionStatusID,
                                                                                                                                                                      1);
                // if save is success
                if (result_TransactionHeader.IsResult == true)
                {
                    result.Status = transactionStatu.TransactionStatusCode.ToString();
                    result.Detail = transactionStatu.TransactionStatusDetail;
                    result.ReferenceId = response_result.Transid;
                    return result;
                }
                else //save fail
                {
                    result.Status = transactionStatu.TransactionStatusCode.ToString();
                    result.Detail = transactionStatu.TransactionStatusDetail + " | การบันทึกลงฐานข้อมูลมีปัญหา : " + result_TransactionHeader.Msg;
                    result.ReferenceId = response_result.Transid;
                    return result;
                }

            }
            catch (Exception ex)
            {
                result.Detail = "รอข้อมูล";// ex.Message;
                return result;
            }
        }

        private static OTP_DetailResult DoSendRequestOTP(OTP_DetailRequest sMSSingle_DetailRequest)
        {
            //Declaration parameter
            OTP_DetailResult result = new OTP_DetailResult() // define default of result
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = SmsCentreFunction.statusDetail_waiting,
                RefCode = string.Empty,
                ReferenceId = string.Empty
            };

            string RefTransactionId = string.Empty;
            string phoneNumber = sMSSingle_DetailRequest.PhoneNo;
            string refCode = string.Empty;
            int transactionHeaderId = -1;

            try
            {
                //Insert database SMS
                var result_SMSInstance2 = SmsCentreFunction.InstanceInsert(sMSSingle_DetailRequest.SMSTypeId, //28 OTP SmsTypeid
                                                                                                           phoneNumber,
                                                                                                           string.Empty,
                                                                                                           sMSSingle_DetailRequest.CreatedById,
                                                                                                           sMSSingle_DetailRequest.SenderId,
                                                                                                           sMSSingle_DetailRequest.ProviderId);

                if (result_SMSInstance2.IsResult == false)
                {
                    result.Detail = result_SMSInstance2.Msg;
                    return result;
                }
                transactionHeaderId = Convert.ToInt32(result_SMSInstance2.Result);
                //Generate Reference ID
                RefTransactionId = FillId(result_SMSInstance2.Result);
                //Generate RefCode
                refCode = SmsCentreFunction.GetRefCodeOTP();

                //Create object
                REQ_DATA request = new REQ_DATA
                {
                    TRANSID = RefTransactionId,
                    KEYAUTHEN = SettingKeyAuthen,
                    RefText = refCode,
                    Sender = SettingSenderName,
                    Recipient = ConvertPhoneNoLength(phoneNumber)
                };

                //Get response
                var response = RequestApiProvider(request, uriRequestOTP);

                //Deserialize string of xml to object
                var response_result = (SmsOTPByADV_Response)DeserializeXML(response, typeof(SmsOTPByADV_Response));

                ////*****TEST
                //SmsOTPByADV_Response response_result = new SmsOTPByADV_Response
                //{
                //    Transid = RefTransactionId,
                //    SessionId = "1111111111",
                //    RefText = "AAAAA",
                //    Status = "005",
                //    Detail = "Invalid RefCode"
                //};
                ////*****TEST

                //string responseStatusCode = "1"; //set defalt 1 = รูปแบบ XML ไม่ถูกต้อง
                TransactionStatu transactionStatu = new TransactionStatu();
                if (response_result == null)
                {
                    transactionStatu = SmsCentreFunction.MatchStatusCodeInDB("1");
                }
                else //deserailize response class success
                {
                    transactionStatu = SmsCentreFunction.MatchStatusCodeInDB(GetStatusIdByCode(response_result.Status));
                }

                //1
                //save result
                var result_TransactionHeader = SmsCentreFunction.TransactionHeaderOTPUpdate(transactionHeaderId,
                                                                                                                                                                      response_result.Transid,
                                                                                                                                                                      transactionStatu.TransactionStatusID,
                                                                                                                                                                      1,
                                                                                                                                                                      response_result.SessionId,
                                                                                                                                                                      response_result.RefText);
                //result_TransactionHeader.
                // if save is success
                if (result_TransactionHeader.IsResult == true)
                {
                    result.Status = transactionStatu.TransactionStatusCode.ToString().Trim();
                    result.Detail = transactionStatu.TransactionStatusDetail;
                    result.RefCode = response_result.RefText;
                    result.ReferenceId = transactionHeaderId.ToString();
                    return result;
                }
                else //save fail
                {
                    result.Status = transactionStatu.TransactionStatusCode.ToString().Trim();
                    result.Detail = transactionStatu.TransactionStatusDetail + " | การบันทึกลงฐานข้อมูลมีปัญหา : " + result_TransactionHeader.Msg;
                    result.RefCode = response_result.RefText;
                    result.ReferenceId = transactionHeaderId.ToString();
                    return result;
                }

            }
            catch (Exception ex)
            {
                result.Detail = "รอข้อมูล";// ex.Message;
                return result;
            }
        }

        private static Verify_DetailResult DoVerifyOTP(VerifyOTP_DetailRequest verifyDetailRequest)
        {
            //Declaration parameter
            Verify_DetailResult result = new Verify_DetailResult() // define default of result
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = SmsCentreFunction.statusDetail_waiting,
                Result = false,
                //Remark = ""
            };

            string RefTransactionId = verifyDetailRequest.ReferenceId; //find PhoneNo, TokenID
            string refCode = verifyDetailRequest.RefCode; //
            string token = "";
            string otpCode = verifyDetailRequest.OTPCode; //
            string phoneNo = ""; //ต้องทำเป็น 66XXXXXXXXX ก่อน
            int verifyOtpId = -1;

            try
            {
                //2
                //Find phone number and token
                var findResult = SmsCentreFunction.FindPhoneAndToken(RefTransactionId);
                if (findResult.Item1 == "" ||
                    findResult.Item2 == "")
                {
                    //result.Remark = "ไม่พบรายการ";
                    return result;
                }
                phoneNo = findResult.Item1;
                token = findResult.Item2;

                //3
                //Insert database VerifyOTP
                var resultInsert = SmsCentreFunction.InsertVerify(token,
                                                                                                      refCode,
                                                                                                      otpCode,
                                                                                                      phoneNo);
                if (resultInsert.IsResult == false || resultInsert.Result == "-1")
                {
                    result.Detail = resultInsert.Msg;
                    return result;
                }

                //Get verifyOTPId
                verifyOtpId = Convert.ToInt32(resultInsert.Result);
                //Create object
                string md5RefCode = "";
                using (MD5 mD5 = MD5.Create())
                {
                    md5RefCode = Encoding_MD5(mD5, refCode + ConvertPhoneNoLength(findResult.Item1) + otpCode);
                }
                SmsOTPVerifyByADV_Request request = new SmsOTPVerifyByADV_Request
                {
                    KeyAuth = SettingKeyAuthen,
                    RefCode = md5RefCode,
                    SessionId = token
                };

                //Get response
                var response = RequestApiProvider(request, uriVerifyOTP);

                //Deserialize string of xml to object
                var response_result = (SmsOTPVerifyByADV_Response)DeserializeXML(response, typeof(SmsOTPVerifyByADV_Response));
                //string responseStatusCode = "1"; //set defalt 1 = รูปแบบ XML ไม่ถูกต้อง
                TransactionStatu transactionStatu = new TransactionStatu();
                if (response_result == null)
                {
                    //transactionStatu = SmsCentreFunction.MatchStatusCodeInDB("1");
                    result.Result = false;
                    //result.Detail = transactionStatu.TransactionStatusDetail;
                    //result.Remark = "ไม่สามารถยืนยันตัวตนได้";
                }
                else //deserailize response class success
                {
                    transactionStatu = SmsCentreFunction.MatchStatusCodeInDB(GetStatusIdByCode(response_result.Status));
                    if (transactionStatu.TransactionStatusID == 1)//ยืนยันตัวตนสำเร็จ
                    {
                        result.Result = true;
                        //result.Remark = "";
                    }
                    else
                    {
                        result.Detail = transactionStatu.TransactionStatusDetail;
                        //result.Remark = transactionStatu.TransactionStatusDetail;
                    }
                }


                //4
                //Update database VerifyOTP
                var resultUpdate = SmsCentreFunction.UpdateVerify(verifyOtpId,
                                                                                                                                 transactionStatu.TransactionStatusID.ToString(),
                                                                                                                                 transactionStatu.TransactionStatusDetail);
                // if save is success
                if (resultUpdate.IsResult == true)
                {
                    result.Status = transactionStatu.TransactionStatusCode.ToString().Trim();
                    result.Detail = transactionStatu.TransactionStatusDetail;
                    return result;
                }
                else //save fail
                {
                    result.Status = transactionStatu.TransactionStatusCode.ToString().Trim();
                    result.Detail = transactionStatu.TransactionStatusDetail + " | การบันทึกลงฐานข้อมูลมีปัญหา : " + resultUpdate.Msg;
                    
                    return result;
                }

            }
            catch (Exception ex)
            {
                result.Detail = "รอข้อมูล";// ex.Message;
                return result;
            }
        }

        private static string ConvertPhoneNoLength(string phoneNo)
        {
            string newPhone = "";
            int stringLenght = phoneNo.Length;
            if (stringLenght == 10)
            {
                var regex = @"^0\d{9}$";
                var match = Regex.Match(phoneNo, regex, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    //เปลี่ยนเบอร์เป็นรูปแบบ 66XXXXXXXXX)
                    string first_xter = phoneNo.Substring(0, 1);
                    newPhone = "66" + phoneNo.Remove(0, 1);
                    return newPhone;
                }
                else
                {
                    return newPhone;//เบอร์ไม่ถูกต้อง
                }

                    
            }
            else if (stringLenght == 11)
            {
                var regex = @"^66\d{9}$";
                var match = Regex.Match(phoneNo, regex, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    newPhone= phoneNo;
                    return newPhone;
                }
                else return newPhone;//เบอร์ไม่ถูกต้อง
            }
            else return newPhone;//เบอร์ไม่ถูกต้อง

        }


        private static int MappingStatusResponse(int shineeCode)
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

        private static string GetStatusIdByCode(string shineeStatudCode)
        {
            switch (shineeStatudCode)
            {
                case "000"://Success
                    return "1";//ส่งข้อมูลกลับมาสมบูรณ์ (OK)
                case "100"://Verify Success
                    return "1";//ส่งข้อมูลกลับมาสมบูรณ์ (OK)
                case "001"://Missing TRANSID
                    return "11";//ไม่พบหมายเลข Transaction การส่งในครั้งนี้
                case "002"://Invalid KeyAuthen
                    return "28";//KeyAuthen ไม่ถูกต้อง
                case "003"://Invalid Sender
                    return "7";//ไม่พบชื่อผู้ส่ง
                case "004"://Mobileno is wrong format
                    return "29";//รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง
                case "005"://Invalid RefCode
                    return "30";//ไม่พบ Token
                case "300"://OTP TIMEOUT
                    return "24";//ตรวจสอบ Token ไม่ได้ Token ของคุณหมดอายุ
                default://other
                    return "15"; //รอข้อมูล
            }
        }

        private static string FillId(string smsId)
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

        private Tuple<int, int> FindTaskCount(int val)
        {
            int number = val;
            int getTaskCount = 1;

            if (number % 2 == 0) getTaskCount = 2;
            if (number % 3 == 0) getTaskCount = 3;
            if (number % 4 == 0) getTaskCount = 4;
            if (number % 5 == 0) getTaskCount = 5;

            return Tuple.Create<int, int>(getTaskCount, (val / getTaskCount));
        }

        private static Tuple<bool, string> ValidationParameter(string message, DateTime schedule = default(DateTime), int SmstimeOut = 0)
        {
            try
            {
                //lenght of message
                //-ข้อความภาษาไทย ก่อน encode ความยาวต้องไม่เกิน 70 ตัวอักษร
                //-ข้อความภาษาอังกฤษ ก่อน encode ความยาวต้องไม่เกิน 160 ตัวอักษร
                ////Maximum 536
                //if (message.Length > 536)
                //{
                //    return Tuple.Create<bool, string>(false, ErrorMessageLenghtOver);
                //}
                
                //schedule is present
                if (schedule != default(DateTime))
                { if (schedule < DateTime.Now) return Tuple.Create<bool, string>(false, ErrorScheduleNotUpToDate); }

                if (SmstimeOut < 0) return Tuple.Create<bool, string>(false, ErrorTimeOutWrong);

                return Tuple.Create<bool, string>(true, "");
            }
            catch (Exception ex)
            {
                return Tuple.Create<bool, string>(false, "รอข้อมูล"/* $"Error validation : {ex.Message}"*/);
            }

        }
        
        private static object DeserializeXML(string xmlString, Type objResult)// dynamic objResult)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
           
        }
        
        private static string RequestApiProvider(dynamic objXml, string uri )
        {
            //send to Shinee API
            var client = new RestClient(uri);
            var request = new RestRequest();

            request.Method = Method.POST;
            request.RequestFormat = DataFormat.Xml;
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.AddXmlBody(objXml);
            
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Content;
            }


            return "";
        }
        

        private static string SendSMSToShineeProvider(string requestXml, int mode = 0)
        {
            string broadcasturl = string.Empty;
            switch (mode)
            {
                case 1:
                    broadcasturl = uriDeliveryReport;//mode request result
                    break;
                default://0
                    broadcasturl = uriSendSms; //mode send sms
                    break;
            }
            //send to Shinee API

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(broadcasturl);
            byte[] bytes;
            bytes = System.Text.Encoding.ASCII.GetBytes(requestXml);
            request.ContentType = "text/xml; encoding='utf-8'";
            request.ContentLength = bytes.Length;
            request.Method = "POST";
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse response;
            response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream responseStream = response.GetResponseStream();
                string responseStr = new StreamReader(responseStream).ReadToEnd();
                return responseStr;
            }
            return "";
        }

        private static string InitailSms(List<string> phoneNumber, string message, string refId, string messageTypes = "", string prioSms = "", DateTime schedule = default(DateTime), int SmstimeOut = 0)
        {
            //ตัวอย่าง ของ Mode schedule 
            //<content>
            //<type> sms </type >
            //<language> th </language >
            //<message>
            //</content>
            //<schedule> 2009-12-31 11:20:00</ schedule >


            //การใช้ Onetime & Limit Period Message กำหนด 2 ตัว priority=otp, timeout
            //ตัวอย่าง การใช้ message OPT
            //< content >
            //<type> sms </type>
            //<language> th </language>
            //<message> </ message >
            //</content >
            //<priority> otp </priority>
            //<timeout> 5 </timeout >

            //การส่ง message แบบทันที โดยระบุ Timeout เมื่อปลายทางไม่ได้รับข้อความตามเวลาที่กำหนด


            string username = SettingUsername;
            string password = SettingPassword;
            string senderName = SettingSenderName;
            string operatorString = SettingOperationAll;//6 is unknow
            string smsType = messageTypes != "" ? messageTypes : SettingMessageTypeURL;//"sms","url";
            string messageLanguage = SettingLanguageThaiI;//th, en
            string prioritySms = prioSms != "" ? prioSms : SettingPriorityOnline;//otp,normal,online
            DateTime scheduleSms = schedule != default(DateTime) ? schedule : default(DateTime); //YYYY-MM-DD hh:mm:ss
            //การ set timeout เป็นการระบุการหมดเวลาของข้อความถ้าปลายทางไม่ได้ข้อความตามเวลาที่กำหนด เหมาะกับ type online หรือ otp
            int timeOut = SmstimeOut != 0 ? SmstimeOut : 0;


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
                smsisdn.Text = item.Trim(SmsCentreFunction.MyChar);
                msisdn.Add(smsisdn);
            }
            destination.Msisdn = msisdn;

            //content tag
            Content content = new Content();
            content.Type = smsType;
            content.Language = messageLanguage;
            content.subject = EncodingMessage(SettingSubjectMessage);
            content.Message = EncodingMessage(message.Trim(SmsCentreFunction.MyChar));

            //sms tag
            var smssending = new Sms_Request();
            smssending.Transid = refId.Trim(SmsCentreFunction.MyChar);
            smssending.Account = account;
            smssending.Source = source;
            smssending.Destination = destination;
            smssending.Content = content;
            smssending.priority = prioritySms;
            smssending.schedule = schedule != default(DateTime) ? schedule.ToString(GeneralDatetimeFormat, new CultureInfo("en-US")) : "";
            smssending.timeout = timeOut == 0 ? "" : timeOut.ToString();

            //Convert to xml string
            string xmlString = Serialize(smssending).Trim(SmsCentreFunction.MyChar);
            return xmlString;
        }
        
        private static string EncodingMessage(string text)
        {
            var builder = new StringBuilder(text.Length * 5);
            foreach (char chr in text)
            {

                builder.AppendFormat("{0:X4}", (ushort)chr);
            }
            return builder.ToString();
        }

        private static string Encoding_MD5(MD5 md5Hash,string text)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        private static string Serialize(dynamic details)
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
        #endregion
        
        #region Public static methods
        //
        // Standard Functions of SMS
        //- single sms immediately
        //- sms on schedule(การส่งsmsแบบรอเวลาที่กำหนด)
        //- otp sms(one time password)
        //- get credit remain

        /// <summary>
        /// Single sms by send immediately
        /// </summary>
        /// <param name="sMSSingle_DetailRequest"></param>
        /// <returns></returns>
        public static SMS_DetailResult SendSMS(SMSSingle_DetailRequest sMSSingle_DetailRequest)
        {
            return DoSendSMS(sMSSingle_DetailRequest, SettingMessageTypeSMS, SettingPriorityOnline);
        }

        public static OTP_DetailResult SendSmsOTP(OTP_DetailRequest sMS_V2)
        {
            return DoSendRequestOTP(sMS_V2);
        }

        public static Verify_DetailResult VerifyOTP(VerifyOTP_DetailRequest sMS_V2)
        {

            return DoVerifyOTP(sMS_V2);

        }

        public static SMS_DetailResult SendSmsSchedule(SMSSchedule_Single_V2 sMSSchedule_Single_)
        {
            var smsDetail = new SMSSingle_DetailRequest();
            smsDetail.SMSTypeId = sMSSchedule_Single_.SMSTypeId;
            smsDetail.Message = sMSSchedule_Single_.Message;
            smsDetail.PhoneNo = sMSSchedule_Single_.PhoneNo;
            smsDetail.CreatedById = sMSSchedule_Single_.CreatedById;
            smsDetail.ProviderId = sMSSchedule_Single_.ProviderId;
            smsDetail.SenderId = sMSSchedule_Single_.SenderId;

            return DoSendSMS(smsDetail, SettingMessageTypeSMS, SettingPriorityOnline, sMSSchedule_Single_.Schedule);
        }

        public static List<CreditRemain> GetCredit()
        {
            return null;//not support
        }

        public static bool GetDeliveryNotify(string transactionId)
        {//ของ shinee แยก controller 
            return false;//not support
        }
        
        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #region Provider Class
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

    [XmlRoot(ElementName = "REQ_DATA")]
    public class REQ_DATA
    {
        [XmlElement(ElementName = "TRANSID")]
        public string TRANSID { get; set; }

        [XmlElement(ElementName = "KEYAUTHEN")]
        public string KEYAUTHEN { get; set; }

        [XmlElement(ElementName = "RefText")]
        public string RefText { get; set; }

       [XmlElement(ElementName = "Sender")]
        public string Sender { get; set; }

       [XmlElement(ElementName = "Recipient")]
        public string Recipient { get; set; }
    }

    [XmlRoot(ElementName = "RESP_DATA")]
    public class SmsOTPByADV_Response
    {
        [XmlElement(ElementName = "SesID")]
        public string SessionId { get; set; }

        [XmlElement(ElementName = "TRANSID")]
        public string Transid { get; set; }
        
        [XmlElement(ElementName = "RefText")]
        public string RefText { get; set; }

        [XmlElement(ElementName = "Status")]
        public string Status { get; set; }

        [XmlElement(ElementName = "Detail")]
        public string Detail { get; set; }
    }

    [XmlRoot(ElementName = "REQ_DATA")]
    public class SmsOTPVerifyByADV_Request
    {
        [XmlElement(ElementName = "SesID")]
        public string SessionId { get; set; }

        [XmlElement(ElementName = "KEYAUTHEN")]
        public string KeyAuth { get; set; }

        [XmlElement(ElementName = "RefCode")]
        public string RefCode { get; set; }
        
    }

    [XmlRoot(ElementName = "RESP_DATA")]
    public class SmsOTPVerifyByADV_Response
    {
        [XmlElement(ElementName = "SesID")]
        public string SessionId { get; set; }
        
        [XmlElement(ElementName = "Status")]
        public string Status { get; set; }

        [XmlElement(ElementName = "Detail")]
        public string Detail { get; set; }
    }

    #endregion


}