using SmileSCommunicate.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using SmileSCommunicate.Models;

namespace SmileSCommunicate.Controllers
{
    public class FormController : Controller
    {
        // GET: Form
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PaySlip(string code)
        {
            if (code != "" && code != null)
            {
                using (var db = new CommunicateV1Entities())
                {
                    try
                    {
                        var base64EncodedBytes = Convert.FromBase64String(code);
                        var decode = Encoding.UTF8.GetString(base64EncodedBytes);

                        //GET PaySlipHeader  TO VIEWBAG
                        var PaySlipHeader = db.usp_SMSDetail_Select(decode, 0, 999, null, null, null).FirstOrDefault();

                        if (PaySlipHeader == null) return null;

                        //SET  TO VIEWBAG
                        ViewBag.PaySlipHeader = PaySlipHeader;
                        ViewBag.BahtText = GlobalFunction.ThaiBahtText(PaySlipHeader.d04.ToString());
                        //GET PaySlipDetail
                        var PaySlipDetail = db.usp_SMSSubDetail_Select(PaySlipHeader.Code, 0, 999, null, null, null).ToList();
                        //SET  TO VIEWBAG
                        ViewBag.PaySlipDetail = PaySlipDetail;

                        return View();
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("InternalServerError", "Error");
                    }
                }
            }
            else
            {
                return RedirectToAction("NotFound", "Error");
            }
        }
    }
}