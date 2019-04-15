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

            var list = newDocument.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("listing")).ToList();
            List<string> urls = new List<string>();

            foreach (var l in list)
            {
                var childDiv = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("list_col2")).ToList();
                foreach (var c in childDiv)
                {
                    var childUl = c.QuerySelectorAll("ul").Where(item => item.ClassName != null && item.ClassName.Contains("mp0 clearfix")).ToList();
                    foreach (var cu in childUl)
                    {
                        var childDiv2 = cu.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("clearfix")).ToList();
                        foreach (var c2 in childDiv2)
                        {
                            var childDivName = c2.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("prod_name")).ToList();
                            foreach (var cdn in childDivName)
                            {
                                var hrefs = cdn.QuerySelectorAll("a").OfType<IHtmlAnchorElement>();
                                foreach (var h in hrefs)
                                {
                                    urls.Add("https://www.avbuyer.com"+h.GetAttribute("href"));
                                    Console.WriteLine(h.GetAttribute("href"));
                                }
                            }
                        }
                    }
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

                var listTest = document.QuerySelectorAll($"body > section > div.conteiner").ToList();
                //var listTest = document.QuerySelectorAll($"body > section > div.conteiner > div.clearfix" + ' ' + "vif_wrapper > " +
                //                                        "div.fa_right_panel" + ' ' + "new_vif > div.vif_other_info > h1").ToList();
                foreach (var lt in listTest)
                {
                    Console.WriteLine(lt.TextContent);
                }

                Plane plane = new Plane();
                foreach(var l in list2)
                {
                    var nameH1 = l.QuerySelectorAll("h1").ToList();
                    foreach (var h in nameH1)
                    {
                        plane.Name = h.TextContent;
                        Console.WriteLine("Name: " + h.TextContent);
                    }
                    var price = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("vif_price")).ToList();
                    if (price.Count != 0)
                    {
                        plane.Price = GetPrice(price[0].TextContent);
                        plane.Currency = GetCurrency(price[0].TextContent);
                        Console.WriteLine("Price: " + plane.Price + "Currency: " + plane.Currency);
                    }
                }

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
