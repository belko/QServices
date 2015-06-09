using ASDataContext;
using ASWeb.Extension;
using ASWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ASWeb.Extension;

namespace ASWeb.Controllers
{
    public class ServicesController : Controller
    {

        private GrabRepository repo = new GrabRepository();

        

        public ActionResult Index(string filter="",float latitude=0,float longitude=0,string sort="")
        {
            
            GEOPos g = this.GetUserPos();

            var services = repo.GetAllServices(filter, g.Latitude, g.Longitude);
            var model = services.Select(
                s => new ServiceModel()
                {
                    id = s.id,
                    title = s.title,
                    count = s.count,
                    distance = s.distance
                });
            if (sort == "count") 
            {
                model = model.OrderByDescending(m => m.count);
            }
            if (sort == "dist")
            {
                model = model.Where(m=>m.distance!=null).OrderBy(m => m.distance);
            }
            ViewBag.Filter = filter;
            ViewBag.Sort = sort;
            return View(model);
        }

        public ActionResult Item(int id, float latitude = 0, float longitude = 0, string sort = "") 
        {
            if (Request.Cookies["Latitude"] != null && Request.Cookies["Longitude"] != null) 
            {

                latitude = float.Parse(Request.Cookies["Latitude"].Value.Replace(".",","));
                longitude = float.Parse(Request.Cookies["Longitude"].Value.Replace(".", ","));
            }
            var Service = repo.Services.First(s => s.id == id);
            
            var services = repo.GetServiceCompanies(id, latitude, longitude);
            var model = services;
            
            if (sort == "dist")
            {
                model = model.Where(m => m.distance != null).OrderBy(m => m.distance);
            }
            ViewBag.Service = Service;
            ViewBag.ServiceId = id;
            ViewBag.Sort = sort;
            return View(model);
        }



    }
}
