using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumDemo;
using SeleniumDemo.Models;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SeleniumDemo
{
    public static class SeleniumTestsHelpers
    {
        public static string RemoveInvalidChars(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Hardcoded invalid characters for Windows
            char[] invalidCharsForWindowsAndLinux = { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' };

            Console.WriteLine($"Invalid chars: {string.Join(", ", invalidCharsForWindowsAndLinux)}");
            return new string(input.Where(ch => !invalidCharsForWindowsAndLinux.Contains(ch)).ToArray());
        }

        public static string ReplaceBadCharactersInFilePath(string input)
        {
            return input.Replace(":", "_")
                        .Replace("//", "_")
                        .Replace("/", "_")
                        .Replace(".", "_")
                        .Replace("?", "_")
                        .Replace("=", "_")
                        .Replace("%", "_")
                        .Replace(")", "_")
                        .Replace("(", "_");
        }

        public static string GenerateTsvFileNameForUrl(string url)
        {
            var fileName = SeleniumTestsHelpers.RemoveInvalidChars(SeleniumTestsHelpers.ReplaceBadCharactersInFilePath(url));
            var tsvFilePath = $"JobListings_{fileName}.tsv";

            return tsvFilePath;
        }

        public static string? ExtractHref(string addDomainToJobPaths, IWebElement jobNode)
        {
            string? jobLink = string.Empty;
            try
            {
                jobLink = RetryFindAttribute(jobNode, "href");
                if (!string.IsNullOrEmpty(jobLink))
                {
                    jobLink = addDomainToJobPaths + jobLink;
                }
                if (string.IsNullOrEmpty(jobLink))
                {
                    var anchorTag = jobNode.FindElement(By.TagName("a"));
                    jobLink = RetryFindAttribute(anchorTag, "href");
                }
                if (string.IsNullOrEmpty(jobLink))
                {
                    var innerHtml = RetryFindAttribute(jobNode, "innerHTML");
                }
                TestContext.WriteLine($"Job link: {jobLink}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Could not GetAttribute(\"href\") for {ex.InnerException}");
                throw;
            }
            return jobLink;
        }

        private static string? RetryFindAttribute(IWebElement element, string attribute, int retryCount = 3)
        {
            while (retryCount-- > 0)
            {
                try
                {
                    // Use null-forgiving operator (!) to suppress CS8603 warning
                    return element.GetAttribute(attribute)!;
                }
                catch (StaleElementReferenceException)
                {
                    Thread.Sleep(1000); // Wait for 1 second before retrying
                }
            }
            throw new StaleElementReferenceException($"Element is stale after {retryCount} retries");
        }

        public static void WriteListOfJobsToFile(List<JobListing> results, string tsvFilePath)
        {
            // Log the job listings
            foreach (var jobListing in results)
            {
                TestContext.WriteLine($"JobLink: {jobListing.JobLink}, Title: {jobListing.Title}, Description: {jobListing.Description}");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t"
            };

            using (var writer = new StreamWriter(tsvFilePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<JobListing>();
                csv.NextRecord();
                foreach (var jobListing in results)
                {
                    // Remove invalid characters
                    jobListing.Title = RemoveInvalidChars(jobListing.Title);
                    jobListing.Description = RemoveInvalidChars(jobListing.Description);
                    if (!string.IsNullOrEmpty(jobListing.JobLink)) // Ensure JobLink is not empty
                    {
                        csv.WriteRecord(jobListing);
                        csv.NextRecord();
                    }
                    else
                    {
                        TestContext.WriteLine($"JobLink is empty for job listing: {jobListing.Title}");
                    }
                }
            }

            TestContext.WriteLine($"TSV file created: {tsvFilePath}");
            using (var reader = new StreamReader(tsvFilePath))
            using (var csvR = new CsvReader(reader, config))
            {
                var records = csvR.GetRecords<JobListing>().ToList();
                Assert.That(records.Count, Is.GreaterThan(0), "The TSV file does not contain any job listings.");
                TestContext.WriteLine($"Validated that the TSV file contains {records.Count} job listings.");
            }
        }

        public static void WriteToFile(JobListings results, string tsvFilePath)
        {
            // Log the job listings
            foreach (var jobListing in results.JobListingsList)
            {
                TestContext.WriteLine($"JobLink: {jobListing.JobLink}, Title: {jobListing.Title}, Description: {jobListing.Description}");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t"
            };

            using (var writer = new StreamWriter(tsvFilePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<JobListing>();
                csv.NextRecord();
                foreach (var jobListing in results.JobListingsList)
                {
                    // Remove invalid characters
                    jobListing.Title = RemoveInvalidChars(jobListing.Title);
                    jobListing.Description = RemoveInvalidChars(jobListing.Description);
                    if (!string.IsNullOrEmpty(jobListing.JobLink)) // Ensure JobLink is not empty
                    {
                        csv.WriteRecord(jobListing);
                        csv.NextRecord();
                    }
                    else
                    {
                        TestContext.WriteLine($"JobLink is empty for job listing: {jobListing.Title}");
                    }
                }
            }

            TestContext.WriteLine($"TSV file created: {tsvFilePath}");
            using (var reader = new StreamReader(tsvFilePath))
            using (var csvR = new CsvReader(reader, config))
            {
                var records = csvR.GetRecords<JobListing>().ToList();
                Assert.That(records.Count, Is.GreaterThan(0), "The TSV file does not contain any job listings.");
                TestContext.WriteLine($"Validated that the TSV file contains {records.Count} job listings.");
            }
        }

        public static JobListings LoadJobListingsFromFile(string fileName)
        {
            var jobListings = new JobListings();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                MissingFieldFound = null, // Ignore missing fields
                HeaderValidated = null   // Ignore header validation
            };

            using (var reader = new StreamReader(fileName))
            using (var csv = new CsvReader(reader, config))
            {
                try
                {
                    jobListings.JobListingsList = csv.GetRecords<JobListing>().ToList();
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Error reading file {fileName}: {ex.Message}");
                    throw;
                }
            }

            TestContext.WriteLine($"Loaded {jobListings.JobListingsList.Count} job listings from file: {fileName}");
            return jobListings;
        }

        public static string ExtactContactInfoFromHtml(string html)
        {
            var results = new List<string>();
            string pattern = @"(.{0,125}?)(\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b|07\d{2}-\d{6})(.{0,50}?)";

            foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase))
            {
                if (match.Groups.Count >= 4)
                {
                    string before = match.Groups[1].Value.Trim();
                    string contact = match.Groups[2].Value.Trim();
                    string after = match.Groups[3].Value.Trim();

                    results.Add($"{before} {contact} {after}".Trim());
                }
            }

            return string.Join(", ", results);
        }

        public static string ExtractPhoneNumbersFromAreaCodeExtractions(string html, string countryCode = "+46")
        {
            // Regex for phone numbers starting with +46, allowing spaces inside the number
            var phoneRegex = new Regex(@$"\{countryCode}[\s\-]?[0-9\s\-]+");
            var matches = phoneRegex.Matches(html);

            var result = new List<string>();

            foreach (Match match in matches)
            {
                // Find the name associated with the phone number
                var phoneIndex = html.IndexOf(match.Value);
                var nameStartIndex = html.LastIndexOfAny(new char[] { '.', ';' }, phoneIndex) + 1;
                var nameEndIndex = phoneIndex;
                var name = html.Substring(nameStartIndex, nameEndIndex - nameStartIndex).Trim();

                result.Add($"{name}, {match.Value}");
            }

            return string.Join(", ", result);
        }
    }
}