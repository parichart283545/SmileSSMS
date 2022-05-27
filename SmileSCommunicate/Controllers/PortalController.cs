using SmileSCommunicate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmileSCommunicate.Controllers
{
    public class PortalController : Controller
    {
        // GET: Portal
        /// <summary>
        ///
        /// </summary>
        /// <param name="code">code </param>
        /// <param name="p">Project Id</param>
        /// <returns></returns>
        [Route("portal/{code}")]
        [Route("portal/{code}/{p}")]
        public ActionResult Index(string code, int? p = null)

        {
            try
            {
                if (code != "")
                {
                    switch (p)
                    {
                        //id 13 = criticalIllness
                        case 13:
                            //return Redirect(string.Format("http://uat.siamsmile.co.th:9135/Covid19Application/doc/{0}", code));
                            return Redirect(string.Format("http://operation.siamsmile.co.th:9184/#/view/{0}", code));
                        //id 16 = billpayment
                        case 16:
                            return Redirect(string.Format("http://operation.siamsmile.co.th:9136/doc/{0}", code));
                        //id 17 = TaxAllowance
                        case 17:
                            return Redirect("http://customer.siamsmile.co.th/tax/taxcustomer/taxcustomer");

                        case 19:
                            return Redirect("http://customer.siamsmile.co.th/motorbrochure/m621.pdf");

                        default:
                            //decoding
                            var queueBase64EncodedBytes = Convert.FromBase64String(code);
                            var deCode = System.Text.Encoding.UTF8.GetString(queueBase64EncodedBytes);

                            using (var db = new CommunicateV1Entities())
                            {
                                var smsTypeId = db.usp_GetSMSTypeIdByCode_Select(deCode).SingleOrDefault();

                                switch (smsTypeId)
                                {
                                    case 12:
                                        return RedirectToAction("PaySlip", "Form", new { code = code });

                                    default:
                                        return RedirectToAction("NotFoundNull", "Error");
                                }
                                //Concept
                                //http://customer.siamsmile.co.th/service/portal/1
                                //http://customer.siamsmile.co.th/service/form/payslip?code=1
                            }
                    }
                }
                else
                {
                    return RedirectToAction("NotFoundNull", "Error");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}