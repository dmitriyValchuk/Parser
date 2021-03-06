﻿using AngleSharp;
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
using System.Drawing;


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

        static IElement GetSingleData(string queryStr, IElement element)
        {
            var query = element.QuerySelector(queryStr);

            if (query == null)
            {
                queryStr = queryStr.Replace("fa_right_panel ", "fa_right_panel new_vif");
                query = element.QuerySelector(queryStr);
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

                //var a = document.QuerySelector(planeDiscriptionStrQuery).InnerHtml;
                //Console.WriteLine(a);

                foreach (var dl in discriptionList)
                    plane.Discription += dl.TextContent.Trim(' ', '\t') + '\n';
                Console.WriteLine("Discription: " + plane.Discription);

                var planeSpecificationStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"aircraft_detail\"] > div[id=\"accordion\"] > " +
                    $"div[class=\"panel_default\"]";

                var specificationsList = GetMultiData(planeSpecificationStrQuery, document);

                plane.Specifications = new List<Specification>();
                foreach (var sl in specificationsList)
                {
                    Specification specification = new Specification();
                    var planeSpcificationTitleStrQuery = "div[class=\"panel_title\"] > h4 > a";
                    specification.Title = GetSingleData(planeSpcificationTitleStrQuery, sl).TextContent;

                    var planeSpecificationValueStrQuery = "div[class=\"panel_contain\"]";
                    specification.Value = GetSingleData(planeSpecificationValueStrQuery, sl).TextContent.Trim(' ', '\n', '\t');

                    plane.Specifications.Add(specification);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Specification: " + specification.Title + "\nValue: " + specification.Value);
                    Console.ResetColor();
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

                plane.Seller = seller;

                //Block for some info
                string planeMainPoitsStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                   $"div[class=\"fa_right_panel \"] > div[class=\"vif_other_info\"] > ul[class=\"mp0\"] > li[class=\"clearfix\"]";

                Plane planeMainInfo = GetMainInfo(GetMultiData(planeMainPoitsStrQuery, document));

                plane.Year = planeMainInfo.Year;
                plane.Location = planeMainInfo.Location;
                plane.SerialNumber = planeMainInfo.SerialNumber;
                plane.Registration = planeMainInfo.Registration;
                plane.TotlaTimeAirFrame = planeMainInfo.TotlaTimeAirFrame;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Year: " + plane.Year + "\nLocation: " + plane.Location + "\nS/N: " + plane.SerialNumber 
                    + "\nRegistration: " + plane.Registration + "\nTTAF: " + plane.TotlaTimeAirFrame + "\n\n\n");
                Console.ResetColor();

                //Getting photos
                var planePhotosStrQuery = $"body > section > div[class=\"container\"] > div[class=\"clearfix vif_wrapper\"] > " +
                    $"div[class=\"fa_left_panel \"] > div[class=\"vif_carousel owl-theme\"] > div[id=\"ad-image-carousel\"]";
                var listBlockWithImg = GetMultiData(planePhotosStrQuery, document);

                //var a = document.QuerySelector("div").InnerHtml;

                List<string> photosUrl = new List<string>();

                foreach (var ppsq in listBlockWithImg)
                {
                    var imgSrc = ppsq.QuerySelectorAll<IHtmlImageElement>($"img").Select(img => img.Source).ToList();
                    foreach (var i_s in imgSrc)
                    {
                        photosUrl.Add(i_s);
                        Console.WriteLine("IMG SRC: " + i_s);
                    }
                }

                plane.Photos = GetPhotosByteArraysFromUrl(photosUrl);

                //foreach (var p in plane.Photos)
                //{
                //    Console.WriteLine("\nSource: " + p.Source + "\n");
                //    foreach (var i in p.Image)
                //    {
                //        Console.Write(i + " ");
                //    }
                //}

                //When web project will be finished, replace imgPath! And database structure must be:
                //1. Plane table
                //id
                //other info
                //2. Img table
                //id
                //source
                //byteArr or imgPath
                //3. ImagesToPlane table
                //id
                //id_plane
                //id_img

                //this work good!
                //string imgPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)));
                //string currentFolderName = plane.Name.Replace('/', '-');
                //imgPath += "\\Images\\" + currentFolderName + "_" + plane.Seller.Name + "\\";
                //Directory.CreateDirectory(imgPath);

                //foreach(var p in plane.Photos)
                //{
                //    //Console.WriteLine("Photo size: " + GetImageFromByteArr(p.Image).Size.ToString());
                //    int indexOfBeginMainName = p.Source.LastIndexOf('/');
                //    string currentFileName = p.Source.Remove(0, indexOfBeginMainName);
                //    string finalPath = imgPath + currentFileName;
                //    File.WriteAllBytes(finalPath, p.Image);
                //}
                
                Console.ReadLine();
            }

            return planes;
        }

        static List<Photo> GetPhotosByteArraysFromUrl(List<string> photosUrl)
        {
            List<Photo> photos = new List<Photo>();
            foreach (var pu in photosUrl)
            {
                Photo photo = new Photo();
                photo.Source = pu;
                using (var webClient = new WebClient())
                {
                    photo.Image = webClient.DownloadData(pu);
                }

                photos.Add(photo);
            }
            return photos;
        }

        static Image GetImageFromByteArr(byte[] photoArr)
        {
            using (var img = new MemoryStream(photoArr))
            {
                return Image.FromStream(img);
            }
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

        static public Plane GetMainInfo(List<IElement> elements)
        {
            Plane plane = new Plane();
            foreach (var e in elements)
            {
                var liName = e.QuerySelector("div[class=\"col_30\"]").TextContent;
                string value = "div[class=\"col_63\"]";

                if (liName == "YEAR")
                    //Fucking symbol "‐", copy from debug window, becouse it isn`t equals "-" O_o (i think, diggerent encoding)
                    plane.Year = e.QuerySelector(value).TextContent.Equals("‐") ? "no information" : e.QuerySelector(value).TextContent;
                if (liName == "LOCATION")
                {
                    var a = e.QuerySelector(value).TextContent;
                    plane.Location = e.QuerySelector(value).TextContent.Equals("‐") ? "no information" : e.QuerySelector(value).TextContent.Trim(' ', '\n', '\t');
                    var b = plane.Location;
                }
                if (liName == "S/N")
                    plane.SerialNumber = e.QuerySelector(value).TextContent.Equals("‐") ? "no information" : e.QuerySelector(value).TextContent;
                if (liName == "REG")
                    plane.Registration = e.QuerySelector(value).TextContent.Equals("‐") ? "no information" : e.QuerySelector(value).TextContent;
                if (liName == "TTAF")
                    plane.TotlaTimeAirFrame = e.QuerySelector(value).TextContent.Equals("‐") ? "no information" : e.QuerySelector(value).TextContent;
            }
            return plane;
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
