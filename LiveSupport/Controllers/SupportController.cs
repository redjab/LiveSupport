using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LiveSupport.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        // GET: Support
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Agent()
        {
            return View();
        }
    }
}