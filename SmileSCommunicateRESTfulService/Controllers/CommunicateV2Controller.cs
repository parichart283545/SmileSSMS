using System;
using System.Collections.Generic;
using SmileSCommunicateRESTfulService.Models;
using System.Linq;
using System.Web.Http;
using SmileSCommunicateRESTfulService.Helper;
using SmileSCommunicateRESTfulService.BLL;
using System.Web.Http.Description;
using static SmileSCommunicateRESTfulService.Models.Model;
using SmileSCommunicateRESTfulService.Class_Lib;

namespace SmileSCommunicateRESTfulService.Controllers
{
    [APIAuthorization]
    [RoutePrefix("api/sms")]
    public class CommunicateV2Controller : ApiController
    {
        #region Context

        //
        private readonly CommunicateV1DBContext _context;

        public CommunicateV2Controller()
        {
            _context = new CommunicateV1DBContext();
            SmsCentreFunction._context = _context;
            SmsCentreFunction.DoLoadData();
        }

        protected virtual void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        #endregion Context

        #region API

        /// <summary>
        /// To Queue Header
        /// </summary>
        /// <param name="dataInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(Model.SMSQueueHeaderDetailResult))]
        [Route("SendSMSList")]
        public IHttpActionResult SMSToQueueHeaderDetail([FromBody] SMSQueueHeaderDetailViewModel dataInfo)
        {
            return Json(SmsCentreFunction.SMSToQueueHeaderDetail(dataInfo), GlobalObject.carmelSetting());
        }

        /// <summary>
        /// To Queue Header New design for Premium Project 2022
        /// </summary>
        /// <param name="dataInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(Model.SMSQueueHeaderDetailResultV2))]
        [Route("SendSMSListV2")]
        public IHttpActionResult SMSToQueueHeaderDetailV2([FromBody] SMSQueueHeaderDetailViewModelV2 dataInfo)
        {
            return Json(SmsCentreFunction.SMSToQueueHeaderDetailV2(dataInfo), GlobalObject.carmelSetting());
        }

        /// <summary>
        /// Get SMS credit
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(List<CreditRemain>))]
        [Route("GetCredit")]
        public IHttpActionResult GetCredit(int? providerId = 2)
        {
            return Json(SmsCentreFunction.RequestCreditRemain(providerId), GlobalObject.carmelSetting());
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
            var result = SmsCentreFunction.DoGetSmsDetail(SMSId);
            if (result.Item1 == "") return Json(result.Item2, GlobalObject.carmelSetting());
            else return Json(result.Item1, GlobalObject.carmelSetting());
        }

        /// <summary>
        /// Search SMS history by phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="smsTypeId"></param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(List<Search_DetailResult>))]
        [Route("SearchByPhoneNumber")]
        public IHttpActionResult SearchSMSByPhoneNumber(string phoneNumber, int? smsTypeId)
        {
            var result = SmsCentreFunction.DoSearchByMessage(phoneNumber, "", smsTypeId);
            if (result.Item1 == "") return Json(result.Item2, GlobalObject.carmelSetting());
            else return Json(result.Item1, GlobalObject.carmelSetting());
        }

        /// <summary>
        /// search sms by criteria in message
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="smsTypeId"></param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(List<Search_DetailResult>))]
        [Route("SearchByCriteria")]
        public IHttpActionResult SearchSMSByCriteria(string criteria, int? smsTypeId)
        {
            var result = SmsCentreFunction.DoSearchByMessage("", criteria, smsTypeId);
            if (result.Item1 == "") return Json(result.Item2, GlobalObject.carmelSetting());
            else return Json(result.Item1, GlobalObject.carmelSetting());
        }

        /// <summary>
        /// Send with send date return smsID - เวอร์ชั่นเก่า
        /// </summary>
        /// <param name="sms"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(SMS_DetailResult))]
        [Route("SendSMSV2")]
        public IHttpActionResult SendSms([FromBody] SMSSingle_DetailRequest sMS_V2)
        {
            return Json(SmsCentreFunction.SendSMS(sMS_V2), GlobalObject.carmelSetting());
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SMSOTP"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(OTP_DetailResult))]
        [Route("SendOTP")]
        public IHttpActionResult SendOTP([FromBody] OTP_DetailRequest SMSOTP)
        {
            return Json(SmsCentreFunction.SendOTPRequest(SMSOTP), GlobalObject.carmelSetting());
        }


        [HttpPost]
        [ResponseType(typeof(Verify_DetailResult))]
        [Route("VerifyOTP")]
        public IHttpActionResult VerifyOTP([FromBody] VerifyOTP_DetailRequest sMSOTP_V2)
        {
            return Json(SmsCentreFunction.VerifyOTP(sMSOTP_V2), GlobalObject.carmelSetting()); 
        }


        /// <summary>
        /// Generate RefCode
        /// </summary>
        /// <returns></returns>
        //[HttpPost]
        //[ResponseType(typeof(SMS_DetailResult))]
        //[Route("GetRefCodeOTP")]
        //public IHttpActionResult GetRefCodeOTP()
        //{
        //    return Json(SmsCentreFunction.GetRefCodeOTP(), GlobalObject.carmelSetting());
        //}
        #endregion API
    }
}