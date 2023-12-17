using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using SeleniumExtras.WaitHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Web_Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                Console.Clear();
                Console.WriteLine("Welcome to Marcelina's web scraping tool!");
                Console.WriteLine("1. Scrape Youtube videos");
                Console.WriteLine("2. Scrape job site data");
                Console.WriteLine("3. Scrape products prices on eBay");
                Console.WriteLine("4. Exit the program");
                Console.WriteLine("Please, select your scraping option: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ScrapeYouTubeVideos();
                        break;
                    case "2":
                        ScrapeIctjobsSite();
                        break;
                    case "3":
                        ScrapeEbay();
                        break;
                    case "4":
                        Console.WriteLine("exit");
                        break;
                    default:
                        Console.WriteLine("Invalid option, please try again.");
                        break;
                }
                Console.WriteLine("Press any key to return to the main menu again!");
                Console.ReadKey();

            }


        }
        public class ScrapedDataItem
        {
            public Dictionary<string, string> Data { get; set; }

            public ScrapedDataItem()
            {
                Data = new Dictionary<string, string>();
            }
        }
        public static void SaveToCSV(List<ScrapedDataItem> items, string filePath)
        {
            var csv = new StringBuilder();
            if (items.Any())
            {
                // CSV header
                var headers = items.First().Data.Keys;
                csv.AppendLine(string.Join(",", headers));

                // CSV rows
                foreach (var item in items)
                {
                    var row = item.Data.Values.Select(field => $"\"{field.Replace("\"", "\"\"")}\"");
                    csv.AppendLine(string.Join(",", row));
                }
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        public static void SaveToJSON(List<ScrapedDataItem> items, string filePath)
        {
            var jsonData = items.Select(i => i.Data).ToList();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public static void SaveScrapedData(List<ScrapedDataItem> scrapedDataItems)
        {
            // Checking if the user wants to save the data
            Console.WriteLine("\nDo you want to save the scraped data? (yes/no)");
            string saveData = Console.ReadLine();

            // if not, leave
            if (saveData.ToLower() != "yes")
            {
                Console.WriteLine("Data will not be saved. Exiting.");
                return;
            }
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string saveDirectory = homeDirectory; // Initializing saveDirectory with the default home directory

            // Asking if they want to change directory
            Console.WriteLine("\nThe default save location is your home directory.");
            Console.WriteLine("Do you want to change the save location? (yes/no)");
            string changeLocation = Console.ReadLine();

            // If yes -> new path
            if (changeLocation.ToLower() == "yes")
            {
                Console.WriteLine("Please enter the full path to the desired save directory:");
                saveDirectory = Console.ReadLine();

                // checking if directory exists, if not create it if wanted.
                if (!Directory.Exists(saveDirectory))
                {
                    Console.WriteLine("The specified directory does not exist. Would you like to create it? (yes/no)");
                    string createDir = Console.ReadLine();
                    if (createDir.ToLower() == "yes")
                    {
                        Directory.CreateDirectory(saveDirectory);
                        Console.WriteLine("Directory created.");
                    }
                    else
                    {
                        Console.WriteLine("Using the default home directory instead.");
                        saveDirectory = homeDirectory; // reverting back to default if not creating a new one
                    }
                }
            }

            // Asking for the file name
            Console.WriteLine("Please enter the filename (without extension):");
            string fileName = Console.ReadLine();
            string fullPath = Path.Combine(homeDirectory, fileName);
            Console.WriteLine("\n Please select the file format to save the data:");
            Console.WriteLine("1. CSV");
            Console.WriteLine("2. JSON");
            string formatChoice = Console.ReadLine();

            // Saving data in the chosen format
            if (formatChoice == "1")
            {
                SaveToCSV(scrapedDataItems, $"{fullPath}.csv");
                Console.WriteLine($"Data saved to {fullPath}.csv");
            }
            else if (formatChoice == "2")
            {
                SaveToJSON(scrapedDataItems, $"{fullPath}.json");
                Console.WriteLine($"Data saved to {fullPath}.json");
            }
            else
            {
                Console.WriteLine("Invalid file format selected. No data saved.");
            }
        }
        public static void ScrapeYouTubeVideos()
        {
            Console.WriteLine("What's your search term? ");
            string userUrl = Console.ReadLine();
            var options = new ChromeOptions();
            options.AddArgument("lang=en-GB");
            using (IWebDriver driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl("https://www.youtube.com");

                //Finding the elements that match by the XPath, it's being put into a list.
                var acceptAllButtons = driver.FindElements(By.XPath("//*[@id=\"content\"]/div[2]/div[6]/div[1]/ytd-button-renderer[2]/yt-button-shape/button/yt-touch-feedback-shape/div/div[2]"));

                // Checking if the list is not empty
                if (acceptAllButtons.Any())
                {
                    // Clicking the first button in the list
                    acceptAllButtons.First().Click();
                }

                System.Threading.Thread.Sleep(2500);

                var searchBar = driver.FindElement(By.Name("search_query"));
                //to fight the StaleElementReferenceException
                try
                {
                    searchBar.SendKeys(userUrl);
                }
                catch (StaleElementReferenceException e)
                {
                    
                    searchBar = driver.FindElement(By.Name("search_query"));
                    searchBar.SendKeys(userUrl);
                }

                

                searchBar.Submit();
                System.Threading.Thread.Sleep(5000);
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                IWebElement recentlyUploadedTab = null;

                try
                {
                    // Waiting for the tab to be clickable
                    recentlyUploadedTab = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//yt-formatted-string[@id='text' and contains(text(), 'Recently uploaded')]")));
                }
                catch (WebDriverTimeoutException)
                {
                    // tab not available
                    Console.WriteLine("The 'Recently uploaded' tab is not available.");
                }

                // If element is there -> click
                if (recentlyUploadedTab != null)
                {
                    recentlyUploadedTab.Click();
                }


                System.Threading.Thread.Sleep(5000);
                var videoElements = driver.FindElements(By.TagName("ytd-video-renderer"));
                int count = 0;
                if (!videoElements.Any())
                {
                    Console.WriteLine("No results found for the given search term.");
                    return;
                }
                List<ScrapedDataItem> scrapedDataItems = new List<ScrapedDataItem>();
                foreach (var videoElement in videoElements)
                {
                    if (count >= 5) break;
                    Console.WriteLine($"\nVideo number {count+1}: ");
                    var titleElement = videoElement.FindElement(By.Id("video-title"));
                    string title = titleElement.Text;
                    string url = titleElement.GetAttribute("href");
                    var channelNameElement = videoElement.FindElement(By.Id("channel-info"));
                    var uploader = channelNameElement.Text;
                    var viewsElement = videoElement.FindElement(By.Id("metadata-line"));
                    string views = viewsElement.Text;
                    views = views.Split('\n')[0];
                    ScrapedDataItem item = new ScrapedDataItem();
                    item.Data.Add("Title", title);
                    item.Data.Add("Price", url);
                    item.Data.Add("Location", uploader);
                    item.Data.Add("Seller Info", views);
                    scrapedDataItems.Add(item);
                    Console.WriteLine($"Title: {title}");
                    Console.WriteLine($"URL: {url}");
                    Console.WriteLine($"Uploader: {uploader}");
                    Console.WriteLine($"Views: {views}");
                    count++;
                }
                driver.Manage().Window.Minimize();
                SaveScrapedData(scrapedDataItems);
            }
        }
        public static void ScrapeIctjobsSite()
        {

            Console.WriteLine("What's your search term? ");
            string userUrl = Console.ReadLine();
            var options = new ChromeOptions();
            options.AddArgument("lang=en-GB");
            using (IWebDriver driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl($"https://www.ictjob.be/en/search-it-jobs?keywords={userUrl}");
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // waiting for the date to be clickable
                var sortByDateLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("sort-by-date")));

                // clicking "date"
                sortByDateLink.Click();
                System.Threading.Thread.Sleep(12000);
                var searchElements = driver.FindElements(By.CssSelector(".search-item.clearfix"));
                if (!searchElements.Any())
                {
                    Console.WriteLine("No results found for the given search term.");
                    return;
                }
                int count = 0;
                List<ScrapedDataItem> scrapedDataItems = new List<ScrapedDataItem>();
                foreach (var searchElement in searchElements)
                {
                    if (count >= 5) break;
                    Console.WriteLine($"\nJob number {count + 1}: ");

                    var titleElement = searchElement.FindElement(By.CssSelector("h2.job-title"));
                    string title = titleElement.Text;
                    var linkElement = titleElement.FindElement(By.XPath(".."));
                    string link = linkElement.GetAttribute("href");
                    var companyElement = searchElement.FindElement(By.CssSelector("span.job-company"));
                    string company = companyElement.Text;
                    var locationElement = searchElement.FindElement(By.CssSelector("span.job-location"));
                    string location = locationElement.Text;
                    var keywordsElement = searchElement.FindElement(By.CssSelector("span.job-keywords"));
                    string keywords = keywordsElement.Text;
                    ScrapedDataItem item = new ScrapedDataItem();
                    item.Data.Add("Title", title);
                    item.Data.Add("Price", company);
                    item.Data.Add("Location", location);
                    item.Data.Add("Seller Info", keywords);
                    item.Data.Add("Link", link);
                    scrapedDataItems.Add(item);
                    Console.WriteLine($"Title: {title}");
                    Console.WriteLine($"Company: {company}");
                    Console.WriteLine($"Location: {location}");
                    Console.WriteLine($"Keywords: {keywords}");
                    Console.WriteLine($"Link: {link}");

                    count++;
                }
                driver.Manage().Window.Minimize();
                SaveScrapedData(scrapedDataItems);



            }
        }

        public static void ScrapeEbay()
        {
            Console.WriteLine("What's your search term? ");
            string userKey = Console.ReadLine();
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Navigate().GoToUrl("https://www.benl.ebay.be");
                var searchBar = driver.FindElement(By.XPath("//*[@id=\"gh-ac\"]"));
                searchBar.SendKeys(userKey);
                searchBar.Submit();
                System.Threading.Thread.Sleep(5000);
                var listings = driver.FindElements(By.CssSelector(".s-item__info"));
                if (!listings.Any())
                {
                    Console.WriteLine("No results found for the given search term.");
                    return;
                }
                int count = 0;
                List<ScrapedDataItem> scrapedDataItems = new List<ScrapedDataItem>();
                foreach (var listing in listings)
                { //omit  one empty element on ebay page
                    if (count == 0)
                    {
                        count++;
                        continue;
                    }
                    if (count >= 6) break; // Only process the first 5 (-the first empty one) listings

                    Console.WriteLine($"\nJob number {count}: ");
                    string title = listing.FindElement(By.CssSelector(".s-item__title")).Text;
                    string price = listing.FindElement(By.CssSelector(".s-item__price")).Text;
                    string location = listing.FindElement(By.CssSelector(".s-item__itemLocation")).Text;
                    string sellerInfo = listing.FindElement(By.CssSelector(".s-item__seller-info")).Text;
                    ScrapedDataItem item = new ScrapedDataItem();
                    item.Data.Add("Title", title);
                    item.Data.Add("Price", price);
                    item.Data.Add("Location", location);
                    item.Data.Add("Seller Info", sellerInfo);
                    scrapedDataItems.Add(item);
                    Console.WriteLine($"Title: {title}");
                    Console.WriteLine($"Price: {price}");
                    Console.WriteLine($"Location: {location}");
                    Console.WriteLine($"Seller info: {sellerInfo}");

                    count++;
                }
                driver.Manage().Window.Minimize();
                SaveScrapedData(scrapedDataItems);
            }
            

        }
    }
}
