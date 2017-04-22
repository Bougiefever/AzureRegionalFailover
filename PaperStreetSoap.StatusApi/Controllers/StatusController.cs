using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PaperStreetSoap.StatusApi.Data;
using StackExchange.Redis;

namespace PaperStreetSoap.StatusApi.Controllers
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

        [HttpGet]
        public IHttpActionResult Get()
        {
            var location = ConfigurationManager.AppSettings["Location"];
            var statusString = $"{location}_WebApi_StatusCode";

            var getSetting = TableOperation.Retrieve("Setting", statusString);
            DynamicTableEntity setting = (DynamicTableEntity) _table.Execute(getSetting).Result;

            var status = Convert.ToInt32(setting.Properties["Value"].StringValue);

            var result = $"{location}-{status}";
            HttpStatusCode statusCode = (HttpStatusCode) status;

            return Content(statusCode, result);
        }

        [Route("location/{location}/{application}")]
        [HttpPost]
        public IHttpActionResult PostWithLocation([FromUri] string location, string application)
        {
            var key = $"{location}_{application}_StatusCode";
            DynamicTableEntity setting = (DynamicTableEntity) _table.Execute(TableOperation.Retrieve("Setting", key)).Result;
            var status = Convert.ToInt32(setting.Properties["Value"].StringValue);

            setting = (DynamicTableEntity) _table.Execute(TableOperation.Retrieve("Setting", "SqlServer_Connection")).Result;
            var dbConnectionString = setting.Properties["Value"].StringValue;
            var db = new StatusLogDataContext(dbConnectionString);
            var log = new StatusLog
            {
                Location = location,
                Application = application,
                Time = DateTime.Now,
                Status = status
            };
            try
            {
                db.StatusLogs.InsertOnSubmit(log);
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult Post()
        {
            var location = ConfigurationManager.AppSettings["Location"];
            var key = $"{location}_WebApi_StatusCode";
            DynamicTableEntity setting = (DynamicTableEntity) _table.Execute(TableOperation.Retrieve("Setting", key)).Result;
            var status = Convert.ToInt32(setting.Properties["Value"].StringValue);

            setting = (DynamicTableEntity) _table.Execute(TableOperation.Retrieve("Setting", "SqlServer_Connection")).Result;
            var dbConnectionString = setting.Properties["Value"].StringValue;
            var db = new StatusLogDataContext(dbConnectionString);
            var log = new StatusLog
            {
                Location = location,
                Application = "WebApi",
                Time = DateTime.Now,
                Status = status
            };
            try
            {
                db.StatusLogs.InsertOnSubmit(log);
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            return Ok();
        }

        [HttpGet]
        [Route("db")]
        public IHttpActionResult DatabaseStatus()
        {
            var setting = (DynamicTableEntity) _table.Execute(TableOperation.Retrieve("Setting", "SqlServer_Connection")).Result;
            var dbConnectionString = setting.Properties["Value"].StringValue;
            try
            {
                var db = new StatusLogDataContext(dbConnectionString);
                var logCount = db.StatusLogs.Count();
                return Ok($"RecordCount={logCount}");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [HttpGet]
        [Route("db/list")]
        public IHttpActionResult StatusList()
        {
            var setting = (DynamicTableEntity) _table.Execute(TableOperation.Retrieve("Setting", "SqlServer_Connection")).Result;
            var dbConnectionString = setting.Properties["Value"].StringValue;
            IList<StatusLog> logs;
            try
            {
                var db = new StatusLogDataContext(dbConnectionString);
                logs = db.StatusLogs.OrderByDescending(l => l.Time).Take(10).ToList();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            return Ok(logs);
        }

        [Route("DbWrite")]
        [HttpGet]
        public IHttpActionResult DbWriteTest()
        {
            var setting = (DynamicTableEntity)_table.Execute(TableOperation.Retrieve("Setting", "SqlServer_Connection")).Result;
            var dbConnectionString = setting.Properties["Value"].StringValue;
            var db = new StatusLogDataContext(dbConnectionString);
            var log = new StatusLog
            {
                Location = "Testing",
                Application = "Testing",
                Time = DateTime.Now,
                Status = 1
            };
            try
            {
                db.StatusLogs.InsertOnSubmit(log);
                db.SubmitChanges();
                var id = log.Id;
                db.StatusLogs.DeleteOnSubmit(log);
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            return Ok();
        }
    }
}
