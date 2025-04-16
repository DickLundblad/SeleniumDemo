using OfficeOpenXml;
using SeleniumDemo.Models;

namespace SeleniumDemo
{
    public class ExcelFileTests
    {
        [Test]
        public void CreateListingsOverviewAsExcel()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileNameOverview = $"CreateOverview_{randomName}";
            var fileNameJobListing1 = $"CreateJobListings1_{randomName}";
            if (fileNameJobListing1.Length > 31)
            {
                fileNameJobListing1 = fileNameJobListing1.Substring(0, 31);
            }

            var fileNameJobListing2 = $"CreateJobListings2_{randomName}";
            if (fileNameJobListing2.Length > 31)
            {
                fileNameJobListing2 = fileNameJobListing2.Substring(0, 31);
            }

            JobListings jobListings1 = new JobListings(fileNameJobListing1);
            var job1 = new JobListing() { JobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433" };
            jobListings1.InsertOrUpdate(job1);
            JobListings jobListings2 = new JobListings(fileNameJobListing2);
            var job2 = new JobListing() { JobLink = "https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest" };
            jobListings2.InsertOrUpdate(job2);

            
            ListingsOverview overView = new ListingsOverview();
            overView.InsertOrUpdate(jobListings1);
            overView.InsertOrUpdate(jobListings2);
            WriteToExcel(overView, fileNameOverview);

            var findFileInfo = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{fileNameOverview}.*");
        }

        private static void WriteToExcel(ListingsOverview overView, string fileNameJobListing)
        {
            // Create Excel file with ListingsOverview
            var excelFileName = $"{fileNameJobListing}.xlsx";
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                foreach (var listing in overView.JobListings)
                {
                    var worksheet = workbook.Worksheets.Add(listing.Name);
                    worksheet.Cell(1, 1).Value = "Job Link";

                    int row = 2;
                    foreach (var job in listing.JobListingsList)
                    {
                        worksheet.Cell(row, 1).Value = job.JobLink;
                        row++;
                    }
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), excelFileName);
                workbook.SaveAs(filePath);
            }
        }
    }
}
