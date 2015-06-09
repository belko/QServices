using ASDataContext;
using ASWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;

namespace ASWeb.Controllers
{
    [Authorize]
    public class MyController : Controller
    {
        GrabRepository repository;

        public MyController() 
        {
            repository = new GrabRepository();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Companies() 
        {
            return View();
        }

        public ActionResult Company(int id) 
        {
            var company = repository.Companies.First(c => c.id == id);


            ViewBag.Services = (from service in repository.Services
                           join companyServices in repository.CompanyServices on service.id equals companyServices.serviceId into csNull
                           from cs in csNull.DefaultIfEmpty()
                           where cs.companyId == id
                           select new ServiceModel()
                           {
                               id = service.id,
                               title = service.title,
                               isChecked = cs != null,
                               parentId = service.parent
                           }).ToList();
            
            return View(company);
        }

    }
}
