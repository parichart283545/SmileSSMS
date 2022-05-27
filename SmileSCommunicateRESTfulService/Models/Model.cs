using System;
using System.Collections;

namespace SmileSCommunicateRESTfulService.Models
{
    public class Model
    {

        #region Constant Parameter
        //General format
        public const string GeneralDatetimeFormat = "yyyy-MM-dd HH:mm:ss";
        
        //Error message format
        public const string ErrorPhoneNumberNotfound = "ไม่พบเบอร์โทรศัพท์";
        public const string ErrorPhoneNumberBlank = "ไม่มีเบอร์โทรศัพท์";
        public const string ErrorPhoneNumberLenghtNotEnough = "เบอร์โทรศัพท์ไม่ครบหลักที่กำหนด";
        public const string ErrorPhoneNumberLenghOver = "เบอร์โทรศัพท์เกินหลักที่กำหนด";
        public const string ErrorPhoneNumberWrongFormat = "เบอร์โทรศัพท์ผิดรูปแบบ";
        public const string ErrorMessageEmpty = "ไม่มีข้อความ";
        public const string ErrorMessageLenghtOver = "ความยาวของข้อความเกิน 536 ตัวอักษร";
        public const string ErrorRegIDNumber = "เลขที่อ้างอิงต้องเป็นตัวเลขเท่านั้น";
        public const string ErrorRefIdLenghtNotEnough = "ความยาวของเลขที่อ้างอิงน้อยกว่า 14 หลัก";
        public const string ErrorRefIdLenghtOver = "ความยาวของเลขที่อ้างอิงเกิน 50 หลัก";
        public const string ErrorMessageTypeWrong = "Type ผิดรูปแบบ";
        public const string ErrorPriorityWrong = "priority ผิดรูปแบบ";
        public const string ErrorScheduleNotUpToDate = "schedule น้อยกว่าวันปัจจุบัน";
        public const string ErrorTimeOutWrong = "timeout ต้องไม่น้อยกว่า 0";
        public const string ErrorPhoneNumberOutOfRange = "ไม่สามารถส่งข้อความเกิน 10,000 เบอร์";
        public const string ErrorNoReferenceID = "ไม่ได้ใส่ ReferenceId";
        public const string ErrorNoRefCode = "ไม่ได้ใส่ RefCode";
        public const string ErrorNoOTP = "ไม่ได้ใส่ OTP";

        #endregion Constant Parameter

        #region StandardResult

        public static class MappingCode
        {
            public enum ResultMessage
            {
                Success,
                Failure
            }
        }

        public class SMSQueueHeaderDetailResultV2
        {
            public string Status { get; set; }
            public string Detail { get; set; }
            public string SMSQueueHeaderId { get; set; }
        }

        public class SMSQueueHeaderDetailResult
        {
            public string Status { get; set; }
            public string Detail { get; set; }
            public string ReferenceHeaderID { get; set; }
        }

        public class StdResult
        {
            public string Result { get; set; }
            public string MSG { get; set; }
        }

        public class StdResultDebt
        {
            public string Result { get; set; }
            public string Msg { get; set; }
            public IEnumerable DebtRefId { get; set; }
            public IEnumerable AppIdPk { get; set; }
        }

        public class FilterResult
        {
            public string Value { get; set; }
            public string Display { get; set; }
        }

        public class MasterTextAutoCompleteResult
        {
            public string Value { get; set; }
            public string Display { get; set; }
        }

        public class DatatableResult
        {
            public int Draw { get; set; }
            public int RecordsTotal { get; set; }
            public int RecordsFiltered { get; set; }
            public int PageSize { get; set; }
            public int IndexStart { get; set; }
            public string SortField { get; set; }
            public string OrderType { get; set; }
            public string Search { get; set; }
            public int TotalPages { get; set; }
            public IEnumerable Data { get; set; }
        }

        #endregion StandardResult

        #region Request class
        //
        public class SMSSingle_DetailRequest
        {
            public int? SMSTypeId { get; set; }
            public string Message { get; set; }
            public string PhoneNo { get; set; }
            public int? CreatedById { get; set; }
            public int? ProviderId { get; set; }
            public int? SenderId { get; set; }
        }

        public class OTP_DetailRequest
        {
            public int? SMSTypeId { get; set; }
            public string PhoneNo { get; set; }
            public int? CreatedById { get; set; }
            public int? ProviderId { get; set; }
            public int? SenderId { get; set; }
        }
        
        public class VerifyOTP_DetailRequest
        {
            public string ReferenceId { get; set; }
            public string RefCode { get; set; }
            public string OTPCode { get; set; }
            public int? ProviderId { get; set; }
        }

        public class SMSQueueHeaderDetailViewModel
        {
            public int ProjectId { get; set; }
            public int SMSTypeId { get; set; }
            public string Remark { get; set; }
            public int Total { get; set; }
            public string SendDate { get; set; }
            public SMSQueueHeaderDetail[] Data { get; set; }
        }

        public class SMSQueueHeaderDetail
        {
            public string PhoneNo { get; set; }
            public string Message { get; set; }
        }

