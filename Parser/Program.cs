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

                //var testList = document.ChildNodes.Where(item => item.NodeName == "blue_bg full_width_bg vif_blue_box");
                //foreach(var t in testList)
                //{
                //    Console.WriteLine($"Node name - " + t.NodeName + "\tNode type - " + t.NodeType + "\tNode Value - " + t.NodeValue
                //                        + "\nNode parent name - " + t.Parent.NodeName + "\tNode parent class name - " + t.ParentElement.ClassName);
                //}
                //var listTest = document.QuerySelectorAll($"body > section > div.conteiner").ToList();
                //var listTest = document.QuerySelectorAll($"body > section > div.conteiner > div.clearfix" + ' ' + "vif_wrapper > " +
                //                                        "div.fa_right_panel" + ' ' + "new_vif > div.vif_other_info > h1").ToList();
                //foreach (var lt in listTest)
                //{
                //    Console.WriteLine(lt.TextContent);
                //}

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
                    //var year = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("col_63") && item.Parent.NodeName == "li" && item.ParentElement.ClassName.Contains("clearfix")).ToList();
                    var year = l.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("col_63")).ToList();
                    
                    foreach (var y in year)
                    {
                        Console.WriteLine("Parent name: " + y.Parent.NodeName + "\tParent classname: " + y.ParentElement.ClassName);
                        //Console.WriteLine("Year: " + y.TextContent);
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
