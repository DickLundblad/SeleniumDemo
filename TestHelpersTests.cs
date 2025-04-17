namespace SeleniumDemo
{
    [TestFixture]
    public class TestHelpersTests
    {

        [TestCase("2025-04-15", "JobbSafari_automation-engineer-level-1-cold-mill-ssste-19223651.txt")]
        [TestCase("2025-04-15", "JobbSafari_automation-engineer-level-2-ssste-19223731.txt")]
        [TestCase("", "Linked_in_factor_10.txt")]
        [TestCase("1 dag sedan", "Monster_Meror_rekrytering.txt")]
        [TestCase("", "se_indeed_com_Tutor_AI_Trainer.txt")]
        [TestCase("", "se_jooble_Bilplatslagare.txt")]
        public void TestExtractPublishedInfo(string expected, string fileName)
        {
            string text = ReadFileContent(fileName);
            var result = SeleniumTestsHelpers.ExtractPublishedInfo(text);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("Boden", "JobbSafari_automation-engineer-level-1-cold-mill-ssste-19223651.txt")]
        [TestCase("Boden", "JobbSafari_automation-engineer-level-2-ssste-19223731.txt")]
        [TestCase("", "Linked_in_factor_10.txt")]
        [TestCase("", "Monster_Meror_rekrytering.txt")]
        [TestCase("", "se_indeed_com_Tutor_AI_Trainer.txt")]
        [TestCase("", "se_jooble_Bilplatslagare.txt")]
        public void TestExtractAreaInfo(string expected, string fileName)
        {
            string text = ReadFileContent(fileName);
            var result = SeleniumTestsHelpers.ExtactAreaInfo(text);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("", "JobbSafari_automation-engineer-level-1-cold-mill-ssste-19223651.txt")]
        [TestCase("", "JobbSafari_automation-engineer-level-2-ssste-19223731.txt")]
        [TestCase("", "Linked_in_factor_10.txt")]
        [TestCase("", "Monster_Meror_rekrytering.txt")]
        [TestCase("", "se_indeed_com_Tutor_AI_Trainer.txt")]
        [TestCase("", "se_jooble_Bilplatslagare.txt")]
        public void TestExtractJobTitle(string expected, string fileName)
        {
            string text = ReadFileContent(fileName);
            var result = SeleniumTestsHelpers.ExtactDataTestIdjobTitleText(text);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Category("live")]
        [TestCase("JobListingsExcel_ClosedXml_.xlsx", "*.csv", "JobListings")]
        public void ZZ_CreateExcelSheetWithJobListingsUsingClosedXML(string fileName, string filePattern, string subFolder ="")
        {
            var files = GetFileNames(filePattern, subFolder);
            if (files != null)
            {
                SeleniumTestsHelpers.CreateExcelFromExistingFiles(fileName, files);
            }
            else
            {
                TestContext.WriteLine($"No files found with pattern: {filePattern}");
            }
        }

        public string ReadFileContent(string fileName)
        {
            // Define the file path relative to the project directory
            string filePath = Path.Combine("TestData", "OfflinePages","Body",fileName);

            // Ensure the file exists before attempting to read
            Assert.That(File.Exists(filePath), Is.True, $"File not found: {filePath}");

            // Read the file content
            string fileContent = File.ReadAllText(filePath);

            return fileContent;
        }

        private string[]? GetFileNames(string searchPatternForFiles, string subPath)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), subPath);
            var files = Directory.GetFiles(folder, searchPatternForFiles);
            if (files.Length == 0)
            {
                TestContext.WriteLine("No files found.");
                return null;
            }
            return files;
        }
    }
}
