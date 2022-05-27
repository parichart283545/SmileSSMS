using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SmileSCommunicate.SmsServiceReference;
using System.Xml;
using System.IO;
using SmileSCommunicate.Models;
using System.Text.RegularExpressions;
using RestSharp;

namespace SmileSCommunicate.Controllers
{
    public class TestWebServiceController : Controller
    {
        // GET: TestWebService
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult TEST_WebServiceSMS()
        {
            //var PathFile = Server.MapPath("") + "TEST_SEND_SMS_FILE.txt";
            var PathFile = "C:\\Users\\TPA-NB-14\\Desktop\\File\\TEST_SEND_SMS_FILE2.txt";
            var sr = new StreamReader(PathFile, System.Text.Encoding.Default);
            var StrData = sr.ReadToEnd();
            sr.Close();

            var ByteData = System.Text.ASCIIEncoding.GetEncoding("TIS-620").GetBytes(StrData);
            StrData = Convert.ToBase64String(ByteData);

            var Username = "SiamSmile";
            var Password = "Admin@1234";
            var FileData = StrData;
            var FileName = "TEST_SEND_SMS_FILE2.txt";
            var SenderName = "SiamSmile";
            var SendDate = "2019-06-11 09:50:00";

            var ObjSMS = new SMSServicePortTypeClient();
            var Result = ObjSMS.sendSMSFile(Username, Password, FileData, FileName, SenderName, SendDate);

            var XML = new XmlDocument();
            XML.LoadXml(Result);
            var Element = XML.DocumentElement;

            foreach (XmlNode NodeChild in Element.ChildNodes)
            {
                Response.Write("[ " + NodeChild.Name + " ] => " + NodeChild.InnerXml);
                Response.Write("<br>");
            }

            return View();
        }

        [HttpGet]
        public void InsertDataToQueueHeader()
        {
            using (var context = new CommunicateV1Entities())
            {
                var queueHeader = new SMSQueueHeader
                {
                    ProjectId = 1,
                    TotalSMS = 4,
                    Remark = "TEST",
                    CreatedDate = DateTime.Now
                };

                context.SMSQueueHeader.Add(queueHeader);
                context.SaveChanges();

                var HeaderId = queueHeader.SMSQueueHeaderId;

                var queueHeaderDetail = new List<SMSQueueDetail>
                {
                 new SMSQueueDetail {
                    SMSQueueHeaderId = HeaderId,
                    SMSTypeID=1,
                    PhoneNo = "0814142623",
                    Message = "ทดสอบ TEST SEND BY WebService Mark",
                    SMSQueueSentId = null,
                    UpdatedDate = DateTime.Now
                 },
                     new SMSQueueDetail {
                    SMSQueueHeaderId = HeaderId,
                    SMSTypeID=1,
                    PhoneNo = "0922742125",
                    Message = "ทดสอบ TEST SEND BY WebService Boom",
                    SMSQueueSentId = null,
                    UpdatedDate = DateTime.Now
                 },
                     new SMSQueueDetail {
                    SMSQueueHeaderId = HeaderId,
                    SMSTypeID=1,
                    PhoneNo = "0806786279",
                    Message = "ทดสอบ TEST SEND BY WebService Penk",
                    SMSQueueSentId = null,
                    UpdatedDate = DateTime.Now
                 },
                    new SMSQueueDetail {
                    SMSQueueHeaderId = HeaderId,
                    SMSTypeID=1,
                    PhoneNo = "0823884746",
                    Message = "ทดสอบ TEST SEND BY WebService Jack",
                    SMSQueueSentId = null,
                    UpdatedDate = DateTime.Now
                 }
                };

                context.SMSQueueDetail.AddRange(queueHeaderDetail);
                context.SaveChanges();
            }
        }

        public JsonResult TEST_SendSMSByStoredProcedure()
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
                        var path = String.Format("C:\\Users\\TPA-NB-14\\Desktop\\File\\LOG_SendSMSList_QueueSent_{0}_{1}.txt", QueueSentId, name);
                        var fi = new FileInfo(path);
                        // Check if file already exists. If yes, delete it.
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }

                        // Create a file to write to.
                        using (StreamWriter sw = System.IO.File.CreateText(path))
                        {
                            foreach (var i in lstData)
                            {
                                var str = String.Format("{0},{1}", i.PhoneNo, i.Message);
                                sw.WriteLine(str);
                            }
                        }

