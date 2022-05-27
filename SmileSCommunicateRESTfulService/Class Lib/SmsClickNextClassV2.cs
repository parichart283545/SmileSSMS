using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using RestSharp;
using EntityFramework.Utilities;
using static SmileSCommunicateRESTfulService.Models.Model;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SmileSCommunicateRESTfulService.Models;

namespace SmileSCommunicateRESTfulService.Class_Lib
{
    public class SmsClickNextClassV2
    {
        #region Field
        private const string UserName = "SiamSmile";
        private const string Password = "Admin@1234";
        private const string SenderName = "SiamSmile";
        private static string ApiKey = Properties.Settings.Default.ClickNext_ApiKey;
        private static string SecretKey = Properties.Settings.Default.ClickNext_SecretKey;
        private static string ProjectKey = Properties.Settings.Default.ClickNext_ProjectKeyOTP;
        private static string ApiEndPoint = Properties.Settings.Default.ClickNext_EndPoint;
        #endregion

        #region Private mathods
        private static dynamic RequestApiProvider(dynamic objRequest, string resourceEndpoint , Boolean isPost)
        {
            var client = new RestClient(ApiEndPoint);
            var request = isPost ?  new RestRequest(resourceEndpoint, Method.POST) : new RestRequest(resourceEndpoint, Method.GET);
            IRestResponse restResponse;
            dynamic jsonSender = null;
            request.AddHeader("api_key", ApiKey);
            request.AddHeader("secret_key", SecretKey);
            request.RequestFormat = DataFormat.Json;
            jsonSender = isPost ? JsonConvert.SerializeObject(objRequest) : jsonSender;
            request = isPost ? request.AddJsonBody(jsonSender) : request;
            restResponse = isPost ? client.Post(request) : client.Get(request);
            return restResponse;
        }

