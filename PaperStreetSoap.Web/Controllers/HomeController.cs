using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace PaperStreetSoap.Web.Controllers
{
    public class HomeController : Controller
    {
        private CloudTable _table;

        public HomeController()
        {
            string storageConnectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient client = storageAccount.CreateCloudTableClient();
            _table = client.GetTableReference("Settings");
        }

        public ActionResult Index()
        {
            var location = ConfigurationManager.AppSettings["Location"];
            var statusString = $"{location}_Web_StatusCode";

            var getSetting = TableOperation.Retrieve("Setting", statusString);
            DynamicTableEntity setting = (DynamicTableEntity)_table.Execute(getSetting).Result;

            var status = Convert.ToInt32(setting.Properties["Value"].StringValue);
            HttpStatusCode statusCode = (HttpStatusCode)status;
            ViewBag.StatusCode = statusCode;
            ViewBag.Location = location;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}