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
        static string myAPI;
        static void Main(string[] args)
        {

            //user input
            userInput input= userInputInterface();
            string domainsFile= input.domainsFile;
            string popularity_Choice= input.popularity_Choice;
            int scoreBase = input.scoreBase;


            //open browser 
            IWebDriver driver =null;
            try
            {
                driver = InitializeWebDriver();
            }
            catch (Exception) {    Console.WriteLine("------------Error in opening browser!");   }


            int numberOfDomains = File.ReadAllLines(domainsFile).Length;
            String[] domains = null;

            //start extracting domains file by index
            domains = extractfile(domainsFile, driver);



            // start the process
            int i = 0;
            checkDomainProcess(i, domainsFile, driver, scoreBase, popularity_Choice , domains, numberOfDomains);
            

            //end the process
            try
            {
                closeBrowser(driver);
            }
            catch (Exception)
            {
               
                
            }
            
            //open the file
            OpenTextFile("Valid_Domains.txt");

            Console.WriteLine("===============");
            Console.WriteLine("process ends !");
            Console.WriteLine("===============");

            Console.ReadKey();

        }

        static void closeBrowser(IWebDriver driver)
        {
            if (IsWebDriverAlive(driver))
            {
                driver.Close();
                driver.Quit();
                driver = null;

            }
            driver = null;

            bool IsWebDriverAlive(IWebDriver driver)
            {
                try
                {
                    // Check if the WebDriver instance is null or not closed
                    return driver != null && driver.WindowHandles.Count > 0;
                }
                catch (WebDriverException)
                {
                    // WebDriver is closed
                    return false;
                }
            }
        }
        static void checkDomainProcess(int index , string domainsFile, IWebDriver driver, int scoreBase, string popularity_Choice ,string[] domains, int numberOfDomains)
        {

           

            do
            {
                try
                {
                    //passed the captcha
                    passeTheCaptcha(driver, domains[index], index, domainsFile);

                    //Submit 
                    IWebElement submitBtn = driver.FindElement(By.XPath("//button[contains(@onclick, 'search()')]"));
                    submitBtn.Click();


                    //catching Popularity element
                    string popularity = catchPopularityValue(driver);

                    //catching Score element
                    int ScoreValue = catchDomainScore(driver);
                        

                    try
                    {
                        validDomain(domains[index], ScoreValue, scoreBase, popularity, popularity_Choice);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error in checking domain {domains[index]}  : {e.Message}");

                    } 

                    driver.Navigate().Refresh();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in checking domain {domains[index]}  : {e.Message}");
                   
                }
                finally
                {
                    // Regardless of whether an exception occurred or not, always increment i
                    index++;
                }
                    
            } while (numberOfDomains > index);
        }



        static userInput userInputInterface()
        {
            string domainsFile = filePath("domains.txt");
            try
            {
                //create domains file .txt if not exists
                CreateFileIfNotExists(domainsFile);
                CreateFileIfNotExists(filePath("HighScore_Domains.txt"));
                CreateFileIfNotExists(filePath("LowScore_Domains.txt"));
                CreateFileIfNotExists(filePath("Valid_Domains.txt"));

                OpenTextFile("domains.txt");
            }
            catch (Exception) { Console.WriteLine("------------Error in .TXT file"); }


            Console.WriteLine(" Entre your 2Captcha API: ");
            myAPI = Console.ReadLine();


            string popularityChoice;
            string popularity_Choice = null;

            do
            {
                Console.WriteLine("\n \n 1:High & Medium popularity \n 2:low & Medium popularity \n\n Enter your choice : (1 or 2)");
                popularityChoice = Console.ReadLine();

                switch (popularityChoice)
                {
                    case "1": popularity_Choice = "High popularity|Medium popularity"; break;
                    case "2": popularity_Choice = "low popularity|Medium popularity"; break;
                }
            } while ((!popularityChoice.Equals("1")) && (!popularityChoice.Equals("2")));



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


            userInput input = new userInput(domainsFile, popularity_Choice, scoreBase);
           
            return input;
        }

        public class userInput
        {
            public string domainsFile { get; set; }
            public string popularity_Choice { get; set; }
            public int scoreBase { get; set; }

            // Constructor
            public userInput(string domainsFile, string popularity_Choice, int scoreBase)
            {
                this.domainsFile = domainsFile;
                this.popularity_Choice = popularity_Choice;
                this.scoreBase = scoreBase;
            }
        }



        static string catchPopularityValue(IWebDriver driver)
        {
            IWebElement popularityValue = driver.FindElement(By.Id("impactValues"));
            string popularity = popularityValue.GetAttribute("value");
            return popularity;
        }

        static void passeTheCaptcha(IWebDriver driver, string domain, int index, string domainsFile)
        {
            

            //find search box
            IWebElement searchBar = driver.FindElement(By.Id("searchBox"));
            searchBar.SendKeys(domain);

            //extracting captcha key 
            string captchaKey = extractCaptchaKey(driver);

            // get url 
            string currentUrl = driver.Url;

            //send captcha request 
            string code = captchaRequest(myAPI, captchaKey, currentUrl, driver);

            if (!isCaptchaReturnError(code, driver))
            {
                Console.WriteLine($"Successfully solved the CAPTCHA");
            }

            // Set the solved CAPTCHA in the textarea
            solveCaptcha(driver, code);
        }

        static void solveCaptcha(IWebDriver driver,string code)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            IWebElement recaptchaResponseElement = driver.FindElement(By.Id("g-recaptcha-response-1"));
            js.ExecuteScript("arguments[0].removeAttribute('style');", recaptchaResponseElement);
            js.ExecuteScript($"arguments[0].value = '{code}';", recaptchaResponseElement);
        }


        static int catchDomainScore(IWebDriver driver)
        {

            // Find the div element with id "domainScore"
            IWebElement domainScoreDiv = driver.FindElement(By.Id("threatScore"));

            // Get the text content of the div element
            string domainScoreText = domainScoreDiv.Text;

            // Extract numeric value using regular expression
            Match match = Regex.Match(domainScoreText, @"\d+");
            int ScoreValue = 0;

            if (match.Success)
            {
                ScoreValue = int.Parse(match.Value);
            }
            return ScoreValue;
        }





        static string extractCaptchaKey(IWebDriver driver)
        {
            // Find the iframe element
            IWebElement iframe = driver.FindElement(By.TagName("iframe"));


            // Get the value of the src attribute
            string src = iframe.GetAttribute("src");


            // Parse the URL
            Uri uri = new Uri(src);


            // Extract the value of the k parameter from the URL
            string kValue = HttpUtility.ParseQueryString(uri.Query).Get("k");

            return kValue;
        }


        static string captchaRequest (string myAPI, string kValue,string currentUrl, IWebDriver driver) 
        {
            // Solve the CAPTCHA
            Console.WriteLine("Solving CAPTCHA........");
            var service = new _2CaptchaAPI._2Captcha(myAPI);
            var response = service.SolveReCaptchaV2(kValue, currentUrl).Result;
            string code = response.Response;            
                return code;
        }

        static bool isCaptchaReturnError(string code, IWebDriver driver)
        {
            bool result;
            switch (code)
            {
                case "ERROR_WRONG_USER_KEY":
                    Console.WriteLine(" ERROR_WRONG_USER_KEY You've provided the key parameter value in an incorrect format. Check your API key.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_KEY_DOES_NOT_EXIST":
                    Console.WriteLine(" ERROR_KEY_DOES_NOT_EXIST The key you've provided does not exist. Check your API key.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_ZERO_BALANCE":
                    Console.WriteLine(" ERROR_ZERO_BALANCE You don't have funds on your account. Deposit your account to continue solving captchas.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_PAGEURL":
                    Console.WriteLine(" ERROR_PAGEURL pageurl parameter is missing in your request. Stop sending requests and change your code to provide valid pageurl parameter.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_NO_SLOT_AVAILABLE":
                    Console.WriteLine(" ERROR_NO_SLOT_AVAILABLE You can receive this error in two cases...");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_ZERO_CAPTCHA_FILESIZE":
                    Console.WriteLine(" ERROR_ZERO_CAPTCHA_FILESIZE Image size is less than 100 bytes. Check the image file.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_TOO_BIG_CAPTCHA_FILESIZE":
                    Console.WriteLine(" ERROR_TOO_BIG_CAPTCHA_FILESIZE Image size is more than 600 kB or image is bigger than 1000px on any side. Check the image file.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_WRONG_FILE_EXTENSION":
                    Console.WriteLine(" ERROR_WRONG_FILE_EXTENSION Image file has an unsupported extension. Accepted extensions: jpg, jpeg, gif, png. Check the image file.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_IMAGE_TYPE_NOT_SUPPORTED":
                    Console.WriteLine(" ERROR_IMAGE_TYPE_NOT_SUPPORTED Server can't recognize the image file type. Check the image file.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_UPLOAD":
                    Console.WriteLine(" ERROR_UPLOAD Server can't get file data from your POST-request. Check your code that makes POST request.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_IP_NOT_ALLOWED":
                    Console.WriteLine(" ERROR_IP_NOT_ALLOWED The request is sent from an IP that is not on the list of your allowed IPs. Check the list of your allowed IPs.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "IP_BANNED":
                    Console.WriteLine(" IP_BANNED Your IP address is banned due to many frequent attempts to access the server using wrong authorization keys. Ban will be automatically lifted after 5 minutes.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_BAD_TOKEN_OR_PAGEURL":
                    Console.WriteLine(" ERROR_BAD_TOKEN_OR_PAGEURL You can get this error code when sending reCAPTCHA V2. That happens if your request contains an invalid pair of googlekey and pageurl. Explore the code of the page carefully to find valid pageurl and sitekey values.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_GOOGLEKEY":
                    Console.WriteLine(" ERROR_GOOGLEKEY You can get this error code when sending reCAPTCHA V2. That means that the sitekey value provided in your request is incorrect: it's blank or malformed. Check your code that gets the sitekey and makes requests to our API.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_PROXY_FORMAT":
                    Console.WriteLine(" ERROR_PROXY_FORMAT You use incorrect proxy format in your request to in.php. Use proper format as described in the section Using proxies.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_WRONG_GOOGLEKEY":
                    Console.WriteLine(" ERROR_WRONG_GOOGLEKEY googlekey parameter is missing in your request. Check your code that gets the sitekey and makes requests to our API.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_CAPTCHAIMAGE_BLOCKED":
                    Console.WriteLine(" ERROR_CAPTCHAIMAGE_BLOCKED You've sent an image that is marked in our database as unrecognizable. Try to override the website's limitations.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "TOO_MANY_BAD_IMAGES":
                    Console.WriteLine(" TOO_MANY_BAD_IMAGES You are sending too many unrecognizable images. Make sure that your last captchas are visible and check unrecognizable images we saved for analysis. Then fix your software to submit images properly.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "MAX_USER_TURN":
                    Console.WriteLine(" MAX_USER_TURN You made more than 60 requests to in.php within 3 seconds. Your account is banned for 10 seconds. Ban will be lifted automatically. Set at least 100 ms timeout between requests to in.php.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_BAD_PARAMETERS":
                    Console.WriteLine(" ERROR_BAD_PARAMETERS  The error code is returned if some required parameters are missing in your request or the values have incorrect format. Or in case if you have SandBox mode and 100% recognition options enabled at the same time. Check that your request contains all the required parameters and the values are in the proper format. Use debug mode to see which values you send to our API.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_BAD_PROXY":
                    Console.WriteLine(" ERROR_BAD_PROXY  You can get this error code when sending a captcha via a proxy server which is marked as BAD by our API. Use a different proxy server in your requests.");
                    closeBrowser(driver);
                    result = true;
                    break;
                case "ERROR_SITEKEY":
                    Console.WriteLine(" ERROR_SITEKEY   You can get this error code when sending hCaptcha. That means that the sitekey value provided in your request is incorrect: it's blank or malformed. Check your code that gets the sitekey and makes requests to our API.");
                    closeBrowser(driver);
                    result = true;
                    break;
                default:
                    result= false;
                    break;
                   
            }
            return result;
        }

        static void validDomain(string domain, int ScoreValue, int scoreBase, string popularity, string popularity_Choice)
        {
            // Check if the threat score is up to 80
            if (ScoreValue >= scoreBase)
            {
                Console.WriteLine($"The domain score is ({ScoreValue})");
                if (HasPopularity(popularity, popularity_Choice))
                {
                    Console.WriteLine($"Valid domain ....");
                    SaveDomainToFile(domain, filePath("Valid_Domains.txt"));
                }
                else
                {
                    SaveDomainToFile(domain, filePath("HighScore_Domains.txt"));
                }

            }
            else
            {
                Console.WriteLine($"The domain score is ({ScoreValue})");
                SaveDomainToFile(domain, filePath("LowScore_Domains.txt"));
            }
        }


        static bool HasPopularity(string popularity, string popularity_Choice)
        {
            // Split the choices and escape each phrase
            string[] choices = popularity_Choice.Split('|');
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i] = Regex.Escape(choices[i].Trim());
            }

            // Join the escaped choices with |
            string escapedChoices = string.Join("|", choices);

            // Define regular expressions to match any of the popularity choices
            Regex regex = new Regex($@"\b(?:{escapedChoices})\b", RegexOptions.IgnoreCase);

            // Check if the input string contains any of the patterns
            return regex.IsMatch(popularity);
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



        private static string[] extractfile(string filePath, IWebDriver driver)
        {
            string[] result = null;
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                if (lines.Length > 0) result = lines;
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
