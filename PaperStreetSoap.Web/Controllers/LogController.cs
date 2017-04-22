using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PaperStreetSoap.Web.Models;

namespace PaperStreetSoap.Web.Controllers
{
    public class LogController : Controller
    {
        private CloudTable _table;

        // GET: Log
        public LogController()
        {
            string storageConnectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient client = storageAccount.CreateCloudTableClient();
            _table = client.GetTableReference("Settings");
        }

        public ActionResult Index()
        {
            DynamicTableEntity setting = (DynamicTableEntity)_table.Execute(TableOperation.Retrieve("Setting", "WebApiBaseUrl")).Result;
            var uri = new Uri(setting.Properties["Value"].StringValue);
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.BaseAddress = uri;
            HttpResponseMessage response = client.GetAsync("api/status/db/list").Result;  // Blocking call!
            StatusLog[] logs;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                logs = response.Content.ReadAsAsync<IEnumerable<StatusLog>>().Result.ToArray();
                
            }
            else
            {
                throw new Exception("failed");
            }
            return View(logs);
        }


    }
}