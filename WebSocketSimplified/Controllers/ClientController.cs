using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebSocketSimplified.Server;

namespace WebSocketSimplified.Controllers
{
    public class ClientController : Controller
    {
        // GET: Client
        public ActionResult UserV()
        {
            return View();
        }
    }
}