using EntityFramework.Utilities;
using SmileSCommunicateRESTfulService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static SmileSCommunicateRESTfulService.Models.Model;

namespace SmileSCommunicateRESTfulService.Class_Lib
{
    public class SmsCentreFunction
    {
        #region Private fields
        //
        // Flag: Has Dispose already been called?
        private bool disposed = false;
        public static CommunicateV1DBContext _context {  get; set; }
        private static List<Provider> providerLst; 
        private static List<Sender> senderLst;
        private static List<SMSType>smsTypeLst;
        private static Random random = new Random();
        #endregion
        #region Public fields
        //
        public static readonly string status_waiting = "1111";
        public static readonly string statusDetail_waiting = "รอข้อมูล";
        public static readonly string language_thai = "th";
        public static readonly string language_english = "eng";
        public static readonly char[] MyChar = { '﻿' };//Trim start and end of string

        #endregion
        #region Properties
        //


        #endregion


        #region Constructor & Dispose

        //public static void DoloadList()
        //{

        //}

        public virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                _context.Dispose();
            }

            disposed = true;
           
        }

        #endregion

        #region Private methods
        //
        private static int SetDefultProvider(int? providerId)
        {
            int? newProviderId = providerId;
            if (providerId is null)
            {
                newProviderId = Convert.ToInt32(Properties.Settings.Default.ProviderID_Default);
            };
            return (int)newProviderId;
        }
        private static int SetDefultSender(int? senderId)
        {
            int? newSenderId = senderId;
            if (senderId is null)
            {
                newSenderId = Convert.ToInt32(Properties.Settings.Default.SenderID_Default);
            };
            return (int)newSenderId;
        }
        private static int SetDefultSmsType(int? smsTypeId)
        {
            int? newSmsTypeId = smsTypeId;
            if (smsTypeId is null)
            {
                newSmsTypeId = Convert.ToInt32(Properties.Settings.Default.SmsTypeID_Default);
            };
            return (int)newSmsTypeId;
        }


        private static string ValidateBeforeSendSms(int? smsType, int? providerId, int? senderId, string phoneNumber, string message)
        {
            try
            {
                //SmsTypeId
                SMSType sMSType = smsTypeLst.Where(x => x.SMSTypeID == smsType).SingleOrDefault();
                if (sMSType is null || sMSType.SMSTypeDetail == "n/a")
                    return "SmsType id not found";

                //Message
                if (message.Trim() == string.Empty)
                {
                    return ErrorMessageEmpty;
                }
                else //มีข้อความ
                {
                    //Maximum 536
                    if (message.Length > 536)
                    {
                        return ErrorMessageLenghtOver;
                    }
                }

                //Phone number
                if (phoneNumber != string.Empty)
                {
                    //ตรวจสอบความเป็นตัวเลข
                    var isNumeric = int.TryParse(phoneNumber, out _);
                    if (!isNumeric) return ErrorPhoneNumberWrongFormat;

                    phoneNumber = phoneNumber.Trim(MyChar);
                    phoneNumber = phoneNumber.Replace("-", "");//convert to format xxxxxxxxxx
                    int stringLenght = phoneNumber.Length;
                    if (stringLenght < 10)
                    {
                        return ErrorPhoneNumberLenghtNotEnough;
                    }
                    else if (stringLenght == 10)
                    {
                        var regex = @"^0\d{9}$";
                        var match = Regex.Match(phoneNumber, regex, RegexOptions.IgnoreCase);
                        if (!match.Success)
                            return ErrorPhoneNumberWrongFormat;
                    }
                    //else if (stringLenght == 11)
                    //        {
                    //            var regex = @"^66\d{9}$";
                    //            var match = Regex.Match(phoneNumber, regex, RegexOptions.IgnoreCase);
                    //            if (match.Success)
                    //                return Tuple.Create<bool, string>(true, "");
                    //            else return Tuple.Create<bool, string>(false, ERROR_PHONENUMBER_WRONGFORMAT);
                    //        }
                    else if (stringLenght >= 11)
                    {
                        return ErrorPhoneNumberLenghOver;
                    }
                    else return ErrorPhoneNumberWrongFormat;

                }
                else if (phoneNumber == string.Empty)//ไม่มีเบอร์
                {
                    return ErrorPhoneNumberBlank;
                }

                //Provider
                Provider result = providerLst.Where(x => x.ProviderId == providerId).SingleOrDefault();
                if (result is null || result.ProviderDetail == "n/a")
                    return "Provider id not found";

                //Sender
                Sender result_s = senderLst.Where(x => x.SenderID == senderId).SingleOrDefault();
                if (result_s is null || result_s.SenderDetail == "n/a")
                    return "Sender id not found";
                
               

               

                return string.Empty;


            }
            catch (Exception ex)
            {
                return statusDetail_waiting;//$"Error validate : {ex.Message}";
            }
        }

        private static string ValidateRequestObject(dynamic objRequest)
        {
            int? createId = null;
            string phoneNo = string.Empty;
            string message = string.Empty;
            //string otpCode = string.Empty;
            //string refCode = string.Empty;
            //string refId = string.Empty;
            bool needMessage = true;
            bool needPhoneNo = true;
            int? providerId = -1;
            int? senderId = -1;
            int? smsType = -1;
            
            Type type = objRequest.GetType();
            if (type == typeof(OTP_DetailRequest))
            {
                needMessage = false;//otp ไม่ต้องเช็คความถูกต้องของข้อความ
                OTP_DetailRequest otp = (OTP_DetailRequest)objRequest;
                createId = otp.CreatedById;
                phoneNo = otp.PhoneNo;
                smsType = otp.SMSTypeId;
                providerId = otp.ProviderId;
                senderId = otp.SenderId;
            }
            else if (type == typeof(VerifyOTP_DetailRequest))
            {
                needMessage = false;//otp ไม่ต้องเช็คความถูกต้องของข้อความ
                needPhoneNo = false;
                VerifyOTP_DetailRequest verify = (VerifyOTP_DetailRequest)objRequest;
                if (verify.ReferenceId.Trim() == string.Empty) return ErrorNoReferenceID;
                if (verify.RefCode.Trim() == string.Empty) return ErrorNoRefCode;
                if (verify.OTPCode.Trim() == string.Empty) return ErrorNoOTP;
                providerId = verify.ProviderId;
            }
            
            try
            {
                //SmsTypeId
                if (smsType != -1)
                {
                    SMSType sMSType = smsTypeLst.Where(x => x.SMSTypeID == smsType).SingleOrDefault();
                    if (sMSType is null || sMSType.SMSTypeDetail == "n/a")
                        return "SmsType id not found";
                }


                //Provider
                if (providerId != -1)
                {
                    Provider result = providerLst.Where(x => x.ProviderId == providerId).SingleOrDefault();
                    if (result is null || result.ProviderDetail == "n/a")
                        return "Provider id not found";
                }


                //Sender
                if (senderId != -1)
                {
                    Sender result_s = senderLst.Where(x => x.SenderID == senderId).SingleOrDefault();
                    if (result_s is null || result_s.SenderDetail == "n/a")
                        return "Sender id not found";
                }


                //Phone number 0xxxxxxxxx 
                if (needPhoneNo)
                {
                    string phoneCheck = ValidatePhoneNumber(phoneNo);
                    if (phoneCheck != string.Empty) return phoneCheck;
                }
               

                //Validate message
                if (needMessage)
                {
                    if (message.Trim() == string.Empty)
                    {
                        return ErrorMessageEmpty;
                    }
                    else //มีข้อความ
                    {
                        //Maximum 536
                        if (message.Length > 536)
                        {
                            return ErrorMessageLenghtOver;
                        }
                    }
                    
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                return statusDetail_waiting;//$"Error validate : {ex.Message}";
            }
        }

        private static string ValidatePhoneNumber(string phoneNo)
        {
            string phone = phoneNo.Trim();
            string newPhone = string.Empty;
            //ไม่มีเบอร์โทรศัพท์
            if (phone == string.Empty) return ErrorPhoneNumberBlank; 
            //กำจัดขีด -
            if (phone.Contains("-")) newPhone = phone.Replace("-", "");
            else newPhone = phone;

            //ตรวจสอบความเป็นตัวเลข
            var isNumeric = int.TryParse(newPhone, out _);
            if (!isNumeric) return ErrorPhoneNumberWrongFormat;

            //ตรวจสอบความยาวของเบอร์โทร
            int lenPhone = newPhone.Length;
            if (lenPhone < 10) return ErrorPhoneNumberLenghtNotEnough; //ไม่ถึง 10 หลัก
            if (lenPhone > 10) return ErrorPhoneNumberLenghOver; //เกิน 10 หลัก
            //if (lenPhone > 11) return ErrorPhoneNumberLenghOver; //เกิน 11 หลัก

            //ตรวจสอบรูปแบบเบอร์โทรตามความยาวของเบอร์
            //10 หลักขึ้นต้นด้วย 0 (ศูนย์)
            if (lenPhone == 10)
            {
                var regex = @"^0\d{9}$";
                var match = Regex.Match(newPhone, regex, RegexOptions.IgnoreCase);
                if (!match.Success)
                    return ErrorPhoneNumberWrongFormat;
            }
            //11 หลักขึ้นต้นด้วย 66
            if (lenPhone == 11)
            {
                var regex = @"^66\d{9}$";
                var match = Regex.Match(newPhone, regex, RegexOptions.IgnoreCase);
                if (match.Success)
                    return ErrorPhoneNumberWrongFormat;
            }
            return string.Empty; //ตรวจสอบเบอร์ผ่านทั้งหมด
        }
        #endregion

        #region Public static methods
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static SMSQueueHeaderDetailResult SMSToQueueHeaderDetail(SMSQueueHeaderDetailViewModel dataInfo)
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
                    catch (Exception ex)
                    {
                        var Result = new Model.SMSQueueHeaderDetailResult()
                        {
                            Status = status_waiting,
                            Detail = statusDetail_waiting,//ex.Message,
                            ReferenceHeaderID = null
                        };
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

                        return Result;
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

                        return Result;
                    }
                }
                else
                {
                    //return result statement (FAIL , Data is Null )
                    var Result = new Model.SMSQueueHeaderDetailResult()
                    {
                        Status = status_waiting,
                        Detail = "Error data does not match",
                        ReferenceHeaderID = null
                    };

                    return Result;
                }
            }
            catch (Exception e)
            {
                //return result statement (FAIL,Other Error )
                var Result = new Model.SMSQueueHeaderDetailResult()
                {
                    Status = status_waiting,
                    Detail = statusDetail_waiting,// e.Message,
                    ReferenceHeaderID = null
                };
                
                return Result;
            }
        }

        public static SMSQueueHeaderDetailResultV2 SMSToQueueHeaderDetailV2(SMSQueueHeaderDetailViewModelV2 dataInfo)
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
                    catch (Exception ex)
                    {
                        var Result = new Model.SMSQueueHeaderDetailResultV2()
                        {
                            Status = status_waiting,
                            Detail = statusDetail_waiting,//ex.Message,
                            SMSQueueHeaderId = null
                        };
                    }
                }

                var DataDetail = dataInfo.Data;
                //check data is not null
                if (DataDetail.Length == Total)
                {
                    if (DataDetail.Length > 0)
                    {
                        //Check duplicate Ref
                        var nDataDetail = DataDetail.Where(x => x.Ref != null && x.Ref != "").ToList();
                        var duplicateKeys = nDataDetail.GroupBy(x => x.Ref)
                                            .Where(group => group.Count() > 1)
                                            .Select(group => group.Key);
                        if(duplicateKeys.Count()>0)
                        {
                            //return result statement (FAIL , Data is Null )
                            var ResultDup = new Model.SMSQueueHeaderDetailResultV2()
                            {
                                Status = status_waiting,
                                Detail = "Duplicate Ref, please try again.",
                                SMSQueueHeaderId = null
                            };

                            return ResultDup;
                        }

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
                                    UpdatedDate = updatedDate,
                                    Ref = DataDetail[i].Ref
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
                                    UpdatedDate = updatedDate,
                                    Ref = DataDetail[0].Ref
                                });
                                context.SaveChanges();
                                context.Dispose();
                            }
                        }

                        //return result statement (SUCCESS, Ok)
                        var Result = new Model.SMSQueueHeaderDetailResultV2()
                        {
                            Status = "000",
                            Detail = "OK",
                            SMSQueueHeaderId = headerId.ToString()
                        };

                        return Result;
                    }
                    else
                    {
                        //return result statement (FAIL , Data is Null )
                        var Result = new Model.SMSQueueHeaderDetailResultV2()
                        {
                            Status = (!(DataDetail.Length > 0) ? "101" : ""),
                            Detail = (!(DataDetail.Length > 0) ? "DATA IS NULL" : ""),
                            SMSQueueHeaderId = null
                        };

                        return Result;
                    }
                }
                else
                {
                    //return result statement (FAIL , Data is Null )
                    var Result = new Model.SMSQueueHeaderDetailResultV2()
                    {
                        Status = status_waiting,
                        Detail = "Error data does not match",
                        SMSQueueHeaderId = null
                    };

                    return Result;
                }
            }
            catch (Exception e)
            {
                //return result statement (FAIL,Other Error )
                var Result = new Model.SMSQueueHeaderDetailResultV2()
                {
                    Status = status_waiting,
                    Detail = statusDetail_waiting,// e.Message,
                    SMSQueueHeaderId = null
                };

                return Result;
            }
        }

        public static Tuple<string, usp_GetMessageDetail_BySMSId_Select_Result> DoGetSmsDetail(int SMSId)
        {
            try
            {
                var result = _context.usp_GetMessageDetail_BySMSId_Select(SMSId).FirstOrDefault();

                return Tuple.Create("", result);
            }
            catch (Exception e)
            {
                return Tuple.Create<string, usp_GetMessageDetail_BySMSId_Select_Result>($"error! : {e}", null); 
            }
        }

        //request credit remain with provider
        public static List<CreditRemain> RequestCreditRemain(int? providerId = 2)
        {
            List<CreditRemain> result = null;
            switch (providerId)
            {
                case 2:
                    result = SmsClickNextClass.GetCredit();
                    break;
                case 3:
                    result = SmsShineeClass.GetCredit();
                    break;
                default:
                    result = SmsShineeClass.GetCredit();
                    break;
            }
            return result;
        }

        //search sms by criteria in message
        public static Tuple<string, List<Search_DetailResult>> DoSearchByMessage(string phoneNumber = "",string criteria="", int? smsTypeId = null)
        {
            try
            {
                //ค้นหาจากเบอร์โทร
                if (phoneNumber != "")
                {
                    //validate phone number
                    if (phoneNumber.Trim().Length == 10)
                    {
                        var list = _context.usp_GetListOfSMSId_ByPhoneNumber_Select(smsTypeId, phoneNumber.Trim()).ToList();
                        if (list.Count() > 0)
                        {
                            var resultList = new List<Search_DetailResult>();
                            foreach (var item in list)
                            {
                                resultList.Add(new Search_DetailResult()
                                {
                                    PhoneNo = item.PhoneNo,
                                    SendDate = item.SendDate,
                                    SMSTypeDetail = item.SMSTypeDetail,
                                    Message = item.Message,
                                    TransactionStatusDetail = item.TransactionStatusDetail,
                                    TransactionDetailStatusDetail = item.TransactionDetailStatusDetail
                                });
                            }
                            return Tuple.Create("", resultList);
                        }
                        else return Tuple.Create("", new List<Search_DetailResult>());

                    }
                    else
                    {
                        return Tuple.Create("error! : กรุณากรอกหมายเลขให้ถูกต้อง", new List<Search_DetailResult>());
                    }
                    
                }
                //ค้นหาจากข้อความ
                if (criteria != "")
                {
                    var list = _context.usp_GetListSMSByMessage_Select(smsTypeId, criteria).ToList();
                    if (list.Count() > 0)
                    {
                        var resultList = new List<Search_DetailResult>();
                        foreach (var item in list)
                        {
                            resultList.Add(new Search_DetailResult()
                            {
                                PhoneNo = item.PhoneNo,
                                SendDate = item.SendDate,
                                SMSTypeDetail = item.SMSTypeDetail,
                                Message = item.Message,
                                TransactionStatusDetail = item.TransactionStatusDetail,
                                TransactionDetailStatusDetail = item.TransactionDetailStatusDetail
                            });
                        }
                        return Tuple.Create("", resultList);
                    }
                    else return Tuple.Create("", new List<Search_DetailResult>());
                }
                //ไม่ได้กรอกทั้งเบอร์ หรือ ข้อความ เพื่อค้นหา
                return Tuple.Create<string, List<Search_DetailResult>>("กรุณากรอกคำค้นหา", null);

            }
            catch (Exception ex)
            {
                return Tuple.Create<string, List<Search_DetailResult>>($"error! : {ex.Message}", null);
            }
                
        }

        //sent sms
        public static SMS_DetailResult SendSMS(SMSSingle_DetailRequest detailRequest)
        {
            //of object is null then return error
            if (detailRequest is null) return CreateSMSDetailResult(status_waiting, "Parameter is not valid");

            //Set default
            detailRequest.SMSTypeId = SetDefultSmsType(detailRequest.SMSTypeId);
            detailRequest.ProviderId = SetDefultProvider(detailRequest.ProviderId);
            detailRequest.SenderId = SetDefultSender(detailRequest.SenderId);
            //validate provider id, sender id, phone number
            string validateResult = ValidateBeforeSendSms(detailRequest.SMSTypeId,
                                                                                              detailRequest.ProviderId,
                                                                                              detailRequest.SenderId,
                                                                                              detailRequest.PhoneNo,
                                                                                              detailRequest.Message);
            if (validateResult != "") {
                SMS_DetailResult result = new SMS_DetailResult() // define default of result
                {
                    Status = status_waiting,
                    Detail = validateResult,
                    Language = "th",
                    SumPhone = "1",
                    ReferenceId = null
                };
                return result;
            }

            switch (detailRequest.ProviderId)
            {
                case 2:
                    return SmsClickNextClass.SendSMS(detailRequest);
                case 3:
                    return SmsShineeClass.SendSMS(detailRequest);
                default:
                    return SmsShineeClass.SendSMS(detailRequest);
            }

        }

        //Generate RefCode
        public static string GetRefCodeOTP()
        {
            string refCode = RandomString(6);
            return refCode;
            
        }

        //sent sms OTP
        public static OTP_DetailResult SendOTPRequest(OTP_DetailRequest detailRequest)
        {
            //Create result object default
            OTP_DetailResult result = new OTP_DetailResult
            {
                Status = status_waiting,
                Detail = statusDetail_waiting,
                RefCode = string.Empty,
                ReferenceId = string.Empty
            };
            //of object is null then return error
            if (detailRequest is null)
            {
                result.Detail = "Parameter is not valid";
                return result;
            };
            
            //Set default
            detailRequest.SMSTypeId = SetDefultSmsType(detailRequest.SMSTypeId);
            detailRequest.ProviderId = SetDefultProvider(detailRequest.ProviderId);
            detailRequest.SenderId = SetDefultSender(detailRequest.SenderId);

            //Validate smsType
            if (detailRequest.SMSTypeId != 28)
            {
                result.Detail = "SMSTypeId ไม่ถูกต้อง";
                return result;
            }


            //validate provider id, sender id, phone number
            //string validateResult = ValidateBeforeSendSms(Convert.ToInt32(detailRequest.ProviderId),
            //                                                                                  Convert.ToInt32(detailRequest.SenderId),
            //                                                                                  detailRequest.PhoneNo,
            //                                                                                  "");
            string validateResult = ValidateRequestObject(detailRequest);


            if (validateResult != "")
            {
                result.Detail = validateResult;
                return result;
            }

            switch (detailRequest.ProviderId)
            {
                case 2:
                    return SmsClickNextClass.SendSmsOTP(detailRequest);
                case 3:
                    return SmsShineeClass.SendSmsOTP(detailRequest);
                default:
                    return SmsShineeClass.SendSmsOTP(detailRequest);
            }

        }


        /// <summary>
        /// Find phone number and token by referenceId
        /// </summary>
        /// <param name="refId"></param>
        /// <returns>phone number, token</returns>
        public static Tuple<string, string> FindPhoneAndToken(string transactionHeaderId)
        {
            try
            {
                //Convert string to interger
                int thId = Convert.ToInt32(transactionHeaderId);
                //Get OTP Detail
                usp_GetOTPDetail_Result result = _context.usp_GetOTPDetail(thId).SingleOrDefault();

               //Return result
                if(result is null)
                return Tuple.Create<string, string>("", ""); // if stored return null object
                else return Tuple.Create<string, string>(result.PhoneNo, result.Token); //have the data

            }
            catch (Exception e)
            {
                return Tuple.Create<string, string>("", "");
            }
        }

        public static Verify_DetailResult VerifyOTP(VerifyOTP_DetailRequest detailRequest)
        {
            //of object is null then return error
            if (detailRequest is null) return new Verify_DetailResult { Status = status_waiting, Detail = "Parameter is not valid" };


            //Set default
            detailRequest.ProviderId = SetDefultProvider(detailRequest.ProviderId);

            //validate provider id, sender id, phone number
            //string validateResult = ValidateBeforeSendSms(Convert.ToInt32(detailRequest.ProviderId),
            //                                                                                  3,
            //                                                                                  string.Empty,
            //                                                                                  "");
            string validateResult = ValidateRequestObject(detailRequest);

            if (validateResult != "" && validateResult != ErrorPhoneNumberBlank)
            {
                Verify_DetailResult result = new Verify_DetailResult() // define default of result
                {
                    Status = status_waiting,
                    Detail = validateResult,
                    Result = false,
                    //Remark = ""
                };
                return result;
            }

            switch (detailRequest.ProviderId)
            {
                case 2:
                    return SmsClickNextClass.VerifyOTP(detailRequest);
                case 3:
                    return SmsShineeClass.VerifyOTP(detailRequest);
                default:
                    return SmsShineeClass.VerifyOTP(detailRequest);
            }

        }

        //create object result
        private static SMS_DetailResult CreateSMSDetailResult(string status = null, string detail = null, string language = null, string usedCredit = null, string sumPhone = null, string referenceId = null)
        {
            SMS_DetailResult result = null;
            if (status is null &&
                detail is null &&
                language is null &&
                usedCredit is null &&
               sumPhone is null &&
               referenceId is null)
            { return result; }
            else
            {
                return result = new SMS_DetailResult()
                {
                    Status = status,
                    Detail = detail,
                    Language = language,
                    UsedCredit = usedCredit,
                    SumPhone = sumPhone,
                    ReferenceId = referenceId
                };
            }
        }
        
        //insert transaction before send sms
        public static usp_SMSInstance2_Insert_Result InstanceInsert(int? smstypeId, string phoneNumber, string message, int? createById, int? senderId, int? providerId)
        {
            try
            {
                var result = _context.usp_SMSInstance2_Insert(smstypeId,
                                                                                                 phoneNumber,
                                                                                                 message,
                                                                                                 createById,
                                                                                                 senderId,
                                                                                                 providerId).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                var resultErr = new usp_SMSInstance2_Insert_Result() { IsResult = false, Msg = $"Insert error : {ex.Message}", Result = "error" };
                return resultErr;
            }
        }

        //update transaction after send sms version 2(Shinee and another provider)
        public static usp_TransactionHeaderInstanceShinee_Update_Result TransactionHeaderInstanceUpdate(int? tHeaderId, string refId, int? statusId, int? sumPhone)
        {
            try
            {
                var result = _context.usp_TransactionHeaderInstanceShinee_Update(tHeaderId,
                                                                                                                                      refId,
                                                                                                                                      statusId,
                                                                                                                                      sumPhone).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                var resultErr = new usp_TransactionHeaderInstanceShinee_Update_Result() { IsResult = false, Msg = $"Update error : {ex.Message}", Result = "error" };
                return resultErr;
            }
        }

        //update transaction after send sms version 1(ClickNext)
        public static usp_TransactionHeaderInstance_Update_Result TransactionHeaderInstanceUpdate_Clicknext(int? refId, string xmlReturn)
        {
            try
            {
                var result = _context.usp_TransactionHeaderInstance_Update(refId,xmlReturn).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                var resultErr = new usp_TransactionHeaderInstance_Update_Result() { IsResult = false, Msg = $"Update error : {ex.Message}", Result = "error" };
                return resultErr;
            }
        }

        //update transaction after send OTP
        public static usp_TransactionHeaderInstanceOTP_Update_Result TransactionHeaderOTPUpdate(int? tHeaderId, string refId, int? statusId, int? sumPhone,string token,string refCode)
        {
            try
            {
                var result = _context.usp_TransactionHeaderInstanceOTP_Update(tHeaderId,
                                                                                                                                      refId,
                                                                                                                                      statusId,
                                                                                                                                      sumPhone,
                                                                                                                                      token,
                                                                                                                                      refCode).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                var resultErr = new usp_TransactionHeaderInstanceOTP_Update_Result() { IsResult = false, Msg = $"Update error : {ex.Message}", Result = "error" };
                return resultErr;
            }
        }

        //Insert Verify OTP data
        public static usp_VerifyOPT_Insert_Result InsertVerify(string token, string refCode, string otpCode, string phoneNo)
        {
            try
            {
                var result = _context.usp_VerifyOPT_Insert(token,
                                                                                          refCode,
                                                                                          otpCode,
                                                                                          phoneNo).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                var resultErr = new usp_VerifyOPT_Insert_Result() { IsResult = false, Msg = $"Update error : {ex.Message}", Result = "error" };
                return resultErr;
            }
        }

        //Update Verify OTP data
        public static usp_VerifyOPT_Update_Result UpdateVerify(int id, string status, string statusDetail)
        {
            try
            {
                var result = _context.usp_VerifyOPT_Update(id,
                                                                                          status,
                                                                                          statusDetail).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                var resultErr = new usp_VerifyOPT_Update_Result() { IsResult = false, Msg = $"Update error : {ex.Message}", Result = "error" };
                return resultErr;
            }
        }

        //insert transaction detail
        public static string TransactionDetaiInsert(int? refId,string detail,string phoneNo, string transStatusId,DateTime? createDatetime,int? credit )
        {
            
            try
            {
                var result = _context.usp_TransactionDetail_Insert(refId,
                                                                                                        detail,
                                                                                                        phoneNo,
                                                                                                        transStatusId,
                                                                                                        createDatetime,
                                                                                                        credit).FirstOrDefault();
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //mapping status result between privider result and Siamsmile database
        public static TransactionStatu MatchStatusCodeInDB(string status)
        {
            try
            {
                int statusId = Convert.ToInt32(status);
                //TransactionStatu result = _context.TransactionStatus.Where(x => x.TransactionStatusID == statusId ).FirstOrDefault();
                TransactionStatu result = (from o in _context.TransactionStatus where o.TransactionStatusID == statusId select o).SingleOrDefault();
                if (result != null)
                { return result; }
                else return null;//1 = รูปแบบ XML ไม่ถูกต้อง
            }
            catch (Exception)
            {
                return null;
            }
            

        }

        
        //mapping status result between privider result and Siamsmile database
        public static TransactionStatu MatchStatusCodeInDBByCode(string statusCode)
        {
            try
            {
                TransactionStatu result = _context.TransactionStatus.Where(x => x.TransactionStatusCode == statusCode).FirstOrDefault();
                if (result != null)
                { return result; }
                else return null;//1 = รูปแบบ XML ไม่ถูกต้อง
            }
            catch (Exception)
            {
                return null;
            }


        }

        //For load data
        public static void DoLoadData()
        {
            providerLst = _context.Providers.ToList();
            senderLst = _context.Senders.ToList();
            smsTypeLst = _context.SMSTypes.ToList();
        }
        #endregion

    }

    #region Class
    //
    #endregion
}