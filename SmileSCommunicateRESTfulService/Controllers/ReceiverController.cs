using System;
using System.Collections.Generic;
using SmileSCommunicateRESTfulService.Models;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmileSCommunicateRESTfulService.Helper;
using SmileSCommunicateRESTfulService.BLL;
using EntityFramework.Utilities;
using System.Web.Http.Description;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using SmileSCommunicateRESTfulService.Class_Lib;
using static SmileSCommunicateRESTfulService.Models.Model;

namespace SmileSCommunicateRESTfulService.Controllers
{
    //[APIAuthorization]
    [RoutePrefix("api/receiversms/v2")]
    public class ReceiverController : ApiController
    {
        // GET: Receiver

        #region Context

        //
        private readonly CommunicateV1DBContext _context;

        public ReceiverController()
        {
            _context = new CommunicateV1DBContext();
        }

        protected virtual void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        #endregion Context

        #region "API"

        [HttpPost]
        [Route("ReceiverResultSMS")]
        public IHttpActionResult updateTransaction([FromBody] sms_dr document)
        {
            var stResult = new StdResult();

            //Check Login
            var userName = "shinee.Dev";
            var passWord = "KnjiYgrtcgbIJ7";
            var source = "vendor.SMS.shinee";

            if (document.username != userName)
            {
                stResult.MSG = "Username Not Valid";
                return Ok(stResult);
            }

            if (document.password != passWord)
            {
                stResult.MSG = "Password Not Valid";
                return Ok(stResult);
            }

            if (document.source != source)
            {
                stResult.MSG = "Source Not Valid";
                return Ok(stResult);
            }

            var isResult = ReceiverResultSMS(document);

            return Ok(stResult);
        }

        #endregion "API"

        #region "Function"

        private bool ReceiverResultSMS(sms_dr smsdr)
        {
            bool isResult = false;
            string xmlResult = "";
            string status_Success = "0";
            string status_Fail = "0";
            DateTime? SendingStatusUpdateDate = null;

            //Get TransactionHeader From ReferenceId
            var objTransactionHeader = _context.TransactionHeaders.Where(x => x.ReferenceId == smsdr.transid).Single();

            //Get Transaction From TransactionHeaderId
            var objTransaction = _context.Transactions.Where(x => x.TransactionHeaderId == objTransactionHeader.TransactionHeaderId).Single();

            //Convert object to xml
            xmlResult = Serialize(smsdr);

            //Check Status
            if (smsdr.status == "OK")
            {
                status_Success = "1";
            }

            if (smsdr.status == "ERR")
            {
                status_Fail = "1";
            }

            //Convert SendTime
            if (smsdr.sendtime != null)
            {
                //Convert String to Datetime
                SendingStatusUpdateDate = DateTime.ParseExact(smsdr.sendtime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }

            //Update To Transaction (GetDeliverry,ReferanceId,SendingStatusUpdateDate,Success,Faill,XMLResult)
            objTransaction.ReferenceID = objTransactionHeader.ReferenceId;
            objTransaction.GetDeliveryNotify = true;
            objTransaction.Success = status_Success;
            objTransaction.Fail = status_Fail;
            objTransaction.XMLResult = xmlResult;

            if (SendingStatusUpdateDate != null)
            {
                objTransaction.SendingStatusUpdateDate = SendingStatusUpdateDate;
            }

            _context.SaveChanges();
            isResult = true;

            return isResult;
        }

        public static string Serialize<T>(T dataToSerialize)
        {
            try
            {
                var stringwriter = new System.IO.StringWriter();
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
            catch
            {
                throw;
            }
        }

        #endregion "Function"
    }
}