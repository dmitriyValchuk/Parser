using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser
{
    class Program
    {
        static StreamReader GetPage(string url)
        {            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader document = new StreamReader(response.GetResponseStream());
            return document;
        }

        static IHtmlDocument GetDocument(string url)
        {
            var output = GetPage(url).ReadToEnd();
            var domParser = new HtmlParser();
            var document = domParser.ParseDocument(output);

            return document;
        }

        static int GetLastPage(IHtmlDocument document)
        {
            var lastPageQueryStr = $"body > section > div[class=\"container\"] > div[class=\"clearfix common_wrapper\"] > div[class=\"list_right_panel\"] >" +
                $" div[class=\"pagination\"] > div[id=\"paging\"] > div[class=\"pager\"] > span[class=\"paging_link\"] > a[class=\"page_num last\"]";
            var lastPageQuery = document.QuerySelector(lastPageQueryStr);

            int lastPage;
            bool isNumber = Int32.TryParse(lastPageQuery.TextContent, out lastPage);
            if (!isNumber)
                lastPage = 1;

            return lastPage;            
        }

        static List<string> GetPlaneUrlsForPages(string url, int pageStart = 1, int pageEnd = 1)
        {
            List<string> urls = new List<string>();
            for (int i = pageStart; i <= pageEnd; i++)
            {
                string currentUrl = url + "?page=" + i;

                var document = GetDocument(currentUrl);
                //var output = GetPage(currentUrl).ReadToEnd();
                //var domParser = new HtmlParser();
                //var document = domParser.ParseDocument(output);

                Console.WriteLine("Urls for page #" + i);

                var listAllAirplanes = document.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("listing")).ToList();

                foreach (var l in listAllAirplanes)
                {
                    var listAirplanesUrls = l.QuerySelectorAll($"div[class=\"list_col2\"] > div[class=\"clearfix\"] > div[class=\"share_link\"] > " +
                                                $"a[class=\"btn_more\"]");
                    foreach (var tl in listAirplanesUrls)
                    {
                        urls.Add("https://www.avbuyer.com" + tl.GetAttribute("href"));
                        Console.WriteLine("https://www.avbuyer.com" + tl.GetAttribute("href"));
                    }
                }
                //Console.WriteLine("Last page - " + GetLastPage(document));
            }
            return urls;
        }

        static IElement GetSingleData(string queryStr, IHtmlDocument document)
        {
            var query = document.QuerySelector(queryStr);

            if (query == null)
            {
                queryStr = queryStr.Replace("fa_right_panel ", "fa_right_panel new_vif");
                query = document.QuerySelector(queryStr);
            }

            if (query == null)
                ShowError("Uncatched error! Can`t get column \"right panel\"");

            return query;
        }

        static List<Plane> GetPlanesInfo(List<string> urls)
        {
            List<Plane> planes = new List<Plane>();
            foreach (var url in urls)
            {
                var document = GetDocument(url);
                Plane plane = new Plane();

                string planeNameStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_right_panel \"] > div[class=\"vif_other_info\"] > h1";

                plane.Name = GetSingleData(planeNameStrQuery, document).TextContent;
                Console.WriteLine("Plane name - " + plane.Name);

                string planePriceStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_right_panel \"] > div[class=\"vif_other_info\"] > div[class=\"vif_price\"]";

                plane.Price = GetPrice(GetSingleData(planePriceStrQuery, document).TextContent);
                plane.Currency = GetCurrency(GetSingleData(planePriceStrQuery, document).TextContent);
                Console.WriteLine("Plane price - " + plane.Price + ' ' + plane.Currency);

            }

            return planes;
        }

        static void Main(string[] args)
        {
            var url = "https://www.avbuyer.com/aircraft";
            //GetLastPage - for geting last page and method GetPlaneUrlsForPages(url, startPage, lastPage) for get all plane urls
            var urls = GetPlaneUrlsForPages(url, 1, 2);

            Console.ReadLine();

            GetPlanesInfo(urls);

            Console.ReadLine();

            Console.WriteLine("------------------------------------------------------");
            List<Plane> planes = new List<Plane>();
            
            foreach(var el in urls)
            {
                var stream = GetPage(el).ReadToEnd();
                var Parser = new HtmlParser();
                var document = Parser.ParseDocument(stream);

                Plane plane = new Plane();

                string planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_right_panel \"] > div[class=\"vif_other_info\"] > h1";

                var planeNameQuery = document.QuerySelector(planeStrQuery);

                if (planeNameQuery == null)
                {
                    planeStrQuery = planeStrQuery.Replace("fa_right_panel ", "fa_right_panel new_vif");
                    planeNameQuery = document.QuerySelector(planeStrQuery);
                }               

                if(planeNameQuery == null)
                    ShowError("Uncatched error! Can`t get column \"right panel\"");
                    
                plane.Name = planeNameQuery.TextContent;
                Console.WriteLine("Plane name - " + plane.Name);

                planeStrQuery = planeStrQuery.Replace("h1", "div[class=\"vif_price\"]");
                var planePriceQuery = document.QuerySelector(planeStrQuery);

                if (planePriceQuery == null)
                {
                    planeStrQuery = planeStrQuery.Replace("vif_price", "new_price");
                    planePriceQuery = document.QuerySelector(planeStrQuery);
                }

                if (planePriceQuery == null)
                    ShowError("Uncatched error! Can`t get column \"right panel\"");

                plane.Price = GetPrice(planePriceQuery.TextContent);
                plane.Currency = GetCurrency(planePriceQuery.TextContent);
                Console.WriteLine("Plane price - " + plane.Price + ' ' + plane.Currency);

                planeStrQuery = planeStrQuery.Remove(planeStrQuery.Length - 24, 24);
                planeStrQuery = planeStrQuery + " > ul[class=\"mp0\"] > li[class=\"clearfix\"]";

                //var mainInfo = document.QuerySelectorAll(planeStrQuery).ToList();
                //foreach(var mi in mainInfo)
                //{
                //    var liName = mi.QuerySelector("div[class=\"col_30\"]").TextContent;
                //    string value = "div[class=\"col_63\"]";

                //    if (liName == "YEAR")
                //        //Fucking symbol "‐", copy from debug window, becouse it isn`t equals "-" O_o (i think, diggerent encoding)
                //        plane.Year = mi.QuerySelector(value).TextContent.Equals("‐") ? "no information" : mi.QuerySelector(value).TextContent;
                //    if (liName == "LOCATION")
                //    {
                //        var a = mi.QuerySelector(value).TextContent;
                //        plane.Location = mi.QuerySelector(value).TextContent.Equals("‐") ? "no information" : mi.QuerySelector(value).TextContent.Trim(' ', '\n', '\t');
                //        var b = plane.Location;
                //    }
                //    if (liName == "S/N")
                //        plane.SerialNumber = mi.QuerySelector(value).TextContent.Equals("‐") ? "no information" : mi.QuerySelector(value).TextContent;
                //    if (liName == "REG")
                //        plane.Redistration = mi.QuerySelector(value).TextContent.Equals("‐") ? "no information" : mi.QuerySelector(value).TextContent;
                //    if (liName == "TTAF")
                //        plane.TotlaTimeAirFrame = mi.QuerySelector(value).TextContent.Equals("‐") ? "no information" : mi.QuerySelector(value).TextContent;
                //}

                //Console.WriteLine($"\tYear: " + plane.Year + "\n\tLocation: " + plane.Location + "\n\tS/N: " + plane.SerialNumber +
                //    "\n\tREG: " + plane.Redistration + "\n\tTTAF: " + plane.TotlaTimeAirFrame);

                planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[class=\"clearfix\"] > " +
                    $"div[class=\"product_info_col1\"] > div[class=\"disc\"] > p";

                var planeDiscriptionQuery = document.QuerySelectorAll(planeStrQuery).ToList();

                if(planeDiscriptionQuery.Count() == 0)
                {
                    planeStrQuery = planeStrQuery.Replace("fa_left_panel ", "fa_left_panel new_vif");
                    planeDiscriptionQuery = document.QuerySelectorAll(planeStrQuery).ToList();
                }

                if (planeDiscriptionQuery.Count() == 0)
                    ShowError("Uncatched error! Can`t get discription");

                foreach(var pdq in planeDiscriptionQuery)
                {
                    //for (int i = 0; i < pdq.TextContent.Length; i++)
                    //{
                    //    if (i != pdq.TextContent.Length && pdq.TextContent[i] == '"' && pdq.TextContent[i + 1] == '"')
                    //        plane.Discription += pdq.TextContent[i] + '\n';
                    //    else
                    //        plane.Discription += pdq.TextContent[i];
                    //}
                    //if(planeDiscriptionQuery.Count() < 2)
                    //{
                    //    var findBr = pdq.QuerySelectorAll("br").ToList();
                    //    foreach(var b in findBr)
                    //    {
                    //        var t = b.TagName;
                    //    }
                    //}
                    //var b = pdq.TagName;
                    plane.Discription += pdq.TextContent.Trim(' ', '\t') + '\n';
                }

                Console.WriteLine("Discription: " + plane.Discription);

                planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[id=\"accordion\"] > " +
                    $"div[class=\"panel_default\"]";

                var planeSpecificationsQuery = document.QuerySelectorAll(planeStrQuery).ToList();

                if (planeSpecificationsQuery.Count() == 0)
                {
                    planeStrQuery = planeStrQuery.Replace("fa_left_panel ", "fa_left_panel new_vif");
                    planeSpecificationsQuery = document.QuerySelectorAll(planeStrQuery).ToList();
                }

                plane.specifications = new List<Specification>();
                foreach(var psq in planeSpecificationsQuery)
                {

                    var planeSpeclTitleQuery = psq.QuerySelector("> div[class=\"panel_title\"] > h4 > a");
                    Specification specification = new Specification();                    
                    specification.Title = planeSpeclTitleQuery.TextContent;

                    var planeSpecValueQuery = psq.QuerySelector(" > div[class=\"panel_contain\"]");
                    specification.Value = planeSpecValueQuery.TextContent.Trim(' ', '\n', '\t');

                    plane.specifications.Add(specification);
                    Console.WriteLine("Specification: " + specification.Title + "\nValue: " + specification.Value);                    
                }

                planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_right_panel \"] > div[class=\"seller_info\"] > div[class=\"seller_name\"]";

                var planeSellerNameQuery = document.QuerySelector(planeStrQuery);

                if (planeSellerNameQuery == null)
                    ShowError("This plane order hasn`t seller contscts");

                planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                $"div[class=\"fa_right_panel \"] > div[class=\"contact_slide\"] > span";

                var planeSellerPhoneQuery = document.QuerySelector(planeStrQuery);

                if (planeSellerPhoneQuery == null)
                    ShowError("Can`t get seller phone");

                Seller seller = new Seller();
                seller.Name = planeSellerNameQuery.TextContent;
                seller.Phone = planeSellerPhoneQuery.TextContent;
                Console.WriteLine("Seller Name: " + seller.Name + "\tSeller phone: " + seller.Phone);

                //Getting photos
                planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                   $"div[class=\"fa_left_panel \"] > div[class=\"vif_carousel owl-theme\"] > div[class=\"owl-carousel owl-loaded owl-drag\"] > " +
                   $"div[class=\"owl-stage-outer\"] > div[class=\"owl-stage\"] > div[class=\"owl-item active\"] > div[class=\"item\"] > a";

                var fileExtensions = new string[] { ".jpg", ".png" };

                var planePhotosQuery = document.QuerySelectorAll(planeStrQuery).ToList();

                var result = from element in planePhotosQuery
                             from attribute in element.Attributes
                             where fileExtensions.Any(e => attribute.Value.EndsWith(e))
                             select attribute;

                foreach (var item in result)
                {
                    Console.WriteLine(item.Value);
                }

            }

            Console.ReadLine();
        }

        static public void ShowError(string err)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err);
            Console.ResetColor();
        }

        static public string GetCurrency(string str)
        {
            List<string> currency = new List<string>() { "USD", "AUD", '£'.ToString(), '€'.ToString() };
            string tempStr = null;
            foreach (var c in currency)
            {
                tempStr = c;
                var a = '£'.ToString();
                if (tempStr == '£'.ToString() && str.Contains(tempStr))
                {
                    tempStr = "GBP";
                    return tempStr;
                }
                else if(tempStr == '€'.ToString() && str.Contains(tempStr))
                {
                    tempStr = "EUR";
                    return tempStr;
                }
                if (str.Contains(c))
                    return c;
            }
            return null;
        }

        static public string GetPrice(string str)
        {
            var numbers = Regex.Split(str, @"\D+");
            
            string price = null;
            foreach (var n in numbers)
            {
                if(!string.IsNullOrEmpty(n))
                    price += n + ',';
            }
            if (price == null)
                return null;
            price = price.Remove(price.Length-1, 1);
            return price;
        }
    }
}
