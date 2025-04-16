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
            string prompt = $@"extract contact information and roles from this text in the same language as the text: {bodyNode.Text}";
            response = ChatGPTSearch(prompt);

            // fallback solutions
            if (string.IsNullOrWhiteSpace(response))
            {
                response = SeleniumTestsHelpers.ExtactContactInfoFromHtml(bodyNode.Text);
            }
            if (string.IsNullOrWhiteSpace(response))
            {
                response = SeleniumTestsHelpers.ExtractPhoneNumbersFromAreaCodeExtractions(bodyNode.Text);
            }

            TestContext.WriteLine($"Extracted ContactInfo: {response}");
            return response;
        }
        public string ExtractTitle()
        {
            string response = string.Empty;
            var bodyNode = driver.FindElement(By.XPath("//body"));
            string prompt = $@"extract job title from this text in the same language as the text: {bodyNode.Text}";
            response = ChatGPTSearch(prompt);

            // fallback solutions
            if (string.IsNullOrEmpty(response))
            {
                IWebElement? titleNode = TryFindElement("//h1");
                if (titleNode != null)
                {
                    response = titleNode.Text;
                }
                else
                {
                    SeleniumTestsHelpers.ExtactDataTestIdjobTitleText(bodyNode.Text);
                }
                TestContext.WriteLine($"Extracted Title: {response}");
            }
            return response;
        }

        public string ExtractPublishedDate()
        {
            string response = string.Empty;
            var bodyNode = driver.FindElement(By.XPath("//body"));
            string prompt = $@"extract published date from this text in the same language as the text: {bodyNode.Text}";
            response = ChatGPTSearch(prompt);

            // fallback solution using regex
            if (string.IsNullOrEmpty(response))
            {
                response = SeleniumTestsHelpers.ExtractPublishedInfo(bodyNode.Text);
            }

            TestContext.WriteLine($"Extracted Published Date: {response}");
            return response;
        }

        public string ChatGPTSearch(string prompt)
        { 
            string response  = string.Empty;
            if (_chatService != null)
            {
                TestContext.WriteLine($"Using ChatGPT to extract Info, {prompt}");       
                var task = _chatService.GetChatResponse(prompt);
                if (task != null)
                {
                    response = task.Result;
                    TestContext.WriteLine($"Result from ChatGPT: {prompt}");
                }
                if (response == string.Empty)
                {
                   TestContext.WriteLine($"ChatGPT returned empty response for prompt");
                }
            }
            return response;
        }

        private IWebElement? TryFindElement(string selector)
        {
            try
            {
                return driver.FindElement(By.XPath(selector));
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Selector {selector} not found: {ex.Message}");
                return null;
            }
        }
    }
}