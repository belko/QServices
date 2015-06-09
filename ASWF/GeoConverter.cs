using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ASWF
{
    public class GeoConverter
    {
        private string geoServiceUrl = "http://maps.google.com/maps/api/geocode/json?address=";


        public GeoResponse getByAddress(string address) 
        {
            var json = new WebClient().DownloadString(geoServiceUrl+address);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            GeoResponse obj = jss.Deserialize<GeoResponse>(json);
            return obj;
        }

        public class GeoResponse 
        {
            public GeoResult[] results;
            public string status;
        }

        public class GeoResult 
        {
            //public object address_components;
            public string formatted_address;
            public Geometry geometry;
            public string[] types;
        }

        public class Geometry 
        {
            public Location location;
            public string location_type;
            //public object viewport;
        }
        public class Location 
        {
            public float lat;
            public float lng;
        }
    }
}
