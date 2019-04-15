using AngleSharp;
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
                    Console.WriteLine("Uncatched error! Can`t get column \"right panel\"");
                    
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
                    Console.WriteLine("Uncatched error! Can`t get column \"right panel\"");

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
                    Console.WriteLine("Uncatched error! Can`t get discription");

                foreach(var pdq in planeDiscriptionQuery)
                {
                    //for (int i = 0; i < pdq.TextContent.Length; i++)
                    //{
                    //    if (i != pdq.TextContent.Length && pdq.TextContent[i] == '"' && pdq.TextContent[i + 1] == '"')
                    //        plane.Discription += pdq.TextContent[i] + '\n';
                    //    else
                    //        plane.Discription += pdq.TextContent[i];
                    //}
                    if(planeDiscriptionQuery.Count() < 2)
                    {
                        var findBr = pdq.QuerySelectorAll("br").ToList();
                        foreach(var b in findBr)
                        {
                            var t = b.TagName;
                        }
                    }
                    //var b = pdq.TagName;
                    plane.Discription += pdq.TextContent.Trim(' ', '\t') + '\n';
                }

                Console.WriteLine("Discription: " + plane.Discription);

                //Console.WriteLine("Current query - " + planeStrQuery);

                //foreach(var l in list2)
                //{
                //    var nameH1 = l.QuerySelectorAll("h1").ToList();
                //    foreach (var h in nameH1)
                //    {
                //        plane.Name = h.TextContent;
                //        Console.WriteLine("Name: " + h.TextContent);
                //    }
                //    var price = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("vif_price")).ToList();
                //    if (price.Count != 0)
                //    {
                //        plane.Price = GetPrice(price[0].TextContent);
                //        plane.Currency = GetCurrency(price[0].TextContent);
                //        Console.WriteLine("Price: " + plane.Price + "Currency: " + plane.Currency);
                //    }
                //    //var year = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("col_63") && item.Parent.NodeName == "li" && item.ParentElement.ClassName.Contains("clearfix")).ToList();
                //    var year = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("col_63")).ToList();

                //    foreach (var y in year)
                //    {
                //        Console.WriteLine("Parent name: " + y.Parent.NodeName + "\tParent classname: " + y.ParentElement.ClassName);
                //        //Console.WriteLine("Year: " + y.TextContent);
                //    }
                //}

            }

            Console.ReadLine();
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
