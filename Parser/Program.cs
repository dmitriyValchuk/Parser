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
using System.Text.RegularExpressions;

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
                ShowError("Uncatched error! Can`t get data");

            return query;
        }

        static List<IElement> GetMultiData(string queryStr, IHtmlDocument document)
        {
            var query = document.QuerySelectorAll(queryStr).ToList();

            if (query.Count() == 0)
            {
                queryStr = queryStr.Replace("fa_left_panel ", "fa_left_panel new_vif");
                query = document.QuerySelectorAll(queryStr).ToList();
            }

            if (query.Count() == 0)
                ShowError("Uncatched error! Can`t get data");

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

                var planeDiscriptionStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[class=\"clearfix\"] > " +
                    $"div[class=\"product_info_col1\"] > div[class=\"disc\"] > p";

                var discriptionList = GetMultiData(planeDiscriptionStrQuery, document);

                foreach (var dl in discriptionList)
                    plane.Discription += dl.TextContent.Trim(' ', '\t') + '\n';
                Console.WriteLine("Discription: " + plane.Discription);

                var planeSpecificationStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[id=\"accordion\"] > " +
                    $"div[class=\"panel_default\"]";

                var specificationsList = GetMultiData(planeSpecificationStrQuery, document);

                plane.specifications = new List<Specification>();
                foreach (var sl in specificationsList)
                {
                    Specification specification = new Specification();
                    var planeSpcificationTitleStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[id=\"accordion\"] > " +
                    $"div[class=\"panel_default\"] > div[class=\"panel_title\"] > h4 > a";
                  
                    specification.Title = GetSingleData(planeSpcificationTitleStrQuery, document).TextContent;

                    var planeSpecificationValueStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[id=\"accordion\"] > " +
                    $"div[class=\"panel_default\"] > div[class=\"panel_contain\"]";

                    specification.Value = GetSingleData(planeSpecificationValueStrQuery, document).TextContent.Trim(' ', '\n', '\t');

                    plane.specifications.Add(specification);
                    Console.WriteLine("Specification: " + specification.Title + "\nValue: " + specification.Value);
                }

                Seller seller = new Seller();
                
                var planeSellerNameStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                   $"div[class=\"fa_right_panel \"] > div[class=\"seller_info\"] > div[class=\"seller_name\"]";

                seller.Name = GetSingleData(planeSellerNameStrQuery, document).TextContent;

                var planeSellerPhoneStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                $"div[class=\"fa_right_panel \"] > div[class=\"contact_slide\"] > span";

                seller.Phone = GetSingleData(planeSellerPhoneStrQuery, document).TextContent;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Seller Name: " + seller.Name + "\tSeller phone: " + seller.Phone);
                Console.ResetColor();
            }

            return planes;
        }

        static void Main(string[] args)
        {
            var url = "https://www.avbuyer.com/aircraft";
            //GetLastPage - for geting last page and method GetPlaneUrlsForPages(url, startPage, lastPage) for get all plane urls
            var urls = GetPlaneUrlsForPages(url, 1, 2);

            Console.WriteLine("For getting data press <ENTER>");
            Console.ReadLine();

            GetPlanesInfo(urls);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Process finished success!");
            Console.ResetColor();
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
