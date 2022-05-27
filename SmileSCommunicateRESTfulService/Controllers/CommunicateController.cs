using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml;
using SmileSCommunicateRESTfulService.Models;
using SmileSCommunicateRESTfulService.SMKTSMSService;
using SmileSCommunicateRESTfulService.BLL;
using System.Web.Http.Cors;
using SmileSCommunicateRESTfulService.Helper;
using Newtonsoft.Json.Linq;
using EntityFramework.Utilities;
using System.Xml.Serialization;
using static SmileSCommunicateRESTfulService.Models.Model;
using System.IO;

namespace SmileSCommunicateRESTfulService.Controllers
{
    //[APIAuthorization]
    [RoutePrefix("api/sms/v1")]
    public class CommunicateController : ApiController
    {
        #region Constructor

        private readonly CommunicateV1DBContext _context;

        public CommunicateController()
        {
            _context = new CommunicateV1DBContext();
        }

        protected virtual void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        #endregion Constructor

        #region API

        /// <summary>
        /// To Queue Header
        /// </summary>
        /// <param name="dataInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(Model.SMSQueueHeaderDetailResult))]
        [Route("SendSMSList")]
        public IHttpActionResult SMSToQueueHeaderDetail([FromBody] Model.SMSQueueHeaderDetailViewModel dataInfo)
        {
            try
            {
                var ProjectId = dataInfo.ProjectId;
                var SMSTypeId = dataInfo.SMSTypeId;
                var Remark = dataInfo.Remark;
                var Total = dataInfo.Total;
                DateTime? SendDate = null;

                if (!string.IsNullOrEmpty(dataInfo.SendDate))
                {
                    try
                    {
                        SendDate = Convert.ToDateTime(dataInfo.SendDate);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                var DataDetail = dataInfo.Data;
                //check data is not null
                if (DataDetail.Length == Total)
                {
                    if (DataDetail.Length > 0)
                    {
                        //Assign length to variable
                        var total = DataDetail.Length;
                        //Initialize List Object
                        var list = new List<SMSQueueDetail>();
                        var updatedDate = DateTime.Now;
                        int? headerId;

                        using (var context = new CommunicateV1DBContext())
                        {
                            context.Configuration.AutoDetectChangesEnabled = false;
                            //Insert to  queue header
                            var resultQueueHeader = context.SMSQueueHeaders.Add(new SMSQueueHeader
                            {
                                ProjectId = ProjectId,
                                TotalSMS = total,
                                Remark = Remark,
                                SendDate = SendDate,
                                CreatedDate = updatedDate
                            });
                            context.SaveChanges();
                            context.Dispose();
                            //assign  queue header id to variable
                            headerId = resultQueueHeader.SMSQueueHeaderId;
                        }

                        if (total > 1)
                        {
                            //loop for insert data to queue detail
                            for (int i = 0; i < total; i++)
                            {
                                list.Add(new SMSQueueDetail()
                                {
                                    SMSQueueHeaderId = headerId,
                                    SMSTypeID = SMSTypeId,
                                    PhoneNo = DataDetail[i].PhoneNo,
                                    Message = DataDetail[i].Message,
                                    SMSID = null,
                                    SMSQueueSentId = null,
                                    UpdatedDate = updatedDate
                                });
                            }

                            using (var db = new CommunicateV1DBContext())
                            {
                                //INSERT LIST ALL
                                EFBatchOperation.For(db, db.SMSQueueDetails).InsertAll(list);

                                db.Dispose();
                            }
                        }
                        else
                        {
                            using (var context = new CommunicateV1DBContext())
                            {
                                context.Configuration.AutoDetectChangesEnabled = false;
                                //Insert to  queue header
                                var resultQueueDetails = context.SMSQueueDetails.Add(new SMSQueueDetail
                                {
                                    SMSQueueHeaderId = headerId,
                                    SMSTypeID = SMSTypeId,
                                    PhoneNo = DataDetail[0].PhoneNo,
                                    Message = DataDetail[0].Message,
                                    SMSID = null,
                                    SMSQueueSentId = null,
                                    UpdatedDate = updatedDate
                                });
                                context.SaveChanges();
                                context.Dispose();
                            }
                        }

                        //return result statement (SUCCESS, Ok)
                        var Result = new Model.SMSQueueHeaderDetailResult()
                        {
                            Status = "000",
                            Detail = "OK",
                            ReferenceHeaderID = headerId.ToString()
                        };

                        return Json(Result, GlobalObject.carmelSetting());
                    }
                    else
                    {
                        //return result statement (FAIL , Data is Null )
                        var Result = new Model.SMSQueueHeaderDetailResult()
                        {
                            Status = (!(DataDetail.Length > 0) ? "101" : ""),
                            Detail = (!(DataDetail.Length > 0) ? "DATA IS NULL" : ""),
                            ReferenceHeaderID = null
                        };

                        return Json(Result, GlobalObject.carmelSetting());
                    }
                }
                else
                {
                    //return result statement (FAIL , Data is Null )
                    var Result = new Model.SMSQueueHeaderDetailResult()
                    {
                        Status = "1111",
                        Detail = "Error data does not match",
                        ReferenceHeaderID = null
                    };

                    return Json(Result, GlobalObject.carmelSetting());
                }
            }
            catch (Exception e)
            {
                //return result statement (FAIL,Other Error )
                var Result = new Model.SMSQueueHeaderDetailResult()
                {
                    Status = "1111",
                    Detail = e.Message,
                    ReferenceHeaderID = null
                };

                return Json(Result, GlobalObject.carmelSetting());
            }
        }

        /// <summary>
        /// Send with send date return smsID - เวอร์ชั่นเก่า
        /// </summary>
        /// <param name="sms"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(List<Model.SMSDetail>))]
        [Route("SendSMS")]
        public IHttpActionResult SendSMS([FromBody] Model.SMS sms)
        {
            //declared
            var ss = new Model.SMSDetail();
            //DateTime sendDateConvert = DateTime.Now;

            //insert data to db
            DateTime sendDateConvert = Convert.ToDateTime(sms.SendDate);
            if (sendDateConvert <= DateTime.Now)
            {
                sendDateConvert = DateTime.Now;
            }

            var resultSMS = _context.usp_SMS_Insert(sms.Message, 3, sms.SMSTypeId, sendDateConvert,
                sms.CreatedById, DateTime.Now, 1).FirstOrDefault();

            try
            {
                _context.usp_Receiver_Insert(resultSMS.SMSID, sms.PhoneNo);

                //check number ==10
                if (sms.PhoneNo.Trim().Length == 10)
                {
                    //declare service
                    var objSms = new SMSServicePortTypeClient();
                    //call service sendMessage Return XML
                    var result = objSms.sendMessage("SiamSmile", "Admin@1234", sms.PhoneNo, sms.Message, "SiamSmile", string.Empty);

                    //get value XML
                    var xml = new XmlDocument();
                    xml.LoadXml(result);
                    var element = xml.DocumentElement;

                    if (element != null)
                    {
                        ss.SMSId = resultSMS.SMSID;
                        ss.Status = element.ChildNodes[0].InnerXml;
                        ss.Detail = element.ChildNodes[1].InnerXml;
                        ss.Language = element.ChildNodes[2].InnerXml;
                        ss.UsedCredit = Convert.ToInt32(element.ChildNodes[3].InnerXml);
                        ss.SumPhone = Convert.ToInt32(element.ChildNodes[4].InnerXml);
                        ss.Transaction = element.ChildNodes[5].InnerXml;

                        var trRerult = _context.usp_Transaction_Insert(resultSMS.SMSID
                                                          , ss.Status
                                                          , ss.Transaction
                                                          , ss.SumPhone
                                                          , ss.UsedCredit
                                                          , DateTime.Now).FirstOrDefault();
                        return Json(trRerult, GlobalObject.carmelSetting());
                    }
                    else
                    {
                        ss.SMSId = resultSMS.SMSID;
                        ss.Status = "101";
                        ss.Detail = "FAIL";
                        ss.Language = "TH";
                        ss.UsedCredit = 0;
                        ss.SumPhone = 0;
                        ss.Transaction = null;

                        //insert tr
                        var trResult = _context.usp_Transaction_Insert(resultSMS.SMSID
                                                          , ss.Status
                                                          , ss.Transaction
                                                          , ss.SumPhone
                                                          , ss.UsedCredit
                                                          , DateTime.Now).FirstOrDefault();

                        //insert tr detail
                        const string detailStatusCode = "2"; // 2 == fail
                        var trdResult = _context.usp_TransactionDetail_Insert(trResult.TransactionID, ss.Detail, sms.PhoneNo.Trim(), detailStatusCode, DateTime.Now, 0);
                        return Json(trdResult, GlobalObject.carmelSetting());
                    }
                }
                else
                {
                    ss.SMSId = resultSMS.SMSID;
                    ss.Status = "101";
                    ss.Detail = "FAIL";
                    ss.Language = "TH";
                    ss.UsedCredit = 0;
                    ss.SumPhone = 0;
                    ss.Transaction = null;

                    //insert tr
                    var trResult = _context.usp_Transaction_Insert(resultSMS.SMSID
                                                      , ss.Status
                                                      , ss.Transaction
                                                      , ss.SumPhone
                                                      , ss.UsedCredit
                                                      , DateTime.Now).FirstOrDefault();

                    //insert tr detail
                    const string detailStatusCode = "2"; // 2 == fail
                    var trdResult = _context.usp_TransactionDetail_Insert(trResult.TransactionID, ss.Detail, sms.PhoneNo.Trim(), detailStatusCode, DateTime.Now, 0);
                    return Json(trdResult, GlobalObject.carmelSetting());
                }
            }
            catch (Exception e)
            {
                ss.SMSId = resultSMS.SMSID;
                ss.Status = "101";
                ss.Detail = "FAIL";
                ss.Language = "TH";
                ss.UsedCredit = 0;
                ss.SumPhone = 0;
                ss.Transaction = null;

                //insert tr
                var trResult = _context.usp_Transaction_Insert(resultSMS.SMSID
                                                  , ss.Status
                                                  , ss.Transaction
                                                  , ss.SumPhone
                                                  , ss.UsedCredit
                                                  , DateTime.Now).FirstOrDefault();

                //insert tr detail
                const string detailStatusCode = "2"; // 2 == fail
                var trdResult = _context.usp_TransactionDetail_Insert(trResult.TransactionID, ss.Detail, sms.PhoneNo.Trim(), detailStatusCode, DateTime.Now, 0);
                return Json(trdResult, GlobalObject.carmelSetting());
            }
        }

        /// <summary>
        /// Send with send date return smsID - เวอร์ชั่นที่ใช้ปัจจุบัน
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(List<Model.SMS_V2_DetailResult>))]
        [Route("SendSMSV2")]
        public IHttpActionResult SendSMSV2([FromBody] Model.SMS_V2 data)
        {
            try
            {
                if (data.PhoneNo.Length == 10)
                {
                    //assign value
                    var SMSTypeId = data.SMSTypeId;
                    var PhoneNo = data.PhoneNo;
                    var Message = data.Message;
                    var CreatedById = data.CreatedById;
                    var SenderID = 3;//3 = SiamSmile
                                     //set default value
                    var Username = "SiamSmile";
                    var Password = "Admin@1234";
                    var Sender = "SiamSmile";
                    var SenDate = ""; // send string empty = send now;
                    using (var context = new CommunicateV1DBContext())
                    {
                        //INSERT
                        var result_SMSInstance = context.usp_SMSInstance_Insert(SMSTypeId, PhoneNo, Message, CreatedById, SenderID).FirstOrDefault();
                        //CALL SERVICE
                        var client = new SMSServicePortTypeClient();
                        var result_SendMessage = client.sendMessage(Username, Password, PhoneNo, Message, Sender, SenDate);
                        //LOAD XML RESULT
                        var XML = new XmlDocument();
                        XML.LoadXml(result_SendMessage);
                        var Element = XML.DocumentElement;
                        //CHECK RESULT FROM STORED PROCEDURE
                        int? TransactionHeaderID = null;
                        if (result_SMSInstance.IsResult.Value) TransactionHeaderID = Convert.ToInt32(result_SMSInstance.Result);
                        //UPDATE TRANSACTION HEADER
                        var result_TransactionHeader = context.usp_TransactionHeaderInstance_Update(TransactionHeaderID, Element.OuterXml).FirstOrDefault();

                        var ElementCount = Element.ChildNodes.Count;

                        var Result = new Model.SMS_V2_DetailResult()
                        {
                            Status = (ElementCount > 0 ? Element.ChildNodes[0].InnerXml : null),
                            Detail = (ElementCount > 1 ? Element.ChildNodes[1].InnerXml : null),
                            Language = (ElementCount > 2 ? Element.ChildNodes[2].InnerXml : null),
                            UsedCredit = (ElementCount > 3 ? Element.ChildNodes[3].InnerXml : null),
                            SumPhone = (ElementCount > 4 ? Element.ChildNodes[4].InnerXml : null),
                            Transaction = (ElementCount > 5 ? Element.ChildNodes[5].InnerXml : null)
                        };

                        return Json(Result, GlobalObject.carmelSetting());
                    }
                }
                else
                {
                    var Result = new Model.SMS_V2_DetailResult()
                    {
                        Status = "1111",
                        Detail = "Check Phone Number!",
                        Language = null,
                        UsedCredit = null,
                        SumPhone = null,
                        Transaction = null
                    };

                    return Json(Result, GlobalObject.carmelSetting());
                }
            }
            catch (Exception e)
            {
                var Result = new Model.SMS_V2_DetailResult()
                {
                    Status = "1111",
                    Detail = e.Message,
                    Language = null,
                    UsedCredit = null,
                    SumPhone = null,
                    Transaction = null
                };

                return Json(Result, GlobalObject.carmelSetting());
            }
        }

