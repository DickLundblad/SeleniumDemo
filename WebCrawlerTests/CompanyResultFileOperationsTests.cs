using WebCrawler.Models;

namespace WebCrawler
{
    [TestFixture]
    public class CompanyResultFileOperationsTests
    {

        /// <summary>
        /// Merge all files into one CSV file.
        /// </summary>
        /// <param name="inputFolder"></param>
        [TestCase("CompanyListings")]
        public void MergeAllCVFilesToOne(string inputFolder)
        {
            string testName = "TestMergeAllCVFilesToOne";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{testName}_{timestamp}.csv";
            SeleniumTestsHelpers.MergeAllCVFilesToOne(inputFolder, fileName);
            var filePath = Path.Combine(SeleniumTestsHelpers.GetOutputFolderPath(), fileName);
            Assert.That(File.Exists(filePath), Is.True, $"The merged file {fileName} was not created successfully.");
        }
    }
}
