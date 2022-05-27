using System.Web.Mvc;

namespace SmileSCommunicate.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult InternalServerError()
        {
            return View();
        }

        public ActionResult NotFound()
        {
            return View();
        }

        public ActionResult NotFoundNull()
        {
            return View();
        }
    }
}