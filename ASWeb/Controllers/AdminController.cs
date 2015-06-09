using ASDataContext;
using ASWeb.Extension;
using ASWeb.Filters;
using ASWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace ASWeb.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AdminController : Controller
    {
        GrabRepository repo = new GrabRepository();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Companies(int page=1,string filter="")
        {
            int pageSize = 100;
            int totalItems = 0;
            int totalPages = 0;
            ViewBag.Filter = filter;
            filter = filter.ToLower();
            List<GrabedCompany> companies=null;
            if (User.IsInRole(Permitions.RoleNameAdmins))
            {
                var filtred = repo.Companies;
                if (filter != "")
                    filtred = filtred.Where(c => 
                        (c.title!=null && c.title.ToLower().Contains(filter))
                        || (c.address != null && c.address.ToLower().Contains(filter))
                        || (c.phoness != null && c.phoness.ToLower().Contains(filter)));
                totalItems = filtred.Count();
                totalPages = totalItems / pageSize;
                if (totalItems > totalPages * pageSize)
                    totalPages++;
                var cdisplay = filtred.Skip((page - 1) * pageSize).Take(pageSize);
                companies = cdisplay.ToList();
            }
            else 
            {

            }
            if (companies == null)
                companies = new List<GrabedCompany>();
            
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            return View(companies);
        }

        public ActionResult Services(int page = 1, string filter = "",string sort="")
        {
            int pageSize = 100;
            int totalItems = 0;
            int totalPages = 0;
            ViewBag.Filter = filter;
            ViewBag.Sorting = sort;
            filter = filter.ToLower();
            List<ServiceModel> services = null;
            if (User.IsInRole(Permitions.RoleNameAdmins))
            {
                var filtred = from svc in repo.Services
                           join s in repo.Services on svc.parent equals s.id into parents
                           from p in parents.DefaultIfEmpty()
                           select new ServiceModel()
                           {
                               id = svc.id,
                               title = svc.title,
                               description = svc.description,
                               parent = p!=null?p.title:""
                           };

                if (filter != "")
                    filtred = filtred.Where(c =>
                        (c.title != null && c.title.ToLower().Contains(filter)));

                switch (sort)
                {
                    case "":
                        filtred = filtred.OrderBy(s => s.title);
                        break;
                    case "parent":
                        filtred = filtred.OrderBy(s => s.parent).ThenBy(s=>s.title);
                        break;
                }
                
                totalItems = filtred.Count();
                totalPages = totalItems / pageSize;
                if (totalItems > totalPages * pageSize)
                    totalPages++;
                var cdisplay = filtred.Skip((page - 1) * pageSize).Take(pageSize);

                services = cdisplay.ToList();
            }
            else
            {

            }
            if (services == null)
                services = new List<ServiceModel>();

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            return View(services);
        }

        public ActionResult ServiceEdit(int id = 0, string returnUrl="") 
        {
            ViewBag.Parents = repo.Services.OrderBy(s => s.title)
                                    .Select(s => new { id = s.id, title = s.title })
                                    .Where(s => s.id != id);
            ViewBag.ReturnUrl = returnUrl;
            if (id != 0)
            {
                var service = repo.Services.First(s => s.id == id);
                
                return View(service);
            }
            else 
            {
                return View(new Service());
            }
        }
        [HttpPost]
        public ActionResult ServiceEdit(Service service,string returnUrl)
        {
            if (service.id == 0)
            {
                int id = repo.AddService(service);
            }
            else 
            {
                repo.UpdateService(service);
            }
            if (returnUrl != "")
                return Redirect(returnUrl);
            else
                return RedirectToAction("Services");
        }
    }
}
