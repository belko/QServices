using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ASWeb.Models
{
    public class ServiceModel
    {
        public int id{get;set;}
        public string title { get; set; }
        public string description { get; set; }
        public int? parentId { get; set; }
        public string parent { get; set; }
        public int? count { get; set; }
        public double? distance { get; set; }
        public bool isChecked { get; set; }
    }
}