        public class SMSQueueHeaderDetailViewModelV2
        {
            public int ProjectId { get; set; }
            public int SMSTypeId { get; set; }
            public string Remark { get; set; }
            public int Total { get; set; }
            public string SendDate { get; set; }
            public SMSQueueHeaderDetailV2[] Data { get; set; }
        }

        public class SMSQueueHeaderDetailV2
        {
            public string Ref { get; set; }
            public string PhoneNo { get; set; }
            public string Message { get; set; }
        }
        public class SMSSchedule_Single_V2
        {
            public int? SMSTypeId { get; set; }
            public string Message { get; set; }
            public string PhoneNo { get; set; }
            public int? CreatedById { get; set; }
            public DateTime Schedule { get; set; }
            public int TimeoutMinute { get; set; }
            public int? ProviderId { get; set; }
            public int? SenderId { get; set; }
        }

        #endregion

        #region Response class
        public class SMS_DetailResult
        {
            public string Status { get; set; }
            public string Detail { get; set; }
            public string Language { get; set; }
            public string UsedCredit { get; set; }
            public string SumPhone { get; set; }
            public string ReferenceId { get; set; }
        }

        public class OTP_DetailResult
        {
            public string Status { get; set; }
            public string Detail { get; set; }
            public string RefCode { get; set; }
            public string ReferenceId { get; set; }
        }

        public class Verify_DetailResult
        {
            public string Status { get; set; }
            public string Detail { get; set; }
            public bool Result { get; set; }
            //public string Remark { get; set; }
        }

        public class CreditRemain
        {
            public int Credit { get; set; }
            public string Status { get; set; }
            public string Detail { get; set; }
            public string Name { get; set; }
            public string Expire { get; set; }
        }

        public class Search_DetailResult
        {
            public string PhoneNo { get; set; }
            public DateTime? SendDate { get; set; }
            public string SMSTypeDetail { get; set; }
            public string Message { get; set; }
            public string TransactionStatusDetail { get; set; }
            public string TransactionDetailStatusDetail { get; set; }
        }
        

        #endregion

        #region Normal class
        public class DeliveryNotify
        {
            public string TransId { get; set; }
            public string Status { get; set; }
            public string Detail { get; set; }
            public string Phone { get; set; }
            public int Credit { get; set; }
            public bool Success { get; set; }
            public bool Fail { get; set; }
            public bool Block { get; set; }
            public bool Expire { get; set; }
            public bool Processing { get; set; }
            public bool Unknown { get; set; }
            public string DetailStatusCode { get; set; }
        }


        #endregion
        
        #region SMSCommunicate

        public class SMS
        {
            //public int SMSId { get; set; }
            public string Message { get; set; }

            //public int? SenderId { get; set; }
            public int? SMSTypeId { get; set; }

            public string PhoneNo { get; set; }
            public DateTime SendDate { get; set; }
            public int? CreatedById { get; set; }

            //public DateTime CreatedDate { get; set; }
            //public int? SectionId { get; set; }
        }

        public class SMS_V2

        {
            public int? SMSTypeId { get; set; }
            public string Message { get; set; }
            public string PhoneNo { get; set; }
            public int? CreatedById { get; set; }
        }

        public class SMS_V2_DetailResult
        {
            public string Status { get; set; }
            public string Detail { get; set; }
            public string Language { get; set; }
            public string UsedCredit { get; set; }
            public string SumPhone { get; set; }
            public string Transaction { get; set; }
        }

        public class SMSDetail
        {
            public int? SMSId { get; set; }
            public string Status { get; set; }
            public string Detail { get; set; }
            public string Language { get; set; }
            public int? UsedCredit { get; set; }
            public int? SumPhone { get; set; }
            public string Transaction { get; set; }
        }

        //public class CreditRemain
        //{
        //    public int Credit { get; set; }
        //    public string Status { get; set; }
        //    public string Detail { get; set; }
        //    public string Name { get; set; }
        //    public string Expire { get; set; }
        //}

        //public class DeliveryNotify
        //{
        //    public string TransId { get; set; }
        //    public string Status { get; set; }
        //    public string Detail { get; set; }
        //    public string Phone { get; set; }
        //    public int Credit { get; set; }
        //    public bool Success { get; set; }
        //    public bool Fail { get; set; }
        //    public bool Block { get; set; }
        //    public bool Expire { get; set; }
        //    public bool Processing { get; set; }
        //    public bool Unknown { get; set; }
        //    public string DetailStatusCode { get; set; }
        //}

        //public class SMSQueueHeaderDetailViewModel
        //{
        //    public int ProjectId { get; set; }
        //    public int SMSTypeId { get; set; }
        //    public string Remark { get; set; }
        //    public int Total { get; set; }
        //    public string SendDate { get; set; }
        //    public SMSQueueHeaderDetail[] Data { get; set; }
        //}

        //public class SMSQueueHeaderDetail
        //{
        //    public string PhoneNo { get; set; }
        //    public string Message { get; set; }
        //}

        #endregion SMSCommunicate
    }
}