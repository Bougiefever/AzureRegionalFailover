using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PaperStreetSoap.Web.Models;

namespace PaperStreetSoap.Web.Controllers
{
    public class ProductController : Controller
    {
        private CloudTable _table;

        // GET: Product
        public ProductController()
        {
            string storageConnectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient client = storageAccount.CreateCloudTableClient();
            _table = client.GetTableReference("Settings");
        }

        public ActionResult Index()
        {
            DynamicTableEntity setting = (DynamicTableEntity)_table.Execute(TableOperation.Retrieve("Setting", "DocumentDbUri")).Result;
            var uri = setting.Properties["Value"].StringValue;
            setting = (DynamicTableEntity)_table.Execute(TableOperation.Retrieve("Setting", "DocumentDbKey")).Result;
            var key = setting.Properties["Value"].StringValue;

            var noImageUrl = ConfigurationManager.AppSettings["noImageUrl"];
            DocumentClient client = new DocumentClient(new Uri(uri), key);
            Database database = client.CreateDatabaseQuery().Where(d => d.Id == "Product").AsEnumerable().First();

            DocumentCollection productCollection = client.CreateDocumentCollectionQuery(database.SelfLink).
                Where(cl => cl.Id.Equals("ProductList")).AsEnumerable().First();

            var productDocuments = client.CreateDocumentQuery<SoapDocument>(productCollection.DocumentsLink);
            var soaps = productDocuments.ToArray();

            setting = (DynamicTableEntity)_table.Execute(TableOperation.Retrieve("Setting", "ProductImageBaseUrl")).Result;
            var imageBaseUrl = setting.Properties["Value"].StringValue;

            foreach (var soap in soaps)
            {
                if (soap.imageUrl == null)
                    soap.imageUrl = noImageUrl;
                else
                    soap.imageUrl = $"{imageBaseUrl}/{soap.imageUrl}";
            }

            var location = ConfigurationManager.AppSettings["Location"];
            var statusString = $"{location}_Web_StatusCode";
            var getSetting = TableOperation.Retrieve("Setting", statusString);
            DynamicTableEntity mysetting = (DynamicTableEntity)_table.Execute(getSetting).Result;

            var status = Convert.ToInt32(mysetting.Properties["Value"].StringValue);
            HttpStatusCode statusCode = (HttpStatusCode)status;
            ViewBag.StatusCode = statusCode;
            ViewBag.Location = location;

            return View(soaps);
        }
    }
}