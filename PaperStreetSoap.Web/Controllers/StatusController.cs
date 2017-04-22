using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace PaperStreetSoap.Web.Controllers
{
    [RoutePrefix("api/status")]
    public class StatusController : ApiController
    {
        private CloudTable _table;

        public StatusController()
        {
            string storageConnectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient client = storageAccount.CreateCloudTableClient();
            _table = client.GetTableReference("Settings");
        }

        public IHttpActionResult Get()
        {
            var location = ConfigurationManager.AppSettings["Location"];
            var statusString = $"{location}_Web_StatusCode";

            var getSetting = TableOperation.Retrieve("Setting", statusString);
            DynamicTableEntity setting = (DynamicTableEntity)_table.Execute(getSetting).Result;

            var status = Convert.ToInt32(setting.Properties["Value"].StringValue);

            var result = $"{location}-{status}";
            HttpStatusCode statusCode = (HttpStatusCode)status;

            return Content(statusCode, result);
        }

        [Route("save")]
        [HttpPost]
        public IHttpActionResult SaveStatus()
        {
            var location = ConfigurationManager.AppSettings["Location"];
            DynamicTableEntity setting = (DynamicTableEntity)_table.Execute(TableOperation.Retrieve("Setting", "WebApiBaseUrl")).Result;
            var uri = new Uri(setting.Properties["Value"].StringValue);
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.BaseAddress = uri;
            HttpResponseMessage response = client.PostAsync($"api/status/location/{location}/Web", null).Result;  // Blocking call!
            var status = response.StatusCode;
            return Content(status, response.Content);
        }
    }
}
