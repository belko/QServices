using ASDataContext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASWF
{
    public class CompanyPageParser
    {
        private string pageContent;

        private GrabRepository repo = new GrabRepository();

        public List<string> scaneFolderForHtmlFiles(string folder) 
        {
            List<string> filePathes = new List<string>();
            var folders  = Directory.GetDirectories(folder);
            foreach (var f in folders) 
            {
                var files = Directory.GetFiles(f, "*.html");
                filePathes.AddRange(files);
            }
            return filePathes;
        }

        public string[] scaneFoldersForWebRequests(string folder)
        {
            return Directory.GetDirectories(folder);
            
            
        }


        List<HtmlElement> findElementByClassName(HtmlDocument doc, string tagName, string className) 
        {
            List<HtmlElement> searched = new List<HtmlElement>();
            var elements = doc.GetElementsByTagName(tagName);
            foreach (HtmlElement element in elements)
            {
                if (element.GetAttribute("class").Contains(className))
                {
                    searched.Add(element);
                }
            }
            return searched;
        }
       
        public Company getCompanyFromLocalFile(string filePath) 
        {
            string html = "";
            using (StreamReader sr = new StreamReader(filePath))
            {
                html = sr.ReadToEnd();
            }
            return parseCompany(html); ;
        }

        public Company getCompanyFromWeb(string url)
        {
            string html = "";
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Encoding = Encoding.UTF8;
                html = client.DownloadString(url);
            }
            return parseCompany(html); ;
        }


        private GrabedCompany convert(Company company)
        {
            GrabedCompany gc = new GrabedCompany()
                  {
                      title = company.Title,
                      address = company.Address,
                      phoness = company.Phones,
                      description = company.Description,
                      @checked = false,
                      webSite = company.WebSiteUrl,

                      stype = "Шиномонтаж"
                  };
            if (company.Photos != null)
            {
                gc.images = string.Join(";", company.Photos);
            }
            if (company.Services != null)
            {
                gc.services = string.Join(";", company.Services);
            }
            if (company.CarModels != null)
            {
                gc.carBrands = string.Join(";", company.CarModels);
            }
            return gc;
        }

        public Company parseCompany(string html) 
        {
            Company c = new Company();
            
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectNodes("//span[@class='fn org']");
            
            if (titleNode!=null && titleNode.Count() > 0) 
            {
                c.Title = titleNode[0].InnerText;
            }
            var ulNode = doc.DocumentNode.SelectNodes("//ul[@class='object-info service-info data']");
            if (ulNode!=null && ulNode.Count() > 0) 
            {
                foreach (var li in ulNode[0].ChildNodes) 
                {
                    if (li.Name == "#text")
                        continue;
                    var label = li.FirstChild.InnerText;
                    var childNodes = li.ChildNodes.ToList();
                    childNodes.RemoveAt(0);
                    var childs = childNodes.Where(ch=>!ch.Name.Contains("#")).Select(ch=>ch.InnerText ).ToList();

                    
                    if (label == "Адрес:")
                    {
                        c.Address = string.Join(",", childs.ToArray());
                    }
                    if (label == "Телефон:")
                    {
                        c.Phones = string.Join(",", childs.ToArray());
                    }
                    if (label == "Веб-сайт:")
                    {
                        c.WebSiteUrl = string.Join(",", childs.ToArray());
                    }
                    if (label == "Специализируется на обслуживании:")
                    {
                        c.CarModels = childs;
                    }
                    if (label == "Виды работ для всех марок:" || label == "Виды работ:")
                    {

                        var child = li.ChildNodes
                                .Where(n => !n.Name.Contains("#"))
                                .Last().ChildNodes
                                    .Where(n => !n.Name.Contains("#"));
                        c.Services = child.Select(s => s.InnerText.Replace("\n", "").Trim()).ToList();
                        
                    }
                }
            }

            var mainNode = doc.DocumentNode.SelectNodes("//div[@class='main']").FirstOrDefault();
            if (mainNode != null) 
            {
                var sub = mainNode.ChildNodes.Where(cn=>cn.Name=="div").ToList();
                if (sub.Count() > 1) 
                {
                    c.Description = sub[1].InnerHtml;
                }
            }

            var galeryNodes = doc.DocumentNode.SelectNodes("//div[@class='gallery_item']");
            if (galeryNodes != null)
            {
                List<string> photos = new List<string>();
                foreach (var gdiv in galeryNodes)
                {
                    var link = gdiv.ChildNodes.Where(cn => !cn.Name.Contains("#")).First();
                    photos.Add(link.Attributes["href"].Value);
                }
                c.Photos = photos;
            }
            return c;
        }

        public void STARTGRAB(string folder) 
        {
            
            var files = scaneFolderForHtmlFiles(folder);
            
            foreach (var filePath in files) 
            {
                var company = getCompanyFromLocalFile(filePath);


                GrabedCompany gc = convert(company);
                gc.stype = "СТО";
                string err = "";
                repo.AddCompany(gc, out err);
                
                
                
            }
        }


        public void StartGrabFromWeb() 
        {

            var folders = Directory.GetDirectories(@"C:\sites\vse-sto\vse-sto.com.ua\schinomontazhi\");
            for (int i= folders.Length-1;i>=0;i--) 
            {
                var stoUrlPart = folders[i];
                try
                {
                    string path = stoUrlPart.Substring(stoUrlPart.LastIndexOf("\\") + 1);
                    var url = "http://vse-sto.com.ua/schinomontazhi/" + path + "/";

                    var company = getCompanyFromWeb(url);


                    GrabedCompany gc = convert(company);
                    gc.detailUrl = url;
                    string err = "";
                    repo.AddCompany(gc, out err);
                }
                catch (Exception ex) 
                {

                }
            }
        }

        public class ProgressEventArgs 
        {
            public int TotlaItems;
            public int CurrentItem;
            public string message;
        }

        public class ThreadGrabber
        {
            private GrabRepository repo = new GrabRepository();

            private Thread m_Thread;

            public event EventHandler<ProgressEventArgs> Progress;

            public ThreadGrabber() 
            {
                m_Thread = new Thread(Run);
            }

            public void Start()
            {
                m_Thread.Start();
            }

            private void Run()
              {

                var folders = Directory.GetDirectories(@"C:\sites\vse-sto\vse-sto.com.ua\sto");
                for (int i=0;i<folders.Count();i++) 
                {
                    var stoUrlPart =folders[i];
                    string path = stoUrlPart.Substring(stoUrlPart.LastIndexOf("\\") + 1);
                    OnProgress(new ProgressEventArgs(){ 
                        TotlaItems = folders.Count(),
                        CurrentItem=i+1,
                        message="Receaving: "+path});
                    var company = getCompanyFromWeb("http://vse-sto.com.ua/sto/"+path+"/");


                    GrabedCompany gc = convert(company);
                    string err = "";

                    OnProgress(new ProgressEventArgs(){ 
                        TotlaItems = folders.Count(),
                        CurrentItem=i+1,
                        message="Inserting: "+gc.title});
                    repo.AddCompany(gc, out err);
                }
              }

            private void OnProgress(ProgressEventArgs args)
            {

                // Get a copy of the multicast delegate so that we can do the
                // null check and invocation safely. This works because delegates are
                // immutable. Remember to create a memory barrier so that a fresh read
                // of the delegate occurs everytime. This is done via a simple lock below.
                EventHandler<ProgressEventArgs> local;
                lock (this)
                {
                    local = Progress;
                }

                if (local != null)
                {
                    local(this, args);
                }
            }

            private GrabedCompany convert(Company company)
            {
                GrabedCompany gc = new GrabedCompany()
                {
                    title = company.Title,
                    address = company.Address,
                    phoness = company.Phones,
                    description = company.Description,
                    @checked = false,
                    webSite = company.WebSiteUrl,

                    stype = "СТО"
                };
                if (company.Photos != null)
                {
                    gc.images = string.Join(";", company.Photos);
                }
                if (company.Services != null)
                {
                    gc.services = string.Join(";", company.Services);
                }
                if (company.CarModels != null)
                {
                    gc.carBrands = string.Join(";", company.CarModels);
                }
                return gc;
            }

            public Company getCompanyFromWeb(string url)
            {
                string html = "";
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    html = client.DownloadString(url);
                }
                return parseCompany(html); ;
            }

            public Company parseCompany(string html)
            {
                Company c = new Company();

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var titleNode = doc.DocumentNode.SelectNodes("//span[@class='fn org']");

                if (titleNode != null && titleNode.Count() > 0)
                {
                    c.Title = titleNode[0].InnerText;
                }
                var ulNode = doc.DocumentNode.SelectNodes("//ul[@class='object-info service-info data']");
                if (ulNode != null && ulNode.Count() > 0)
                {
                    foreach (var li in ulNode[0].ChildNodes)
                    {
                        if (li.Name == "#text")
                            continue;
                        var label = li.FirstChild.InnerText;
                        var childNodes = li.ChildNodes.ToList();
                        childNodes.RemoveAt(0);
                        var childs = childNodes.Where(ch => !ch.Name.Contains("#")).Select(ch => ch.InnerText).ToList();


                        if (label == "Адрес:")
                        {
                            c.Address = string.Join(",", childs.ToArray());
                        }
                        if (label == "Телефон:")
                        {
                            c.Phones = string.Join(",", childs.ToArray());
                        }
                        if (label == "Веб-сайт:")
                        {
                            c.WebSiteUrl = string.Join(",", childs.ToArray());
                        }
                        if (label == "Специализируется на обслуживании:")
                        {
                            c.CarModels = childs;
                        }
                        if (label == "Виды работ для всех марок:" || label == "Виды работ:")
                        {

                            var child = li.ChildNodes
                                    .Where(n => !n.Name.Contains("#"))
                                    .Last().ChildNodes
                                        .Where(n => !n.Name.Contains("#"));
                            c.Services = child.Select(s => s.InnerText.Replace("\n", "").Trim()).ToList();

                        }
                    }
                }

                var mainNode = doc.DocumentNode.SelectNodes("//div[@class='main']").FirstOrDefault();
                if (mainNode != null)
                {
                    var sub = mainNode.ChildNodes.Where(cn => cn.Name == "div").ToList();
                    if (sub.Count() > 1)
                    {
                        c.Description = sub[1].InnerHtml;
                    }
                }

                var galeryNodes = doc.DocumentNode.SelectNodes("//div[@class='gallery_item']");
                if (galeryNodes != null)
                {
                    List<string> photos = new List<string>();
                    foreach (var gdiv in galeryNodes)
                    {
                        var link = gdiv.ChildNodes.Where(cn => !cn.Name.Contains("#")).First();
                        photos.Add(link.Attributes["href"].Value);
                    }
                    c.Photos = photos;
                }
                return c;
            }
        }
    }
}
