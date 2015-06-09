using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASDataContext
{
    
    

    public class GrabRepository
    {
        private string connectionString = "";
        private string errorLogs;

        public string ErrorLogs { get { return errorLogs; } }

        ASDataContext context;
        public void Ensure()
        {
            if (!context.DatabaseExists())
                context.CreateDatabase();
        }

        public GrabRepository() 
        {
            context = new ASDataContext(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        public GrabRepository(string connectionString) 
        {
            context = new ASDataContext(connectionString);
        }

        public IEnumerable<GrabedCompany> Companies 
        {
            get { return context.GrabedCompanies; }
        }

        public IEnumerable<Service> Services 
        {
            get { return context.Services; }
        }

        public int AddService(Service newService) 
        {
            context.Services.InsertOnSubmit(newService);
            context.SubmitChanges();
            return newService.id;
        }
        public void UpdateService(Service update) 
        {
            var exist = context.Services.First(s => s.id == update.id);
            exist.parent = update.parent;
            exist.title = update.title;
            exist.description = update.description;
            context.SubmitChanges();
        }

        public IEnumerable<CompanyService> CompanyServices 
        {
            get { return context.CompanyServices; }
        } 

     

        

        public IEnumerable<GetServicesResult> GetAllServices(string titleFilter="",float latitude=0,float longitude=0) 
        {
            var services = context.GetServices(titleFilter, latitude, longitude);

            return services;
        }

        public IEnumerable<GetServiceCompaniesResult> GetServiceCompanies(int serviceId,float latitude=0,float longitude=0)
        {
            return context.GetServiceCompanies(serviceId,latitude,longitude);
        }

        public int AddCompany(GrabedCompany newCompany, out string exception)
        {
            exception = "";
            int companyId = -1;
            try
            {
                var existed = context.GrabedCompanies.FirstOrDefault(gc =>
                    gc.title.Equals(newCompany.title) &&
                    gc.address.Equals(newCompany.address));

                if (existed == null)
                {
                    newCompany.createTime = DateTime.Now;
                    newCompany.updateTime = DateTime.Now;
                    context.GrabedCompanies.InsertOnSubmit(newCompany);
                    context.SubmitChanges();

                }
                else
                {
                    newCompany.id = existed.id;
                    companyId = existed.id;
                }

                if (newCompany.services != null)
                {
                    List<string> services = newCompany.services.Split(';').ToList();
                    AddCompanyServices(newCompany.id, services);
                }
                if (newCompany.carBrands != null)
                {
                    List<string> brands = newCompany.carBrands.Split(';').ToList();
                    AddCompanyCarBrands(newCompany.id, brands);
                }

                return newCompany.id;
            }
            catch (Exception ex)
            {
                writeToLog("AddCompany", ex);
                exception = ex.Message;
                return -1;
            }
        }

        public void UpdateCompanyLocation(int companyId, float lat, float lng)
        {
            try
            {
                var company = context.GrabedCompanies.FirstOrDefault(c => c.id == companyId);
                if (company != null)
                {
                    company.llat = lat;
                    company.llng = lng;
                    context.SubmitChanges();
                }
            }
            catch { }
        }

        public bool AddCompanyServices(int companyId, List<string> services) 
        {
            List<Service> dbservices = new List<Service>();
            foreach (string serviceTitle in services) 
            {
                try
                {
                    var dbs = context.Services.FirstOrDefault(s => s.title.Equals(serviceTitle));
                    if (dbs == null)
                    {
                        dbs = new Service()
                        {
                            title = serviceTitle
                        };
                        context.Services.InsertOnSubmit(dbs);
                        context.SubmitChanges();
                    }
                    int serviceId = dbs.id;
                    var csdb = context.CompanyServices.FirstOrDefault(cs => cs.companyId == companyId && cs.serviceId == serviceId);
                    if (csdb == null)
                    {
                        csdb = new CompanyService()
                        {
                            companyId = companyId,
                            serviceId = serviceId
                        };
                        context.CompanyServices.InsertOnSubmit(csdb);
                        context.SubmitChanges();
                    }


                }
                catch (Exception ex) 
                {
                    writeToLog("AddCompanyServices", ex);
                }
            }
            return true;

        }
        public bool AddCompanyCarBrands(int companyId, List<string> carBrands)
        {
            List<Service> dbservices = new List<Service>();
            foreach (string brandTitle in carBrands)
            {
                try
                {
                    var dbs = context.CarBrands.FirstOrDefault(s => s.title.Equals(brandTitle));
                    if (dbs == null)
                    {
                        dbs = new CarBrand()
                        {
                            title = brandTitle
                        };
                        context.CarBrands.InsertOnSubmit(dbs);
                        context.SubmitChanges();
                    }
                    int serviceId = dbs.id;
                    var csdb = context.CompanyCarBrands.FirstOrDefault(cs => cs.companyId == companyId && cs.carBrandId == serviceId);
                    if (csdb == null)
                    {
                        csdb = new CompanyCarBrand()
                        {
                            companyId = companyId,
                            carBrandId = serviceId
                        };
                        context.CompanyCarBrands.InsertOnSubmit(csdb);
                        context.SubmitChanges();
                    }

                }
                catch (Exception ex) 
                {
                    writeToLog("AddCompanyCarBrands", ex);
                }

            }
            return true;

        }

        private void writeToLog(string method,Exception ex) 
        {
            errorLogs = string.Concat(errorLogs, "\r\n" + method, "\t",
                (ex != null ? ex.Message : ""));
        }

        /// <summary>
        /// old
        /// </summary>
        /// <param name="update"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool UpdateCompany(Company update,out string message) 
        {
            message = "";
            try
            {
                var existedCompany = context.GrabedCompanies.FirstOrDefault(gc => gc.title.Equals(update.Title));
                if (existedCompany != null)
                {
                    existedCompany.webSite = update.WebSiteUrl;
                    existedCompany.phoness = update.Phones;
                    existedCompany.address = update.Address;
                    if (update.CarModels != null)
                    {
                        foreach (var model in update.CarModels)
                        {
                            var existedModel = context.CarBrands.FirstOrDefault(cm => cm.title.Equals(model));
                            if (existedModel == null)
                            {
                                existedModel = new CarBrand() { title = model };
                                context.CarBrands.InsertOnSubmit(existedModel);
                                context.SubmitChanges();
                            }
                            if (context.CompanyCarBrands.Count(c => c.carBrandId == existedModel.id && c.companyId == existedCompany.id) == 0)
                                context.CompanyCarBrands.InsertOnSubmit(new CompanyCarBrand() { companyId = existedCompany.id, carBrandId = existedModel.id });
                        }
                    }
                    if (update.Services != null)
                    {
                        foreach (var svice in update.Services)
                        {
                            var existedSerice = context.Services.FirstOrDefault(s => s.title.Equals(svice));
                            if (existedSerice == null)
                            {
                                existedSerice = new Service()
                                {
                                    title = svice
                                };
                                context.Services.InsertOnSubmit(existedSerice);
                                context.SubmitChanges();
                            }
                            if (context.CompanyServices.Count(cs => cs.companyId == existedCompany.id && cs.serviceId == existedSerice.id) == 0)
                                context.CompanyServices.InsertOnSubmit(new CompanyService() { companyId = existedCompany.id, serviceId = existedSerice.id });


                        }
                    }
                    context.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex) 
            {
                message = ex.Message;
                return false;
            }
            return false;
        }

        /// <summary>
        /// old
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public IEnumerable<Service> GetServicesExistedOrCreate(List<string> services,int companyId) 
        {
            List<Service> dbservices = new List<Service>();
            bool needUpdate = false;
            foreach(var stitle in services)
            {
                var dbs = context.Services.FirstOrDefault(s => s.title == stitle);
                if (dbs == null) 
                {
                    context.Services.InsertOnSubmit(new Service() { title = stitle });
                    needUpdate = true;
                }
                dbservices.Add(dbs);
            }



            if (needUpdate)
                context.SubmitChanges();
            return dbservices; 
        }
    }
}
