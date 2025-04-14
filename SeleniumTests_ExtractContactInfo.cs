using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace SeleniumDemo
{
    public partial class SeleniumTests
    {
        private string ExtractContactInfo() 
        {
            string response = string.Empty;
            var bodyNode = driver.FindElement(By.XPath("//body"));

            if (_chatService != null) 
            {
                TestContext.WriteLine($"Using ChatGPT to extract contact Info");
                string prompt = $@"extract contact information and roles from this text in the same language as the text: {bodyNode.Text}";
                var task = _chatService.GetChatResponse(prompt);
                if (task != null)
                {
                    response = task.Result;
                }
                if (response != string.Empty) 
                {
                    return response;
                } else 
                {
                    TestContext.WriteLine($"ChatGPT returned empty response for prompt: {prompt}");
                }
            }        
            response = SeleniumTestsHelpers.ExtractPhoneNumbersFromAreaCodeExtractions(bodyNode.Text);

            if (string.IsNullOrEmpty(response)) 
            {
                response = SeleniumTestsHelpers.ExtactContactInfoFromHtml(bodyNode.Text);
            }
            TestContext.WriteLine($"Extracted ContactInfo: {response}");
            return response;
       }
        public string ExtractTitle() 
        {
            string? response = null; // Initialize as null to avoid CS8601

            IWebElement titleNode = TryFindElement("//h1");
            if (titleNode != null) 
            {
                response = titleNode.Text;
            } else 
            {
                TestContext.WriteLine($"Title node not found");
                response = null;
            }
            TestContext.WriteLine($"Extracted Title: {response}");
            return response;
         }

        private IWebElement TryFindElement(string selector)
        { 
            try 
            {
                return driver.FindElement(By.XPath(selector));
            } catch (Exception ex) 
            {
                TestContext.WriteLine($"Selector {selector} not found: {ex.Message}");
                return null;
            }
         }
    }
}