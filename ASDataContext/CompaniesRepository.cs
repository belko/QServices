using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASDataContext
{
    
    public class CompaniesRepository
    {
        ASDataContext context;

        public void Ensure() 
        {
            if (!context.DatabaseExists())
                context.CreateDatabase();
        }
        public CompaniesRepository() 
        {
            context = new ASDataContext(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        public CompaniesRepository(string connectionString) 
        {
            context = new ASDataContext(connectionString);
        }

        public IEnumerable<Company> Companies 
        {
            get 
            {
                var companies = from company in context.GrabedCompanies
                                select new Company()
                                {
                                    Id = company.id,
                                    Title = company.title,
                                    Address = company.address,
                                    Phones = company.phoness,
                                    WebSiteUrl = company.webSite
                                };
                return companies;
            }
        }

        public IEnumerable<Company> GetUserCompanies(int userId) 
        {
            return Companies;
        }

    }
}
