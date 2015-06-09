using ASDataContext;
using ASWF.MOdels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ASWF
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        GrabRepository repository = new GrabRepository();
        WebBrowser wb = new WebBrowser();
        WebBrowser wbDetail = new WebBrowser();
        int pagesCount = 98;
        int currentPage = 1;
        string url = "";
        private void button1_Click(object sender, EventArgs e)
        {
            initDB();
            url = textBox1.Text;

            wb.DocumentCompleted += wb_DocumentCompleted;
            wb.ScriptErrorsSuppressed = true;
            wb.Navigate(url);

            setProgress(98);

        }

        private void initDB() 
        {
            try
            {
                if (repository == null)
                    repository = new GrabRepository(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);

            }
        }

        void wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlDocument doc = wb.Document;

            var objects = ElementsByClass(doc, "catalog-list-item");
            List<Company> companies = new List<Company>();
            foreach (HtmlElement o in objects) 
            {
                Company nc = new Company();
                var h2 = o.GetElementsByTagName("h2");
                if (h2.Count > 0)
                {
                    HtmlElementCollection acollection = h2[0].GetElementsByTagName("a");
                    if (acollection.Count > 0)
                    {
                        nc.LinkToDetail = acollection[0].GetAttribute("href");
                        nc.Title = acollection[0].InnerText;
                    }
                }
                var ul = o.GetElementsByTagName("ul");
                if (ul.Count > 0)
                {
                    var liCollection = ul[0].GetElementsByTagName("li");
                    if (liCollection.Count > 1) 
                    {
                        nc.Address = liCollection[1].InnerText;
                    }
                }
                companies.Add(nc);
            }
            string message="";
            foreach (var c in companies) 
            {
                if (repository.AddCompany(new GrabedCompany()
                     {
                         title = c.Title,
                         address = c.Address,
                         detailUrl = c.LinkToDetail
                     }, out message)==-1) 
                {
                    consoleLog(message);
                }
            
            }

            if (currentPage <= pagesCount) 
            {
                currentPage++;
                string newUrl = string.Format("{0}?page={1}",url,currentPage);
                moveProgress(newUrl);
                wb.Navigate(newUrl);
                
            }

        }



        
        static IEnumerable<HtmlElement> ElementsByClass(HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
            {
                if (e.GetAttribute("className").Contains(className))
                    yield return e;
            }
        }

        private void setProgress(int maxValue) 
        {
            progressBar.Value = 0;
            progressBar.Maximum = maxValue;
            rtbConsole.Text = "";
        }
        private void moveProgress(string message = null) 
        {
            progressBar.Value+=1;
            progressBar.Refresh();
            if (message != null)
            {
                consoleLog(message);
            }

        }
        private void consoleLog(string message)
        {
            rtbConsole.Text = string.Format("{0}\r\n{1}", rtbConsole.Text, message);
            rtbConsole.SelectionStart = rtbConsole.Text.Length;
            rtbConsole.ScrollToCaret();
            rtbConsole.Refresh();
        }

        int totalCount = 0;
        int currentCompanyPos = 0;
        List<GrabedCompany> notDetailedCompanies;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            initDB();
            wbDetail.DocumentCompleted += wbDetail_DocumentCompleted;
            wbDetail.ScriptErrorsSuppressed = true;
            notDetailedCompanies = repository.Companies.Where(c => c.phoness == null).ToList();
            totalCount = notDetailedCompanies.Count();
            tryGetNextDetail();
            
        }

        private void tryGetNextDetail() 
        {
            

            if (currentCompanyPos < totalCount) 
            {

                var company = notDetailedCompanies.Skip(currentCompanyPos).First();

                if (company.detailUrl != null) 
                {
                    consoleLog(string.Format("{0}:{1} {2}", company.title, company.id, company.detailUrl));
                    wbDetail.Navigate(company.detailUrl);
                }
                currentCompanyPos++;
            }

            
        }

        void wbDetail_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlDocument doc = wbDetail.Document;
            
            Company gc = new Company();

            IEnumerable<HtmlElement> titles = ElementsByClass(doc, "fn org");
            if (titles.Count() > 0) 
            {
                gc.Title = titles.First().InnerText;
            }

            var details = ElementsByClass(doc, "object-info");

            if (details.Count() > 0) 
            {
                var detail = details.First();
                foreach (HtmlElement li in detail.GetElementsByTagName("li")) 
                {
                    HtmlElementCollection strong = li.GetElementsByTagName("strong");
                    if (strong.Count > 0) 
                    {

                        string label = strong[0].InnerText;
                        List<string> values = new List<string>();

                        var spans = li.GetElementsByTagName("span");
                        foreach (HtmlElement el in spans) 
                        {
                            values.Add(el.InnerText);
                        }

                        var links = li.GetElementsByTagName("a");
                        foreach (HtmlElement el in links)
                        {
                            values.Add(el.InnerText);
                        }


                        if (label == "Адрес:") 
                        {
                            gc.Address = string.Join(",", values);
                        }
                        if (label == "Телефон:")
                        {
                            gc.Phones = string.Join(",", values);
                        }
                        if (label == "Веб-сайт:")
                        {
                            gc.WebSiteUrl = string.Join(",", values);
                        }
                        if (label == "Специализируется на обслуживании:")
                        {
                            gc.CarModels = values;
                        }
                        if (label == "Виды работ для всех марок:")
                        {
                            gc.Services = values;
                        }
                        
                        
                    }
                }
            }
            string message="";
            if (!repository.UpdateCompany(gc, out message))
            {
                consoleLog(string.Format("{0} of {1}: {2} Error! \r\n\t{3}", currentCompanyPos,totalCount, gc.Title,message));
            }
            else
            {
                consoleLog(string.Format("{0} of {1}: {2}  updated", currentCompanyPos,totalCount, gc.Title));
            }
            tryGetNextDetail();
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string folder = textBox2.Text;
            CompanyPageParser pp = new CompanyPageParser();
            Company c = pp.getCompanyFromLocalFile(folder);
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CompanyPageParser pp = new CompanyPageParser();
            var res = pp.scaneFolderForHtmlFiles(textBox3.Text);

            rtbConsole.Clear();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < res.Count(); i++) 
            {
                sb.AppendFormat("{0} - {1}\r\n",i,res[i]);
            }
            rtbConsole.Text = sb.ToString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //PageParser pp = new PageParser();
            //pp.dataBase();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            CompanyPageParser pp = new CompanyPageParser();
            pp.STARTGRAB(textBox3.Text);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            CompanyPageParser pp = new CompanyPageParser();
            pp.StartGrabFromWeb();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ASWF.CompanyPageParser.ThreadGrabber tg = new CompanyPageParser.ThreadGrabber();
            tg.Progress += tg_Progress;
            tg.Start();
        }

        void tg_Progress(object sender, CompanyPageParser.ProgressEventArgs e)
        {
            try
            {
                progressBar.Maximum = e.TotlaItems;
                progressBar.Value = e.CurrentItem;
                
                //rtbConsole.Text = e.message + "\r\n" + rtbConsole.Text;

                string s = "";
            }catch(Exception ex)
            {
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            repository = new GrabRepository();
            var companies = repository.Companies.Select(c => new { id=c.id,llat=c.llat,llng=c.llng,address=c.address}).ToList();
            GeoConverter gep = new GeoConverter();
            for (int i=companies.Count()-1;i>=0;i--) 
            {
                try
                {
                    var c = companies[i];
                    if (c.llat == null && c.llng == null)
                    {
                        GeoConverter.GeoResponse resp = gep.getByAddress(c.address);
                        if (resp.status == "OK" && resp.results.Length > 0)
                        {
                            repository.UpdateCompanyLocation(c.id, resp.results[0].geometry.location.lat, resp.results[0].geometry.location.lng);
                        }
                    }
                }
                catch (Exception ex) 
                {

                }
            }
        }

        public class ComboboxItem
        {
            public string Text { get; set; }
            public int Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private GrabedCompany currentCompany;
        private int currentIdx = -1;
        private int[] companyIds = null;


        private void bCSNext_Click(object sender, EventArgs e)
        {
            
            if (companyIds==null) 
            {
                companyIds = repository.Companies.Select(gc => gc.id).ToArray();
                
                var services = repository.Services.Select(s => new ComboboxItem { Text=s.title,Value=s.id }).ToArray();
                for(int i=0; i<services.Length;i++)
                {
                    clbServices.Items.Add(services[i]);
                }
            }

            currentIdx++;
            if (currentIdx < companyIds.Length) 
            {
                currentCompany = repository.Companies.Where(c => c.id == companyIds[currentIdx]).FirstOrDefault();

                if (currentCompany != null)
                {
                    webBrowser1.DocumentText = currentCompany.description;
                    label1.Text = currentCompany.title;
                    var css = repository.CompanyServices.Where(cs => cs.companyId == currentCompany.id);
                    //for (int j = 0; j < clbServices.Items.Count; j++)
                    //{
                    //    var s = (ComboboxItem)clbServices.Items[j];
                    //    if (css.Count(cs => cs.serviceId == s.Value) > 0)
                    //    {

                    //    }
                    //}
                }


            }
            


            
        }


    }

    
}
