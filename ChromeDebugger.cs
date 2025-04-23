using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;

namespace SeleniumDemo
{
    public static class ChromeDebugger
    {

        /// <summary>
        /// Start a chrome instance in debug mode 
        /// Requires that Chrome is installed in the default location
        /// </summary>
        /// <returns></returns>
        public static IWebDriver StartChromeInDebugMode()
    {
        IWebDriver driver;
        // open Chrome in debug mode
        try
        {
            // Check if any Chrome instances are already running
            var chromeProcesses = System.Diagnostics.Process.GetProcessesByName("chrome");
            if (chromeProcesses.Length == 0)
            {
                TestContext.WriteLine("No Chrome instances found, start a new one.");
                System.Diagnostics.Process.Start(@"C:\Program Files\Google\Chrome\Application\chrome.exe",
                    @"--remote-debugging-port=9222 --user-data-dir=C:\ChromeDebug");
                // Optional: wait a bit for Chrome to fully initialize
                Thread.Sleep(2000);
            }
            else
            {
                TestContext.WriteLine("An existing Chrome instance is already running.");
            }

            var options = new ChromeOptions();
            options.DebuggerAddress = "127.0.0.1:9222"; // Connect to the debugging port

            // Attempt to connect to the existing Chrome instance
            driver = new ChromeDriver(options);

            try
            {
                TestContext.WriteLine("Connected to existing browser");
                TestContext.WriteLine("Current URL: " + driver.Url);

                // Example: open a new tab and navigate
                driver.Navigate().GoToUrl("https://example.com");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        catch (WebDriverException ex)
        {
            TestContext.WriteLine($"Failed to connect to the existing Chrome instance: {ex.Message}");
            throw;
        }
        return driver;
    }

    }
}