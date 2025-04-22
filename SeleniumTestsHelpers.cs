using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using OpenQA.Selenium;
using SeleniumDemo.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SeleniumDemo
{
    public static class SeleniumTestsHelpers
    {
        const string RESULT_FILE_ENDING = ".csv";
        const char RESULT_FILE_COLUMN_SEPARATOR = ';';
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

        public static string? ExtractHref(string addDomainToJobPaths, IWebElement jobNode, bool removeParams = true)
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
            if (removeParams)
            {
                var uri = new Uri(jobLink);
                jobLink = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
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

        public static List<JobListing> MergeJobListings(List<JobListing> newList, List<JobListing> existingList)
        {
            return newList
                .Where(newJob => !existingList.Any(existingJob => existingJob.JobLink == newJob.JobLink))
                .Concat(existingList)
                .ToList();
        }

        public static void WriteListOfJobsToFile(List<JobListing> results, string filePath, string subFolder="")
        {
            if (!filePath.EndsWith(RESULT_FILE_ENDING))
            {
                filePath += RESULT_FILE_ENDING;
            }
            if (!string.IsNullOrEmpty(subFolder))
            {
                EnsureFolderExists(subFolder);
                filePath = Path.Combine(subFolder, filePath);
            }


            foreach (var jobListing in results)
            {
                TestContext.WriteLine($"JobLink: {jobListing.JobLink}, Title: {jobListing.Title}, Description: {jobListing.Description}");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = RESULT_FILE_COLUMN_SEPARATOR.ToString(),
            };

            using (var writer = new StreamWriter(filePath))
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

            TestContext.WriteLine($"TSV file created: {filePath}");
            using (var reader = new StreamReader(filePath))
            using (var csvR = new CsvReader(reader, config))
            {
                var records = csvR.GetRecords<JobListing>().ToList();
                Assert.That(records.Count, Is.GreaterThan(0), "The TSV file does not contain any job listings.");
                TestContext.WriteLine($"Validated that the TSV file contains {records.Count} job listings.");
            }
        }

        public static void WriteToFile(JobListings results, string filePath, string subFolder = "")
        {
            if (!filePath.EndsWith(RESULT_FILE_ENDING))
            {
                filePath += RESULT_FILE_ENDING;
            }
            if (!string.IsNullOrEmpty(subFolder))
            {
                EnsureFolderExists(subFolder);
                filePath = Path.Combine(subFolder, filePath);
            }
            // Log the job listings
            foreach (var jobListing in results.JobListingsList)
            {
                TestContext.WriteLine($"JobLink: {jobListing.JobLink}, Title: {jobListing.Title}, Description: {jobListing.Description}");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = RESULT_FILE_COLUMN_SEPARATOR.ToString(),
            };

            using (var writer = new StreamWriter(filePath))
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

            TestContext.WriteLine($"file created: {filePath}");
            using (var reader = new StreamReader(filePath))
            using (var csvR = new CsvReader(reader, config))
            {
                var records = csvR.GetRecords<JobListing>().ToList();
                Assert.That(records.Count, Is.GreaterThan(0), "The  file does not contain any job listings.");
                TestContext.WriteLine($"Validated that the file contains {records.Count} job listings.");
            }
        }

        public static JobListings LoadJobListingsFromFile(string fileName, string subFolder ="")
        {
            var nameWithoutFileExtension = Path.GetFileNameWithoutExtension(fileName);
            var jobListings = new JobListings(nameWithoutFileExtension);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = RESULT_FILE_COLUMN_SEPARATOR.ToString(),
                MissingFieldFound = null, // Ignore missing fields
                HeaderValidated = null   // Ignore header validation
            };
            if (! fileName.EndsWith(RESULT_FILE_ENDING))
            {
                fileName += RESULT_FILE_ENDING;
            }

            if (!string.IsNullOrEmpty(subFolder))
            {
                fileName = Path.Combine(subFolder, fileName);
            }
            if (File.Exists(fileName))
            { 

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
            }else
            {
                TestContext.WriteLine($"File not found: {fileName}");
            }

            TestContext.WriteLine($"Loaded {jobListings.JobListingsList.Count} job listings from file: {fileName}");
            return jobListings;
        }

        public static void EnsureFolderExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                TestContext.WriteLine($"Folder created: {folderPath}");
            }
            else
            {
                TestContext.WriteLine($"Folder already exists: {folderPath}");
            }
        }
        private static string ExtractTextUsingRegexp(string text, string regExp)
        {
          var match = Regex.Match(text, regExp);
          string res = match.Success ? match.Groups[1].Value.Trim() : "";
          return res;
        }
        private static string ExtractWholeValueFromTextUsingRegexp(string text, string regExp)
        {
          var match = Regex.Match(text, regExp);
          string res = match.Success ? match.Value.Trim() : "";
          return res;
        }

         public static string ExtactDataTestIdjobTitleText(string html)
        {
            string pattern =  @"data-testid\s*=\s*""jobTitle""[^>]*>(.*?)<\/";
            var res = ExtractTextUsingRegexp(html, pattern);
            return res;
        }
        public static string ExtactSinceInfo(string html)
        {
            string pattern = @"(en|ett|\d+)\s+(dagar|månad|månader|dag|timme|timmar)\s+sedan";
            var res = ExtractWholeValueFromTextUsingRegexp(html, pattern);
            
            return res;
        }

         public static string ExtactPostedInfo(string html)
        {
            string pattern =  @"Reposted (\d+) days ago";
            var res = ExtractTextUsingRegexp(html, pattern);
            if (res != "")
            {
                res = ($"Posted {res} days ago");
            }
            return res;
        }

         public static string ExtractApplyLatestInfo(string html)
        {
            string swedishPattern =  @"ApplyLatest\s*(\d{4}-\d{2}-\d{2})";
            string englishPattern =  @"Ansök senast\s*(\d{4}-\d{2}-\d{2})";
            var res = ExtractTextUsingRegexp(html, swedishPattern);
            if (res == "")
            {
                res = ExtractTextUsingRegexp(html, englishPattern);
            }

            return res;
        }
         public static string ExtractPublishedInfo(string html)
        {
            string swedishPattern =  @"Publicerad\s+(\d{4}-\d{2}-\d{2})";
            string englishPattern =  @"Publicerad\s+(\d{4}-\d{2}-\d{2})";
            var res = ExtractTextUsingRegexp(html, swedishPattern);
            if (res == "")
            {
                res = ExtractTextUsingRegexp(html, englishPattern);
            }
            if (res == "")
            {
                res = ExtactPostedInfo(html);
            }
            if (res == "")
            {
                res = ExtactSinceInfo(html);
            }
            //Den lediga tjänsten publicerades en månad sedan
            return res;
        }
        public static string ExtactCompanyInfo(string html)
        {
            string swedishPattern = @"Företag\s*[:\-]?\s*(.+)";
            string englishPattern = @"Company\s*[:\-]?\s*(.+)";
            var res = ExtractTextUsingRegexp(html, swedishPattern);

            if (res == "")
            {
                res = ExtractTextUsingRegexp(html, englishPattern);
            }
            return res;
        }
        public static string ExtactAreaInfo(string html)
        {
            string swedishPattern = @"Område\s*\n([^\n\r]+)";
            string englishPattern = @"Area\s*\n([^\n\r]+)";
            var res = ExtractTextUsingRegexp(html, swedishPattern);

            if (res == "")
            {
                res = ExtractTextUsingRegexp(html, englishPattern);
            }
            return res;
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
            var res = string.Join(", ", results);
            var companyName = ExtactCompanyInfo(html);
            var areaName = ExtactAreaInfo(html);
            var posted = ExtactPostedInfo(html);
            res = companyName + " " + areaName +" " + posted + " " + res;
            return res;
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
        public static void CreateExcelFromExistingFiles(string fileName, string[] files)
        { 
            using (var workbook = new XLWorkbook())
            {
                foreach (var tsvFile in files)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(tsvFile);
                    if (fileNameWithoutExtension.Length > 31)
                    {
                        fileNameWithoutExtension = fileNameWithoutExtension.Substring(0, 31);
                    }

                    var worksheet = workbook.Worksheets.Add(fileNameWithoutExtension);
                    int row = 1;

                    foreach (var line in ReadLinesPreservingCellLineBreaks(tsvFile, RESULT_FILE_COLUMN_SEPARATOR))
                    { 
                        var columns = line.Split(RESULT_FILE_COLUMN_SEPARATOR);
                        for (int col = 0; col < columns.Length; col++)
                        {
                            // Remove quotes and handle newlines
                            string cellValue = columns[col].Trim('"').Replace("\n", Environment.NewLine);
                            worksheet.Cell(row, col + 1).Value = cellValue;
                            worksheet.Cell(row, col + 1).Style.Alignment.WrapText = true;
                        }
                        row++;
                    }
                }
                workbook.SaveAs(fileName);
            }
        }

         private static IEnumerable<string> ReadLinesPreservingCellLineBreaks(string filePath, char delimiter)
        { 
                            using (var reader = new StreamReader(filePath))
                            {
                                var builder = new StringBuilder();
                                bool insideQuotedField = false;

                                while (!reader.EndOfStream)
                                {
                                    var line = reader.ReadLine();
                                    if (line == null) continue;

                                    foreach (var ch in line)
                                    {
                                        if (ch == '"' && (builder.Length == 0 || builder[^1] != '\\'))
                                        {
                                            insideQuotedField = !insideQuotedField;
                                        }
                                        builder.Append(ch);
                                    }

                                    if (!insideQuotedField)
                                    {
                                        yield return builder.ToString();
                                        builder.Clear();
                                    }
                                    else
                                    {
                                        builder.Append(Environment.NewLine);
                                    }
                                }

                                if (builder.Length > 0)
                                {
                                    yield return builder.ToString();
                                }
                            }
        }
        internal static void WriteToExcel(ListingsOverview overView, string fileNameJobListing)
        {
            // Create Excel file with ListingsOverview
            var excelFileName = $"{fileNameJobListing}.xlsx";
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                foreach (var listing in overView.JobListings)
                {
                    var worksheet = workbook.Worksheets.Add(listing.Name);
                    worksheet.Cell(1, 1).Value = "Title";
                    worksheet.Cell(1, 2).Value = "JobLink";
                    worksheet.Cell(1, 3).Value = "Published";
                    worksheet.Cell(1, 4).Value = "EndDate";
                    worksheet.Cell(1, 5).Value = "ContactInformation";
                    worksheet.Cell(1, 6).Value = "Description";
                    worksheet.Cell(1, 7).Value = "Description";

                    int row = 2;
                    foreach (var job in listing.JobListingsList)
                    {
                        worksheet.Cell(row, 1).Value = job.Title;
                        worksheet.Cell(row, 2).Value = job.JobLink;
                        worksheet.Cell(row, 3).Value = job.Published;
                        worksheet.Cell(row, 4).Value = job.EndDate;
                        worksheet.Cell(row, 5).Value = job.ContactInformation;
                        worksheet.Cell(row, 6).Value = job.Description;
                        worksheet.Cell(row, 7).Value = job.Description;
                        row++;
                    }
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), excelFileName);
                workbook.SaveAs(filePath);
            }
        }

        internal static ListingsOverview LoadListingsOverviewFromExcelFile(string fileNameOverview)
        {
            var listingsOverview = new ListingsOverview();

            using (var workbook = new XLWorkbook(fileNameOverview))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    var jobListings = new JobListings(worksheet.Name);

                    for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++) // Assuming first row is header
                    {
                        var jobListing = new JobListing
                        {
                            Title = worksheet.Cell(row, 1).GetValue<string>(),
                            JobLink = worksheet.Cell(row, 2).GetValue<string>(),
                            Published = worksheet.Cell(row, 3).GetValue<string>(),
                            EndDate = worksheet.Cell(row, 4).GetValue<string>(),
                            ContactInformation = worksheet.Cell(row, 5).GetValue<string>(),
                            Description = worksheet.Cell(row, 6).GetValue<string>(),
                            ApplyLink = worksheet.Cell(row, 7).GetValue<string>()
                        };

                        jobListings.InsertOrUpdate(jobListing);
                    }

                    listingsOverview.InsertOrUpdate(jobListings);
                }
            }

            return listingsOverview;
        }
        public static void CreateExcelFromResultFiles(string excelFileName, string[] tsvFiles)
        {
            using (var workbook = new XLWorkbook())
            {
                foreach (var tsvFile in tsvFiles)
                {
                    string sheetName = Path.GetFileNameWithoutExtension(tsvFile);
                    if (sheetName.Length > 31) // Excel sheet name limit
                    {
                        sheetName = sheetName.Substring(0, 31);
                    }

                    var worksheet = workbook.Worksheets.Add(sheetName);
                    int row = 1;

                    foreach (var line in File.ReadLines(tsvFile))
                    {
                        var columns = line.Split(RESULT_FILE_COLUMN_SEPARATOR);
                        for (int col = 0; col < columns.Length; col++)
                        {
                            worksheet.Cell(row, col + 1).Value = columns[col];
                        }
                        row++;
                    }
                }

                workbook.SaveAs(excelFileName);
            }
        }

    }
}