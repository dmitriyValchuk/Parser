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

        static void Main(string[] args)
        {

            var url = "https://www.avbuyer.com/aircraft";
            var output = GetPage(url).ReadToEnd();
            var domParser = new HtmlParser();
            var newDocument = domParser.ParseDocument(output);

            var listAllAirplanes = newDocument.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("listing")).ToList();
            List<string> urls = new List<string>();

            var listTest = newDocument.QuerySelector("body > div[class=\"cookies_content clearfix cookie_tab hide\"] > div[class=\"container\"] > div[class=\"fl cookies_text\"]");
            Console.WriteLine(listTest.TextContent);

            foreach (var l in listAllAirplanes)
            {
                var listAirplanesUrls = l.QuerySelectorAll($"div[class=\"list_col2\"] > div[class=\"clearfix\"] > div[class=\"share_link\"] > " +
                                            $"a[class=\"btn_more\"]");
                foreach(var tl in listAirplanesUrls)
                {
                    urls.Add("https://www.avbuyer.com" + tl.GetAttribute("href"));
                    Console.WriteLine(tl.GetAttribute("href"));
                }
            }

            Console.WriteLine("------------------------------------------------------");
            List<Plane> planes = new List<Plane>();
            
            foreach(var el in urls)
            {
                var stream = GetPage(el).ReadToEnd();
                //var config = 
                var Parser = new HtmlParser();
                var document = Parser.ParseDocument(stream);

                var list2 = document.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("clearfix vif_wrapper")).ToList();

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

                //planeStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                //    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[id=\"accordion\"] > " +
                //    $"div[class=\"panel_default\"] > div[class=\"panel_title\"] > h4 > a";

                //var planeSpecificationsQuery = document.QuerySelectorAll(planeStrQuery).ToList();

                //if (planeSpecificationsQuery.Count() == 0)
                //{
                //    planeStrQuery = planeStrQuery.Replace("fa_left_panel ", "fa_left_panel new_vif");
                //    planeSpecificationsQuery = document.QuerySelectorAll(planeStrQuery).ToList();
                //}

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

                    //specification.specificationSeeds = new List<SpecificationSeed>();
                    //foreach(var pstavq in planeSpecTitleAndValueQuery)
                    //{
                    //    SpecificationSeed specificationSeed = new SpecificationSeed();
                    //    //var planeSpecTitleQuery = pstavq.QuerySelector("h3");
                    //    //specificationSeed.Title = planeSpecTitleQuery.TextContent;
                    //    specificationSeed.Value = pstavq.TextContent;                        

                    //    specification.specificationSeeds.Add(specificationSeed);
                    //    Console.WriteLine("\tSpecification Seed: " + specificationSeed.Value);
                    //    //Console.WriteLine("Specification Seed Title: " + specificationSeed.Title + "\tValue: " + specificationSeed.Value);
                    //}

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