        /// <summary>
        /// get transaction and return bool // สำหรับเวอร์ชั่นเก่า (ก่อนปี 2019) ***ปัจจุบันไม่ใช้งานแล้ว
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(bool))]
        [Route("GetTransactionNotDeliveryNotify")]
        private IHttpActionResult GetTransactionNotDeliveryNotify()
        {
            var result = false;

            var resultTransaction = _context.usp_TransactionNotGetDeliveryNotify_Select().ToList();

            if (resultTransaction.Any())
            {
                foreach (var itm in resultTransaction)
                {
                    //call get delivery notify
                    if (itm.ReferenceID != null)
                    {
                        GetDeliveryNotify(itm.ReferenceID);
                    }
                    result = true;
                    //save log from windows service
                    _context.usp_Log_GetDeliveryNotify_Insert(DateTime.Now, true);
                }
            }
            else
            {
                _context.usp_Log_GetDeliveryNotify_Insert(DateTime.Now, false);
            }

            return Json(result, GlobalObject.carmelSetting());
        }

        /// <summary>
        /// Get SMS credit
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(List<Model.CreditRemain>))]
        [Route("GetCredit")]
        public IHttpActionResult GetCredit()
        {
            var SMSClient = new SMSServicePortTypeClient();
            var result = SMSClient.getCreditRemain("SiamSmile", "Admin@1234");

            var xml = new XmlDocument();
            xml.LoadXml(result);
            var element = xml.DocumentElement;

            var lstResult = new List<Model.CreditRemain>();
            var cr = new Model.CreditRemain();

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

            return Json(lstResult, GlobalObject.carmelSetting());
        }

        /// <summary>
        /// get SMS detail
        /// </summary>
        /// <param name="SMSId"></param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(usp_GetMessageDetail_BySMSId_Select_Result))]
        [Route("GetSMSDetail")]
        public IHttpActionResult GetSMSDetail(int SMSId)
        {
            try
            {
                var result = _context.usp_GetMessageDetail_BySMSId_Select(SMSId).FirstOrDefault();

                return Json(result, GlobalObject.carmelSetting());
            }
            catch (Exception e)
            {
                return Json("error! :" + e, GlobalObject.carmelSetting());
            }
        }

        /// <summary>
        /// Search SMS history by phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="smsTypeId"></param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(List<usp_GetListOfSMSId_ByPhoneNumber_Select_Result>))]
        [Route("SearchByPhoneNumber")]
        public IHttpActionResult SearchSMSByPhoneNumber(string phoneNumber, int? smsTypeId)
        {
            try
            {
                if (phoneNumber.Trim().Length == 10)
                {
                    var result = _context.usp_GetListOfSMSId_ByPhoneNumber_Select(smsTypeId, phoneNumber.Trim()).ToList();

                    return Json(result, GlobalObject.carmelSetting());
                }
                else
                {
                    return Json("error! : กรุณากรอกหมายเลขให้ถูกต้อง", GlobalObject.carmelSetting());
                }
            }
            catch (Exception e)
            {
                return Json("error! :" + e, GlobalObject.carmelSetting());
            }
        }

        /// <summary>
        /// search sms by criteria in message
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="smsTypeId"></param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(List<usp_GetListSMSByMessage_Select_Result>))]
        [Route("SearchByCriteria")]
        public IHttpActionResult SearchSMSByCriteria(string criteria, int? smsTypeId)
        {
            try
            {
                var result = _context.usp_GetListSMSByMessage_Select(smsTypeId, criteria).ToList();

                return Json(result, GlobalObject.carmelSetting());
            }
            catch (Exception e)
            {
                return Json("error! :" + e, GlobalObject.carmelSetting());
            }
        }

        #endregion API

        #region Method

        /// <summary>
        /// Get ผลการส่งข้อความ และ update
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public bool GetDeliveryNotify(string transactionId)
        {
            using (var objSms = new SMSServicePortTypeClient())
            {
                var result = objSms.getDeliveryNotify("SiamSmile", "Admin@1234", transactionId);
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
                                    _context.usp_TransactionDetail_Insert(Convert.ToInt32(item.TransId), item.Detail, item.Phone, item.DetailStatusCode, DateTime.Now, item.Credit);
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

        #endregion Method
    }
}