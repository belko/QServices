using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ASWeb.Extension
{
    public class GEOPos 
    {
        public float Latitude;
        public float Longitude;
    }
    public static class GEOHelper
    {

        public static GEOPos GetUserPos(this Controller contr)
        {
            GEOPos g = new GEOPos();
            if (contr.Request.Cookies["Latitude"] != null && contr.Request.Cookies["Longitude"] != null)
            {

                g.Latitude = float.Parse(contr.Request.Cookies["Latitude"].Value.Replace(".", ","));
                g.Longitude = float.Parse(contr.Request.Cookies["Longitude"].Value.Replace(".", ","));
            }
            return g;
        }


    }
}