using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebSocketSimplified.Server;

namespace WebServer.Controllers
{
    public class ServerController : Controller
    {
        private static WebSocketServer ws = null;

        // GET: Client
        public ActionResult Admin()
        {
            return View();
        }

        public ActionResult SendMessageToClient()
        {
            if (ws == null)
            {
                ws = new WebSocketServer();
                ws.StartWebSocketServer("127.0.0.1", 8181);
            }
            ws.SendMessageToClient("This is a Message, sent to the Client from the Server");
            return View("Admin");
        }
    }
}