        private static SMS_DetailResult DoSendSMS(SMSSingle_DetailRequest sMSSingle_DetailRequest)
        {
            //Declaration parameter
            SMS_DetailResult result = new SMS_DetailResult() // define default of result
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = SmsCentreFunction.statusDetail_waiting,
                Language = "th",
                SumPhone = "1",
                ReferenceId = string.Empty
            };
            try
            {
                if (sMSSingle_DetailRequest.PhoneNo.Length == 10)
                {
                    //1. ASSIGN VALUE
                    var SMSTypeId = sMSSingle_DetailRequest.SMSTypeId;
                    var PhoneNo = sMSSingle_DetailRequest.PhoneNo;
                    var Message = sMSSingle_DetailRequest.Message;
                    var CreatedById = sMSSingle_DetailRequest.CreatedById;

                    //2. INSERT DATABASE ()
                    var result_SMSInstance = SmsCentreFunction.InstanceInsert(SMSTypeId, PhoneNo, Message, CreatedById, sMSSingle_DetailRequest.SenderId, sMSSingle_DetailRequest.ProviderId);
                    if (result_SMSInstance.IsResult == false) // insert fail
                    { 
                        result.Detail = result_SMSInstance.Msg;
                        return result;
                    }
                    
                    // payload
                    var data = new SendSMS_Request();
                    data.message = Message;
                    data.phone = PhoneNo;
                    data.sender = SenderName;
                    data.send_date = "";  // send string empty = send now;
                    data.url = "";

                    var res = new SendSMS_DetailResult();

                    string resourceEndPoint = "send-message";

                    //3. CALL API
                    var response = RequestApiProvider(data , resourceEndPoint , true);

                    //4. CHECK RESULT API RESPONSE
                    if (response.IsSuccessful)
                    {
                        res = JsonConvert.DeserializeObject<SendSMS_DetailResult>(response.Content);

                        //5. UPDATE TRANSACTION HEADER
                        TransactionStatu transactionStatus = SmsCentreFunction.MatchStatusCodeInDBByCode(res.Code);
                        var result_TransactionHeader = SmsCentreFunction.TransactionHeaderInstanceUpdate(Convert.ToInt32(result_SMSInstance.Result), res.Result.Transaction_id, transactionStatus.TransactionStatusID, 1);
                        if (result_TransactionHeader.IsResult == true) // save success
                        {
                            result.Status = res.Code;
                            result.Detail = transactionStatus.TransactionStatusDetail;//res.Detail.Replace(".", "");
                            result.Language = res.Result.Language;
                            result.UsedCredit = res.Result.Usedcredit.ToString();
                            result.SumPhone = res.Result.Sumphone.ToString();
                            result.ReferenceId = res.Result.Transaction_id;
                        }
                        else // save fail
                        {
                            result.Status = res.Code;
                            result.Detail = transactionStatus.TransactionStatusDetail + " | การบันทึกลงฐานข้อมูลมีปัญหา : " + result_TransactionHeader.Msg;
                            result.ReferenceId = res.Result.Transaction_id;
                            return result;
                        }

                    }
                    else
                    {
                        result.Detail = res.Detail;
                        return result;
                    }

                    //6. RESPONSE TO USER
                    return result;
                }
                else
                {
                    result.Detail = "Check Phone Number!";
                    return result;
                }
            }
            catch (Exception e)
            {
                result.Detail = "รอข้อมูล";// ex.Message;
                return result;
            }
        }
        private static OTP_DetailResult DoSendSmsOTP(OTP_DetailRequest sMSSingle_OTPDetailRequest)
        {
            //Declaration parameter
            OTP_DetailResult result = new OTP_DetailResult() // define default of result
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = SmsCentreFunction.statusDetail_waiting,
                RefCode = string.Empty,
                ReferenceId = string.Empty
            };
            try
            {
                if (sMSSingle_OTPDetailRequest.PhoneNo.Length == 10)
                {
                    //1. ASSIGN VALUE
                    var SMSTypeId = sMSSingle_OTPDetailRequest.SMSTypeId;
                    var PhoneNo = sMSSingle_OTPDetailRequest.PhoneNo;
                    var CreatedById = sMSSingle_OTPDetailRequest.CreatedById;
                    var ProviderId = sMSSingle_OTPDetailRequest.ProviderId;
                    var SenderId = sMSSingle_OTPDetailRequest.SenderId;

                    //2. INSERT
                    //28 OTP SmsTypeid
                    var result_SMSInstance = SmsCentreFunction.InstanceInsert(SMSTypeId,PhoneNo,string.Empty,  CreatedById,   SenderId, ProviderId);
                    if (result_SMSInstance.IsResult == false) // insert fail
                    {
                        result.Detail = result_SMSInstance.Msg;
                        return result;
                    }

                    // payload
                    var data = new SendOTP_Request();
                    data.project_key = ProjectKey;
                    data.phone = PhoneNo;

                    var res = new SendOTP_Result_Detail();

                    string resourceEndPoint = "otp-send";

                    //3. CALL API 
                    var response = RequestApiProvider(data, resourceEndPoint, true);

                    //4. CHECK RESULT API RESPONSE
                    if (response.IsSuccessful) {
                        res = JsonConvert.DeserializeObject<SendOTP_Result_Detail>(response.Content);

                        //5. UPDATE DATABASE
                        //Token ใช้เซฟลง 2 field >> ReferenceId and Token
                        TransactionStatu transactionStatus =  SmsCentreFunction.MatchStatusCodeInDBByCode(res.Code);
                        var result_TransactionHeader = SmsCentreFunction.TransactionHeaderOTPUpdate(Convert.ToInt32(result_SMSInstance.Result), res.Result.token, transactionStatus.TransactionStatusID, 1, res.Result.token, res.Result.ref_code);
                        if (result_TransactionHeader.IsResult == true) //save success
                        {
                            result.Status = res.Code;
                            result.Detail = transactionStatus.TransactionStatusDetail;//res.Detail.Replace(".", "");
                            result.RefCode = res.Result.ref_code;
                            result.ReferenceId = result_SMSInstance.Result;
                        }
                        else //save fail
                        {
                            result.Status = res.Code;
                            result.Detail = transactionStatus.TransactionStatusDetail/*res.Detail*/ + " | การบันทึกลงฐานข้อมูลมีปัญหา : " + result_TransactionHeader.Msg;
                            result.RefCode = res.Result.ref_code;
                            result.ReferenceId = result_SMSInstance.Result;
                            return result;
                        }
                    }
                    else  {
                        result.Detail = res.Detail;
                        return result;
                    }

                    //6. RESPONSE TO USER
                    return result;

                }
                else
                {
                    result.Detail = "Check Phone Number!";
                    return result;
                }
            }
            catch (Exception e)
            {
                result.Detail = "รอข้อมูล";// ex.Message;
                return result;
            }
        }
        private static Verify_DetailResult DoVerifySmsOTP(VerifyOTP_DetailRequest sMSSingle_OTPVerifyRequest)
        {
            //Declaration parameter
            Verify_DetailResult result = new Verify_DetailResult() // define default of result
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = SmsCentreFunction.statusDetail_waiting,
                Result = false,
                //Remark = ""
            };

     
            var ReferenceId = sMSSingle_OTPVerifyRequest.ReferenceId; //Token
            var RefCode = sMSSingle_OTPVerifyRequest.RefCode;
            var OTPCode = sMSSingle_OTPVerifyRequest.OTPCode;
            var ProviderId = sMSSingle_OTPVerifyRequest.ProviderId;
            string Phone = ""; 
            string Token = "";
            int VerifyOTPId = -1; // ใช้ Id ที่ได้จากการ Insert ครั้งแรก เพื่อใช้ Update แก้ปัญหาการอัพเดท record เดิม แทนการใช้ token

            try
            {
                //1. FIND PHONE AND TOKEN
                var findResult = SmsCentreFunction.FindPhoneAndToken(ReferenceId);
                if (findResult.Item1 == "" ||
                    findResult.Item2 == "")
                {
                    //result.Remark = "ไม่พบรายการ";
                    return result;
                }
                Phone = findResult.Item1;
                Token = findResult.Item2;

                //2. INSERT [VerifyOTP]
                var resultInsert = SmsCentreFunction.InsertVerify(Token, RefCode, OTPCode, Phone);
                if (resultInsert.IsResult == false || resultInsert.Result == "-1")
                { // insert fail
                    result.Detail = resultInsert.Msg;
                    return result;
                }

                //2.1 GET verifyOTPId FOR UPDATE DATABASE (5)
                //insert success
                VerifyOTPId = Convert.ToInt32( resultInsert.Result);

                // payload
                    var data = new VerifyOTP_Request();
                    data.token = Token;
                    data.otp_code = OTPCode;
                    data.ref_code = RefCode;

                string resourceEndPoint = "otp-validate";

                //3. CALL API
                var response = RequestApiProvider(data, resourceEndPoint, true);

                var res = new VerifyOTP_Result_Detail();

                //4. CHECK RESULT API RESPONSE
                if (response.IsSuccessful)
                {
                    res = JsonConvert.DeserializeObject<VerifyOTP_Result_Detail>(response.Content);

                    //5. UPDATE DATABSE [VerifyOTP]
                    //CASE 1 :  res.Result.status = false ====> invalid OTP  or invalid refCode
                    //CASE 2 :  res.Result = null ====> token time out
                    if (res.Result != null) { //CASE 1
                        if (!res.Result.status ) {
                            res.Code = "005";
                        }
                    }
                    TransactionStatu transactionStatus = SmsCentreFunction.MatchStatusCodeInDBByCode(res.Code);
                    var resultUpdate = SmsCentreFunction.UpdateVerify(VerifyOTPId, transactionStatus.TransactionStatusID.ToString(), transactionStatus.TransactionStatusDetail); // ถ้ามาถึงขั้น Update แล้ว = Token ที่ FindPhoneAndToken() ใช้ได้เลย
                                                                                                                                                                                 // if save is success
                    if (resultUpdate.IsResult == true) //save success
                    {
                        result.Status = res.Code;
                        result.Detail = transactionStatus.TransactionStatusDetail;//res.Detail;
                                                                                  // result.Remark = transactionStatus.TransactionStatusDetail;

                        if (res.Result == null)  //CASE 2
                        {
                            result.Result = false;
                        }
                        else
                        {
                            result.Result = res.Result.status;
                            //if (result.Result) result.Remark = "";
                        }
                        return result;
                    }
                    else //save fail
                    {
                        result.Status = res.Code;
                        result.Detail = transactionStatus.TransactionStatusDetail + " | การบันทึกลงฐานข้อมูลมีปัญหา : " + resultUpdate.Msg;
                        //result.Remark = transactionStatus.TransactionStatusDetail; 
                        return result;
                    }
                    
                }
                else {
                    result.Detail = res.Detail;
                    return result;
                }

            }
            catch (Exception e)
            {
                result.Detail = "รอข้อมูล";// ex.Message;
                return result;
            }
        }
        private static List<CreditRemain> DoGetCredit() {

            var lstResult = new List<CreditRemain>();
            var detailResult = new CreditRemain {
                Name = "บริษัท สยามสไมล์โบรกเกอร์ (ประเทศไทย) จำกัด",
            };

            //CALL GET API
            string resourceEndpoint = "get-credit";
            var response = RequestApiProvider(null, resourceEndpoint,false);

            if (response.IsSuccessful)
            {
                //response credit กลับมาเป็น  string (decimal) ถ้า convert เป็น int ค่าจะถูกปัดเศษ (93.50 >> 94)
                var result = JsonConvert.DeserializeObject<Credit_Detail>(response.Content);
                int creditTempt = (int)Convert.ToInt64(Convert.ToDouble(result.Result.Credit));

                string detail = result.Detail.Replace(".", "");
                detailResult.Name = result.Result.Name;
                detailResult.Credit = creditTempt;
                detailResult.Status = result.Code;
                detailResult.Detail = detail;

                lstResult.Add(detailResult);
            }

            return lstResult;
        }
        private static DeliveryNotift_Detail DoGetDeliveryNotify(string transaction_id) {
            //Convert to json
            string transactionString = "{'transaction_id':" + "'" + transaction_id + "'}";
            JavaScriptSerializer j = new JavaScriptSerializer();
            object obj = j.Deserialize(transactionString, typeof(object));
            //CALL GET API
            string resourceEndpoint = "get-credit";
            var response = RequestApiProvider(obj, resourceEndpoint, true);
            var result = JsonConvert.DeserializeObject<DeliveryNotift_Detail>(response.Content);
            return result;
        }

      
        #endregion

        #region Public methods
        public static SMS_DetailResult SendSMS(SMSSingle_DetailRequest sMSSingle_DetailRequest)
        {
            return DoSendSMS(sMSSingle_DetailRequest);
        }

        public static OTP_DetailResult SendSmsOTP(OTP_DetailRequest sMSSingle_OTPDetailRequest)
        {
            return DoSendSmsOTP(sMSSingle_OTPDetailRequest);
        }

        public static Verify_DetailResult VerifyOTP(VerifyOTP_DetailRequest sMSSingle_OTPVerifyRequest)
        {
            return DoVerifySmsOTP(sMSSingle_OTPVerifyRequest);
        }

        public static List<CreditRemain> GetCredit()
        {
            return DoGetCredit();
        }

        public static DeliveryNotift_Detail GetDeliveryNotify(string transaction_id)
        {
            return DoGetDeliveryNotify(transaction_id);

        }
        #endregion

        #region Class
        //SMS Request obj
        public class SendSMS_Request { 
            public string message { get; set; }
            public string phone { get; set; }
            public string sender { get; set; }
            public string send_date { get; set; }
            public string url { get; set; }
                
        }

        //SMS Response obj
        public class SendSMS_DetailResult
        {
            public string Code { get; set; }
            public string Detail { get; set; }

            public SendSMS_Result Result;
        }

        public class SendSMS_Result
        {
            public int Blacklist { get; set; }
            public int Duplicate { get; set; }
            public string Language { get; set; }
            public int Sumphone { get; set; }
            public string Transaction_id { get; set; }
            public int Usedcredit { get; set; }
        }

        public class Credit_Detail
        {
            public string Code { get; set; }
            public string Detail { get; set; }

            public Credit_Result Result ;

        }

        public class Credit_Result
        {
            public string Credit { get; set; }
            public string Name { get; set; }

        }

        public class DeliveryNotift_Detail
        {
            public string Code { get; set; }
            public string Detail { get; set; }
            public List<DeliveryNotift_Result> Result { get; set; }

        }
        public class DeliveryNotift_Result
        {
            public int Credit { get; set; }
            public int Fail { get; set; }
            public string Phone { get; set; }
            public int Success { get; set; }

        }

        //Send OTP Request
        public class SendOTP_Request
        {
            public string project_key { get; set; }
            public string phone { get; set; }
            public string ref_code { get; set; }

        }

        //Send OTP Response
        public class SendOTP_Result_Detail
        {
            public string Code { get; set; }
            public string Detail { get; set; }
            public  SendOTP_Result Result ;

        }

        public class SendOTP_Result
        {
            public string token { get; set; }
            public string ref_code { get; set; }

        }

        //Verify OTP Request
        public class VerifyOTP_Request
        {
            public string token { get; set; }
            public string ref_code { get; set; }
            public string otp_code { get; set; }

        }

        //Verify OTP Response
        public class VerifyOTP_Result_Detail
        {
            public string Code { get; set; }
            public string Detail { get; set; }

            public VerifyOTP_Result Result;

        }

        public class VerifyOTP_Result
        {
            public bool status { get; set; }

        }

        #endregion


    }
}