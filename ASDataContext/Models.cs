using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASDataContext
{
    public class Company
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string LinkToDetail { get; set; }
        public string Phones { get; set; }
        public string WebSiteUrl { get; set; }
        public List<string> CarModels { get; set; }
        public List<string> Services { get; set; }
        public List<string> Photos { get; set; }
    }
}
