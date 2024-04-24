//using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Web;
using System.Net.Http;
using TwoCaptcha;
using _2CaptchaAPI;
using System.Text.RegularExpressions;
using System.Diagnostics;
using OpenQA.Selenium.Chrome;



namespace DomainChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string domainsFile = filePath("domains.txt");
                //create domains file .txt if not exists
                CreateFileIfNotExists(domainsFile);
                CreateFileIfNotExists(filePath("higherDomains.txt"));
                CreateFileIfNotExists(filePath("lowerDomains.txt"));


                OpenTextFile("domains.txt");


                Console.WriteLine(" Entre your 2Captcha API: ");
                string myAPI = Console.ReadLine();

                Console.WriteLine("\n \n 1:High popularity \n 2:Medium popularity \n\n Enter your choice : (1 or 2)");
                int popularityChoice =int.Parse( Console.ReadLine());
                string popularity_Choice= null;
                

                switch (popularityChoice)
                {
                    case 1: popularity_Choice = "High popularity";break;
                    case 2: popularity_Choice = "Medium popularity"; break;
                 

                    default:
                        Console.WriteLine("invalid input"); break;
                }

               

                Console.WriteLine(" Enter the minimum score: ");
                int scoreBase;

                // Prompt the user until a valid number between 1 and 100 is entered
                while (true)
                {
                    if (int.TryParse(Console.ReadLine(), out scoreBase))
                    {
                        if (scoreBase >= 1 && scoreBase <= 100)
                        {
                            // Valid number entered
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Please enter a number between 1 and 100.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a valid number.");
                    }
                }

                // Use scoreBase variable for further processing
                Console.WriteLine($"The minimum score entered is: {scoreBase}");

                //open browser 
                IWebDriver driver = InitializeWebDriver();
                int i = 0;
                //start extracting domains file by index
                do
                {
                    String domain = extractfile(i, domainsFile, driver);
                    IWebElement searchBar = driver.FindElement(By.Id("searchBox"));
                    searchBar.SendKeys(domain);


                    // Find the iframe element
                    IWebElement iframe = driver.FindElement(By.TagName("iframe"));

                    // Get the value of the src attribute
                    string src = iframe.GetAttribute("src");

                    // Parse the URL
                    Uri uri = new Uri(src);

                    // Extract the value of the k parameter from the URL
                    string kValue = HttpUtility.ParseQueryString(uri.Query).Get("k");

                    // get url 
                    string currentUrl = driver.Url;



                    // Solve the CAPTCHA
                    Console.WriteLine("Solving CAPTCHA........");
                    var service = new _2CaptchaAPI._2Captcha(myAPI);
                    var response = service.SolveReCaptchaV2(kValue, currentUrl).Result;
                    string code = response.Response;
                    Console.WriteLine($"Successfully solved the CAPTCHA");
                    // Set the solved CAPTCHA
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    IWebElement recaptchaResponseElement = driver.FindElement(By.Id("g-recaptcha-response-1"));
                    js.ExecuteScript("arguments[0].removeAttribute('style');", recaptchaResponseElement);
                    js.ExecuteScript($"arguments[0].value = '{code}';", recaptchaResponseElement);

                    //Submit the form
                    IWebElement submitBtn = driver.FindElement(By.XPath("//button[contains(@onclick, 'search()')]"));
                    submitBtn.Click();


                    IWebElement popularityValue = driver.FindElement(By.Id("impactValues"));
                    string popularity = popularityValue.GetAttribute("value");



                    // Find the div element with id "domainScore"
                    IWebElement domainScoreDiv = driver.FindElement(By.Id("threatScore"));

                    // Get the text content of the div element
                    string domainScoreText = domainScoreDiv.Text;

                    // Extract numeric value using regular expression
                    Match match = Regex.Match(domainScoreText, @"\d+");
                    int ScoreValue = 0;

                    if (match.Success)
                    {
                        ScoreValue = int.Parse(match.Value );
                    }

                    validDomain(domain,ScoreValue,scoreBase, popularity, popularity_Choice);
                   
                    driver.Navigate().Refresh();
                    i++;
                } while (File.ReadAllLines(domainsFile).Length > i);

                driver.Close();
                driver.Quit();
                driver = null;
            }
            catch (Exception) { }



            OpenTextFile("higherDomains.txt");

            Console.WriteLine("===============");
            Console.WriteLine("process ends !");
            Console.WriteLine("===============");

            Console.ReadKey();

        }


        static void validDomain(string domain, int ScoreValue, int scoreBase, string popularity, string popularity_Choice)
        {
            // Check if the threat score is up to 80
            if (ScoreValue >= scoreBase && HasPopularity(popularity, popularity_Choice))
            {
                Console.WriteLine($"The domain  score is ({ScoreValue}) and {popularity_Choice}");
                SaveDomainToFile(domain, filePath("higherDomains.txt"));
            }
            else
            {
                Console.WriteLine($"The domain  score is ({ScoreValue})");
                SaveDomainToFile(domain, filePath("lowerDomains.txt"));
            }
        }


        static bool HasPopularity(string popularity,string popularity_Choice)
        {
            // Define regular expressions to match "High popularity" and "Medium popularity"
            Regex Regex = new Regex($@"\b{Regex.Escape(popularity_Choice)}\b");
            // Check if the input string contains either pattern
            return Regex.IsMatch(popularity) ;
        }


        static void OpenTextFile(string filePath)
        {
            try
            {
                Process.Start("cmd", $"/c start {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while opening the text file: {ex.Message}");
            }
        }


        static void SaveDomainToFile(string domain, string filePath)
        {
            try
            {
                // Append the domain to the file (create the file if it doesn't exist)
                using (StreamWriter writer = File.AppendText(filePath))
                {
                    // Write the domain to the file
                    writer.WriteLine(domain);
                }

                Console.WriteLine($"Domain '{domain}' saved to the file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving domain '{domain}' to file '{filePath}': {ex.Message}");
            }
        }



        private static string extractfile(int index, string filePath, IWebDriver driver)
        {
            string result = null;
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                if (lines.Length > 0) result = lines[index];
                else
                {
                    Console.WriteLine($"--------------The file is empty  {filePath}");
                    driver.Quit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("--------------An error occurred: " + ex.Message);
                driver.Quit();
            }
            return result;
        }

        private static IWebDriver InitializeWebDriver()
        {

            IWebDriver driver = new ChromeDriver();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            try
            {
                string url = "https://brightcloud.com/tools/url-ip-lookup.php";
                driver.Manage().Window.Maximize();
                driver.Navigate().GoToUrl(url);
            }
            catch (Exception)
            {
                Console.WriteLine("--------------error in openning browser!!");
            }

            return driver;
        }
        private static string filePath(string filename)
        {
            // Get the current directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // file
            string filePath = Path.Combine(currentDirectory, filename);



            return filePath;

        }

        private static void CreateFileIfNotExists(string filePath)
        {
            try
            {
                // Check if the file already exists
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"File '{Path.GetFileName(filePath)}' already exists in the following path:");
                    Console.WriteLine(filePath);
                }
                else
                {
                    // Create the file if it doesn't exist
                    using (File.Create(filePath))

                        Console.WriteLine($"File '{Path.GetFileName(filePath)}' created successfully in the following path:");
                    Console.WriteLine(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

    }
}