                        var sendResult = db.usp_SendSMS_Insert(QueueSentId, 1, 3).FirstOrDefault();

                        if (sendResult.IsResult.Value == true)
                        {
                            //Send SMS
                            var PathFile = path;
                            var sr = new StreamReader(PathFile, System.Text.Encoding.UTF8);
                            var StrData = sr.ReadToEnd();
                            sr.Close();

                            var ByteData = System.Text.ASCIIEncoding.GetEncoding("TIS-620").GetBytes(StrData);
                            StrData = Convert.ToBase64String(ByteData);
                            var sendDate2 = DateTime.Now;
                            var sss = sendDate2.AddYears(-543);
                            var ssss = sss.AddMinutes(1);
                            var Username = "SiamSmile";
                            var Password = "Admin@1234";
                            var FileData = StrData;
                            var FileName = fi.Name;
                            var SenderName = "SiamSmile";
                            var SendDate = ssss.ToString("yyyy-MM-dd HH:mm:ss");

                            var ObjSMS = new SMSServicePortTypeClient();
                            var Result = ObjSMS.sendSMSFile(Username, Password, FileData, FileName, SenderName, SendDate);

                            var XML = new XmlDocument();
                            XML.LoadXml(Result);
                            var Element = XML.DocumentElement;

                            var ReferenceID = db.usp_TransactionHeader_Update(Convert.ToInt32(sendResult.Result), Element.OuterXml).FirstOrDefault();

                            return Json(1, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        return Json(0, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (Exception)
                {
                    return Json(0, JsonRequestBehavior.AllowGet);
                }

                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult TEST_SendSMSWebService()
        {
            using (var context = new CommunicateV1Entities())
            {
                //GET DATA AND CREATE TEXT FILE
                var lstData = context.usp_SMSQueueDetail_Select().ToList();

                if (lstData.Count > 0)
                {
                    var dataQueueSent = new SMSQueueSent()
                    {
                        CreatedDate = DateTime.Now
                    };

                    var dataTransactionHeader = new TransactionHeader()
                    {
                        CreatedById = 1,
                        CreatedDate = DateTime.Now
                    };

                    context.TransactionHeader.Add(dataTransactionHeader);
                    context.SMSQueueSent.Add(dataQueueSent);
                    context.SaveChanges();

                    // Update Statement
                    foreach (var i in lstData)
                    {
                        var lstForUpdate = context.SMSQueueDetail.Where(o => o.SMSQueueDetailId == i.SMSQueueDetailId).ToList();

                        if (lstForUpdate != null)
                        {
                            foreach (var ii in lstForUpdate)
                            {
                                ii.SMSQueueSentId = dataQueueSent.SMSQueueSentId;
                            }
                        }
                        context.SaveChanges();
                    }

                    //INSERT TO DB.SMS.SMS && DB.Receiver
                    var dataReceiver = new List<Receiver>();

                    foreach (var i in lstData)
                    {
                        //Assign Value to initialize object
                        var dataSMS = new SMS()
                        {
                            Message = i.Message,
                            SenderID = 3,
                            SMSTypeID = 2,
                            SendDate = DateTime.Now,
                            CreatedByID = 1,
                            CreatedDate = DateTime.Now,
                            SectionID = 1
                        };
                        //SaveChanges -> Get SMSID
                        context.SMS.Add(dataSMS);
                        context.SaveChanges();

                        //Update SMSID IN DB_SMSQueueDetail
                        var updateSMSID = context.SMSQueueDetail.Where(o => o.SMSQueueDetailId == i.SMSQueueDetailId).FirstOrDefault();
                        if (updateSMSID != null)
                        {
                            //Set SMSID = dataSMS.SMSID
                            updateSMSID.SMSID = dataSMS.SMSID;
                        }
                        context.SaveChanges();

                        //SAVE TO DB_TRANSACTION
                        var dataTransaction = new Transaction()
                        {
                            SMSID = dataSMS.SMSID,
                            TransactionHeaderId = dataTransactionHeader.TransactionHeaderId,
                            TransactionStatusID = null,
                            ReferenceID = null,
                            SumPhone = null,
                            UsedCredit = null,
                            CreatedDate = DateTime.Now,
                            GetDeliveryNotify = false,
                            SendingStatusUpdateDate = null,
                            Success = null,
                            Fail = null,
                            Block = null,
                            Expired = null,
                            Processing = null,
                            Unknown = null,
                            XMLResult = null
                        };
                        context.Transaction.Add(dataTransaction);
                        context.SaveChanges();

                        //Assign value To List<Receiver>
                        dataReceiver.Add(new Receiver { SMSID = dataSMS.SMSID, PhoneNo = i.PhoneNo });
                    }
                    //ADD Range To DB_Receiver
                    context.Receiver.AddRange(dataReceiver);
                    context.SaveChanges();

                    //string path = @"c:\temp\MyTest.txt";
                    var strDatetime = DateTime.Now.ToString();
                    var name = Regex.Replace(strDatetime, @"[:/\, ]", "");
                    var path = String.Format("C:\\Users\\TPA-NB-14\\Desktop\\File\\TEST_SEND_SMS_FILE_{0}.txt", name);
                    var fi = new FileInfo(path);
                    // Check if file already exists. If yes, delete it.
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    // Create a file to write to.
                    using (StreamWriter sw = System.IO.File.CreateText(path))
                    {
                        foreach (var i in lstData)
                        {
                            var str = String.Format("{0},{1}", i.PhoneNo, i.Message);
                            sw.WriteLine(str);

                            // Update Statement
                            var update = context.SMSQueueDetail.Where(o => o.SMSQueueHeaderId == i.SMSQueueHeaderId).FirstOrDefault();
                            if (update != null)
                            {
                                update.UpdatedDate = DateTime.Now;
                            }
                            context.SaveChanges();
                        }
                    }

                    //Send SMS
                    //var PathFile = Server.MapPath("") + "TEST_SEND_SMS_FILE.txt";
                    var PathFile = path;
                    var sr = new StreamReader(PathFile, System.Text.Encoding.UTF8);
                    var StrData = sr.ReadToEnd();
                    sr.Close();

                    var ByteData = System.Text.ASCIIEncoding.GetEncoding("TIS-620").GetBytes(StrData);
                    StrData = Convert.ToBase64String(ByteData);

                    var Username = "SiamSmile";
                    var Password = "Admin@1234";
                    var FileData = StrData;
                    var FileName = fi.Name;
                    var SenderName = "SiamSmile";
                    var SendDate = "";

                    var ObjSMS = new SMSServicePortTypeClient();
                    var Result = ObjSMS.sendSMSFile(Username, Password, FileData, FileName, SenderName, SendDate);

                    var XML = new XmlDocument();
                    XML.LoadXml(Result);
                    var Element = XML.DocumentElement;

                    var Status = "";
                    var UsedCredit = "";
                    var SumPhone = "";
                    var TransactionCode = "";

                    foreach (XmlNode NodeChild in Element.ChildNodes)
                    {
                        //Response.Write("[ " + NodeChild.Name + " ] => " + NodeChild.InnerXml);
                        //Response.Write("<br>");
                        switch (NodeChild.Name)
                        {
                            case "status":
                                Status = NodeChild.InnerXml;
                                break;

                            case "usedcredit":
                                UsedCredit = NodeChild.InnerXml;
                                break;

                            case "sumphone":
                                SumPhone = NodeChild.InnerXml;
                                break;

                            case "transaction":
                                TransactionCode = NodeChild.InnerXml;
                                break;

                            default:
                                break;
                        }
                    }

                    //Get Transaction ID
                    var StatusID = context.TransactionStatus.Where(o => o.TransactionStatusCode == Status).FirstOrDefault().TransactionStatusID;

                    //HEADER TRANSACTION
                    //DB_TransactionHeader
                    var updatedTransactionHeader = context.TransactionHeader.Where(o => o.TransactionHeaderId == dataTransactionHeader.TransactionHeaderId).FirstOrDefault();
                    if (updatedTransactionHeader != null)
                    {
                        updatedTransactionHeader.ReferenceId = TransactionCode;
                        updatedTransactionHeader.TransactionStatusID = StatusID;
                        updatedTransactionHeader.SumPhone = Convert.ToInt32(SumPhone);
                        updatedTransactionHeader.UsedCredit = Convert.ToInt32(UsedCredit);
                        updatedTransactionHeader.UpdatedDate = DateTime.Now;

                        context.SaveChanges();
                    }
                    return Json(1, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(0, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpGet]
        public JsonResult CallGetDeliveryNotify()
        {
            try
            {
                using (var context = new CommunicateV1Entities())
                {
                    var Username = "SiamSmile";
                    var Password = "Admin@1234";

                    var lst = context.usp_TransactionGetDeliveryNotify_Select().ToList();
                    // WriteToFile("call usp_TransactionGetDeliveryNotify_Select Count:" + lst.Count);
                    var objSMS = new SMSServicePortTypeClient();
                    var Result = "";
                    if (lst != null)
                    {
                        lst.ForEach(o =>
                        {
                            // WriteToFile("Request objSMS.getDeliveryNotify ReferenceId:" + o.ReferenceId);
                            Result = objSMS.getDeliveryNotify(Username, Password, o.ReferenceId);

                            var XML = new XmlDocument();
                            XML.LoadXml(Result);
                            var Element = XML.DocumentElement;

                            var ID = "";
                            var XMLResult = "";

                            foreach (XmlNode Node in Element.ChildNodes)
                            {
                                foreach (XmlNode NodeAttr in Node.Attributes)
                                {
                                    if (NodeAttr.Name == "id")
                                    {
                                        ID = NodeAttr.Value;
                                        // WriteToFile("Response objSMS.getDeliveryNotify ReferenceId:" + ID);
                                    }
                                }

                                foreach (XmlNode NodeChild in Node.ChildNodes)
                                {
                                    XMLResult = NodeChild.OuterXml;
                                    // WriteToFile("call usp_TransactionGetDeliveryNotify_Update :" + ID + ":" + XMLResult);
                                    context.usp_TransactionGetDeliveryNotify_Update(ID, XMLResult);
                                }
                            }
                        });
                        return Json(1, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(0, JsonRequestBehavior.AllowGet);
            }

            //    using (var context = new CommunicateV1Entities())
            //    {
            //        var Username = "SiamSmile";
            //        var Password = "Admin@1234";

            //        var lst = context.usp_TransactionGetDeliveryNotify_Select().ToList();
            //        var objSMS = new SMSServicePortTypeClient();
            //        var Result = "";
            //        if (lst != null)
            //        {
            //            lst.ForEach(o =>
            //            {
            //                Result = objSMS.getDeliveryNotify(Username, Password, o.ReferenceId);

            //                var XML = new XmlDocument();
            //                XML.LoadXml(Result);
            //                var Element = XML.DocumentElement;

            //                var ID = "";
            //                var XMLResult = "";

            //                foreach (XmlNode Node in Element.ChildNodes)
            //                {
            //                    foreach (XmlNode NodeAttr in Node.Attributes)
            //                    {
            //                        if (NodeAttr.Name == "id") ID = NodeAttr.Value;
            //                    }

            //                    foreach (XmlNode NodeChild in Node.ChildNodes)
            //                    {
            //                        XMLResult = NodeChild.OuterXml;
            //                        context.usp_TransactionGetDeliveryNotify_Update(ID, XMLResult);
            //                    }
            //                }
            //            });
            //            return Json(1, JsonRequestBehavior.AllowGet);
            //        }
            //    }
            //    return Json(0, JsonRequestBehavior.AllowGet);
            // }
            catch (Exception e)
            {
                return Json(e.Message, JsonRequestBehavior.AllowGet);
            }
        }

        // GET:
        public void Index2()
        {
            //555555
        }

        private void SendSMSTEST()
        {
            var listMsg = new List<string>();
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            listMsg.Add("Cuteful");
            try
            {
                //Call Service Send SMS
                var client = new RestClient("http://operation.siamsmile.co.th:9215/api/sms/SendSMSV2");

                var phoneNumber = "0814142623"; //พี่มาร์ค

                foreach (var item in listMsg)
                {
                    //1
                    var param = new { SMSTypeId = 1, Message = item, PhoneNo = phoneNumber, CreatedById = 1 };
                    //Add Json Body to Request
                    var request = new RestRequest().AddJsonBody(param);
                    //Add Header Token
                    request.AddHeader("Authorization", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm9qZWN0aWQiOjl9.XX7R_Ik8pydv2e04ZvrDew8tlszSDrjvTEYKdgO4t7A");
                    //Post Request
                    var response = client.Post(request);

                    var result = response.IsSuccessful;
                }
            }
            catch (Exception e)
            {
                //return exception
                throw;
            }
        }
    }
}