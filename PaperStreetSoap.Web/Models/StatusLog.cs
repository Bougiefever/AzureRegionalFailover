using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaperStreetSoap.Web.Models
{
    public class StatusLog
    {
        public int id { get; set; }

        public DateTime time { get; set; }

        public string application { get; set; }

        public string location { get; set; }

        public int status { get; set; }
    }
}