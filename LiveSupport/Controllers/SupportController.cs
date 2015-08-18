using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LiveSupport.Controllers
{
    public class SupportController : Controller
    {
        // GET: Support
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles="Agent")]
        public ActionResult Agent()
        {
            return View();
        }
    }
}