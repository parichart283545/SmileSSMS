using SmileSCommunicateRESTfulService.Models;
using SmileSCommunicateRESTfulService.SMKTSMSService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using static SmileSCommunicateRESTfulService.Models.Model;

namespace SmileSCommunicateRESTfulService.Class_Lib
{
    public class SmsClickNextClass
    {
        #region Fields
        //
        private bool disposed = false;
        //private static CommunicateV1DBContext _context = new CommunicateV1DBContext();
        private const string Username = "SiamSmile";
        private const string Password = "Admin@1234";
        private const string SenderName = "SiamSmile";

        #endregion
        #region Constructors
        //
        public SmsClickNextClass()
        {
            //initailize
        }

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
        #region Private methods
        //
        //Send with send date return smsID
        private static SMS_DetailResult DoSendSMS(SMSSingle_DetailRequest sMSSingle_DetailRequest)
        {
            //Declaration parameter
            SMS_DetailResult result = new SMS_DetailResult() // define default of result
            {
                Status = "1111",
                Detail = "",
                Language = null,
                SumPhone = null,
                ReferenceId = null
            };
            try
            {
                if (sMSSingle_DetailRequest.PhoneNo.Length == 10)
                {
                    //assign value
                    var SMSTypeId = sMSSingle_DetailRequest.SMSTypeId;
                    var PhoneNo = sMSSingle_DetailRequest.PhoneNo;
                    var Message = sMSSingle_DetailRequest.Message;
                    var CreatedById = sMSSingle_DetailRequest.CreatedById;
                    var SenDate = ""; // send string empty = send now;
                                      //INSERT
                                      //var result_SMSInstance = context.usp_SMSInstance_Insert(SMSTypeId, PhoneNo, Message, CreatedById, SenderID).FirstOrDefault(); old version
                    var result_SMSInstance = SmsCentreFunction.InstanceInsert(SMSTypeId, PhoneNo, Message, CreatedById, sMSSingle_DetailRequest.SenderId, sMSSingle_DetailRequest.ProviderId);
                    //CALL SERVICE
                    var client = new SMSServicePortTypeClient();
                    var result_SendMessage = client.sendMessage(Username, Password, PhoneNo, Message, SenderName, SenDate);
                    //LOAD XML RESULT
                    var XML = new XmlDocument();
                    XML.LoadXml(result_SendMessage);
                    var Element = XML.DocumentElement;
                    //CHECK RESULT FROM STORED PROCEDURE
                    int? TransactionHeaderID = null;
                    if (result_SMSInstance.IsResult.Value) TransactionHeaderID = Convert.ToInt32(result_SMSInstance.Result);
                    //UPDATE TRANSACTION HEADER
                    var result_TransactionHeader = SmsCentreFunction.TransactionHeaderInstanceUpdate_Clicknext(TransactionHeaderID, Element.OuterXml);

                    var ElementCount = Element.ChildNodes.Count;

                    result.Status = (ElementCount > 0 ? Element.ChildNodes[0].InnerXml : null);
                    result.Detail = (ElementCount > 1 ? Element.ChildNodes[1].InnerXml : null);
                    result.Language = (ElementCount > 2 ? Element.ChildNodes[2].InnerXml : null);
                    result.UsedCredit = (ElementCount > 3 ? Element.ChildNodes[3].InnerXml : null);
                    result.SumPhone = (ElementCount > 4 ? Element.ChildNodes[4].InnerXml : null);
                    result.ReferenceId = (ElementCount > 5 ? Element.ChildNodes[5].InnerXml : null);
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
                result.Detail = "รอข้อมูล";//e.Message;
                return result;
            }
        }

        private static bool GetResultDeliveryNotify(string transactionId)
        {
            using (var objSms = new SMSServicePortTypeClient())
            {
                var result = objSms.getDeliveryNotify(Username, Password, transactionId);
                var resultvalue = false;

                var xml = new XmlDocument();
                xml.LoadXml(result);
                var element = xml.DocumentElement;

                var dn = new Model.DeliveryNotify();

                //loop Parent nodes
                if (element != null)
                    foreach (XmlNode node in element.ChildNodes)
                    {
                        //loop attributes in parent nodes such as id , status ,detail
                        var lstSubResult1 = new List<string>();
                        foreach (XmlNode nodeAttr in node.Attributes)
                        {
                            lstSubResult1.Add(nodeAttr.Value);
                        }
                        //loop child nodes such as data
                        foreach (XmlNode nodeChild in node.ChildNodes)
                        {
                            var lstSubResult2 = new List<Model.DeliveryNotify>();
                            //loop attribute in child node such as phone ,credit,success ,fail ,block ,expire
                            foreach (XmlNode subNodeAttr in nodeChild.Attributes)
                            {
                                //Add transectionid, status, detail to DeliveryNotify
                                dn.TransId = lstSubResult1[0]; //transaction_id
                                dn.Status = lstSubResult1[1]; //status , (000 -100 - 101)
                                dn.Detail = lstSubResult1[2]; //detail , (OK ..)

                                //Add phone value to DeliveryNotify
                                if (subNodeAttr.Name == "phone")
                                {
                                    dn.Phone = subNodeAttr.Value;
                                } //Add credit value to DeliveryNotfy
                                if (subNodeAttr.Name == "credit")
                                {
                                    dn.Credit = Convert.ToInt32(subNodeAttr.Value);
                                } //Add success value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "success")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Success = false;
                                    }
                                    else
                                    {
                                        dn.Success = true;
                                        dn.DetailStatusCode = "1"; //1 คือ Code Success
                                    }
                                } //Add fail value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "fail")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Fail = false;
                                    }
                                    else
                                    {
                                        dn.Fail = true;
                                        dn.DetailStatusCode = "2"; //2 คือ Code fail
                                    }
                                } //Add block value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "block")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Block = false;
                                    }
                                    else
                                    {
                                        dn.Block = true;
                                        dn.DetailStatusCode = "3"; //3 คือ Code block
                                    }
                                } //Add expire value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "expire")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Expire = false;
                                    }
                                    else
                                    {
                                        dn.Expire = true;
                                        dn.DetailStatusCode = "4"; //4 คือ Code expire
                                    }
                                }
                                //Detail statuscode == null ถึงจะทำสถานะ processing
                                if (dn.DetailStatusCode == null)
                                {
                                    if (subNodeAttr.Name == "processing")
                                    {
                                        if (subNodeAttr.Value == "0")
                                        {
                                            dn.Processing = false;
                                        }
                                        else
                                        {
                                            dn.Processing = true;
                                            dn.DetailStatusCode = "5"; //5 คือ Code processing
                                        }
                                    }

                                    if (subNodeAttr.Name == "unknown")
                                    {
                                        if (subNodeAttr.Value == "0")
                                        {
                                            dn.Unknown = false;
                                        }
                                        else
                                        {
                                            dn.Unknown = true;
                                            dn.DetailStatusCode = "6"; //6 คือ Code Unknown
                                        }
                                    }
                                }
                            }
                            //add to list
                            lstSubResult2.Add(dn);

                            //add listSubResult2 to lstResult
                            //lstResult.AddRange(lstSubResult2);

                            var lstDelivery = lstSubResult2.Select(o => o).ToList();
                            if (lstDelivery.Any())
                            {
                                foreach (var item in lstDelivery)
                                {
                                    SmsCentreFunction.TransactionDetaiInsert(Convert.ToInt32(item.TransId), item.Detail, item.Phone, item.DetailStatusCode, DateTime.Now, item.Credit);
                                    //_context.usp_TransactionDetail_Insert(Convert.ToInt32(item.TransId), item.Detail, item.Phone, item.DetailStatusCode, DateTime.Now, item.Credit);
                                }

                                ////update
                                //_smsDb.usp_TransactionNotGetDeliveryNotify_Update(transactionId); //transaction_id ที่ return กลับมา คือ ReferenceID ของเรา

                                resultvalue = true;
                            }
                        }
                    }
                return resultvalue;
            }
        }
        
        #endregion
        #region Public methods
        //
        public static SMS_DetailResult SendSMS(SMSSingle_DetailRequest sMSSingle_DetailRequest)
        {
            return DoSendSMS(sMSSingle_DetailRequest);
        }

        public static OTP_DetailResult SendSmsOTP(OTP_DetailRequest sMS_V2)
        {
            //มีแบบเดียว ที่ต้องส่งขอOTPจากProvider (เชื่อมต่อแบบใช้ API ***รอข้อมูล)
            OTP_DetailResult result = new OTP_DetailResult {
                Status = SmsCentreFunction.status_waiting,
                Detail = "Not support",
                RefCode = "",
                ReferenceId = ""
            };
            return result; //not support

        }

        public static Verify_DetailResult VerifyOTP(VerifyOTP_DetailRequest sMS_V2)
        {
            //มีแบบเดียว ที่ต้องส่งขอOTPจากProvider (เชื่อมต่อแบบใช้ API ***รอข้อมูล)
            Verify_DetailResult result = new Verify_DetailResult
            {
                Status = SmsCentreFunction.status_waiting,
                Detail = "Not support",
                Result = false

            };
            return result; //not support

        }

        public static List<CreditRemain> GetCredit()
        {
            var SMSClient = new SMSServicePortTypeClient();
            var result = SMSClient.getCreditRemain(Username, Password);

            var xml = new XmlDocument();
            xml.LoadXml(result);
            var element = xml.DocumentElement;

            var lstResult = new List<CreditRemain>();
            var cr = new CreditRemain();

            if (element != null)
                foreach (XmlNode nodeChild in element.ChildNodes)
                {
                    switch (nodeChild.Name)
                    {
                        case "credit":
                            cr.Credit = Convert.ToInt32(nodeChild.InnerXml);
                            break;

                        case "status":
                            cr.Status = nodeChild.InnerXml;
                            break;

                        case "detail":
                            cr.Detail = nodeChild.InnerXml;
                            break;

                        case "name":
                            cr.Name = nodeChild.InnerXml;
                            break;

                        case "expire":
                            cr.Expire = nodeChild.InnerXml;
                            break;
                    }
                }
            lstResult.Add(cr);

            return lstResult;
        }

        public static bool GetDeliveryNotify(string transactionId)
        {
            using (var objSms = new SMSServicePortTypeClient())
            {
                var result = objSms.getDeliveryNotify(Username, Password, transactionId);
                var resultvalue = false;

                var xml = new XmlDocument();
                xml.LoadXml(result);
                var element = xml.DocumentElement;

                var dn = new Model.DeliveryNotify();

                //loop Parent nodes
                if (element != null)
                    foreach (XmlNode node in element.ChildNodes)
                    {
                        //loop attributes in parent nodes such as id , status ,detail
                        var lstSubResult1 = new List<string>();
                        foreach (XmlNode nodeAttr in node.Attributes)
                        {
                            lstSubResult1.Add(nodeAttr.Value);
                        }
                        //loop child nodes such as data
                        foreach (XmlNode nodeChild in node.ChildNodes)
                        {
                            var lstSubResult2 = new List<Model.DeliveryNotify>();
                            //loop attribute in child node such as phone ,credit,success ,fail ,block ,expire
                            foreach (XmlNode subNodeAttr in nodeChild.Attributes)
                            {
                                //Add transectionid, status, detail to DeliveryNotify
                                dn.TransId = lstSubResult1[0]; //transaction_id
                                dn.Status = lstSubResult1[1]; //status , (000 -100 - 101)
                                dn.Detail = lstSubResult1[2]; //detail , (OK ..)

                                //Add phone value to DeliveryNotify
                                if (subNodeAttr.Name == "phone")
                                {
                                    dn.Phone = subNodeAttr.Value;
                                } //Add credit value to DeliveryNotfy
                                if (subNodeAttr.Name == "credit")
                                {
                                    dn.Credit = Convert.ToInt32(subNodeAttr.Value);
                                } //Add success value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "success")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Success = false;
                                    }
                                    else
                                    {
                                        dn.Success = true;
                                        dn.DetailStatusCode = "1"; //1 คือ Code Success
                                    }
                                } //Add fail value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "fail")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Fail = false;
                                    }
                                    else
                                    {
                                        dn.Fail = true;
                                        dn.DetailStatusCode = "2"; //2 คือ Code fail
                                    }
                                } //Add block value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "block")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Block = false;
                                    }
                                    else
                                    {
                                        dn.Block = true;
                                        dn.DetailStatusCode = "3"; //3 คือ Code block
                                    }
                                } //Add expire value to DeliveryNotfy such as true,false
                                if (subNodeAttr.Name == "expire")
                                {
                                    if (subNodeAttr.Value == "0")
                                    {
                                        dn.Expire = false;
                                    }
                                    else
                                    {
                                        dn.Expire = true;
                                        dn.DetailStatusCode = "4"; //4 คือ Code expire
                                    }
                                }
                                //Detail statuscode == null ถึงจะทำสถานะ processing
                                if (dn.DetailStatusCode == null)
                                {
                                    if (subNodeAttr.Name == "processing")
                                    {
                                        if (subNodeAttr.Value == "0")
                                        {
                                            dn.Processing = false;
                                        }
                                        else
                                        {
                                            dn.Processing = true;
                                            dn.DetailStatusCode = "5"; //5 คือ Code processing
                                        }
                                    }

                                    if (subNodeAttr.Name == "unknown")
                                    {
                                        if (subNodeAttr.Value == "0")
                                        {
                                            dn.Unknown = false;
                                        }
                                        else
                                        {
                                            dn.Unknown = true;
                                            dn.DetailStatusCode = "6"; //6 คือ Code Unknown
                                        }
                                    }
                                }
                            }
                            //add to list
                            lstSubResult2.Add(dn);

                            //add listSubResult2 to lstResult
                            //lstResult.AddRange(lstSubResult2);

                            var lstDelivery = lstSubResult2.Select(o => o).ToList();
                            if (lstDelivery.Any())
                            {
                                foreach (var item in lstDelivery)
                                {
                                    SmsCentreFunction.TransactionDetaiInsert(Convert.ToInt32(item.TransId), item.Detail, item.Phone, item.DetailStatusCode, DateTime.Now, item.Credit);
                                    //_context.usp_TransactionDetail_Insert(Convert.ToInt32(item.TransId), item.Detail, item.Phone, item.DetailStatusCode, DateTime.Now, item.Credit);
                                }

                                ////update
                                //_smsDb.usp_TransactionNotGetDeliveryNotify_Update(transactionId); //transaction_id ที่ return กลับมา คือ ReferenceID ของเรา

                                resultvalue = true;
                            }
                        }
                    }
                return resultvalue;
            }
        }
        #endregion
        #region Static methods
        //
        #endregion
    }
}