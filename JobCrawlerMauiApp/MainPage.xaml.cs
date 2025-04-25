

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualStudio.PlatformUI;
using OpenQA.Selenium;
using System.Globalization;
using WebCrawler;
using static JobListingsApi;

namespace JobCrawlerMauiApp
{
    public partial class MainPage : ContentPage
    {
        public class CrawlItem : ObservableObject  // Implement INotifyPropertyChanged
        {
            public string Url { get; set; }
            public string SelectorXPathForJobEntry { get; set; }
            public string FileName { get; set; }
            public string AddDomainToJobPaths { get; set; }
            public int DelayUserInteraction { get; set; }
            public bool RemoveParamsInJobLinks { get; set; }
    
            private bool _isSelected;

            [CsvHelper.Configuration.Attributes.Ignore]
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }
        }

        private JobListingsApi _api;

        const char RESULT_FILE_COLUMN_SEPARATOR = ';';

        public MainPage()
        {
            InitializeComponent();
            IWebDriver driverToUse = ChromeDebugger.StartChromeInDebugMode();
            _api  = new JobListingsApi(driverToUse);
            LoadSitesToCrawl();
        }

         private async void LoadSitesToCrawl()
        { 
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = RESULT_FILE_COLUMN_SEPARATOR.ToString(),
            };
            try
            {
                // Load the CSV file from resources
                await using var stream = await FileSystem.OpenAppPackageFileAsync("JobCrawlSites.csv");
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, config);
            
                // Read and display the data
                var records = csv.GetRecords<CrawlItem>().ToList();
                DataCollectionView.ItemsSource = records;

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load CSV: {ex.Message}", "OK");
            }
        }



        private async void OnCrawlClicked(object sender, EventArgs e)
        {
            try
            {
                ProgressBar.Progress = 0;
                ProgressLabel.Text = "Begin to crawl";
                CrawlBtn.IsEnabled = false;
                            var selectedItems = DataCollectionView.ItemsSource
                .Cast<CrawlItem>()
                .Where(item => item.IsSelected)
    .            ToList();
            var timeout = TimeSpan.FromMinutes(2);
            foreach (var data in selectedItems)
            {
                    var progress = new Progress<CrawlProgressReport>(async report =>
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ProgressBar.Progress = report.Percentage / 100.0; // MAUI uses 0.0-1.0 range
                            ProgressLabel.Text = report.Message;
                        });
            
                        Console.WriteLine(report.Message);
                    });

                    //var progress = new Progress<CrawlProgressReport>();
                    using var cts = new CancellationTokenSource(timeout);
                    try
                    {
                        await _api.CrawlWithProgressAsync(data.Url, data.SelectorXPathForJobEntry, data.FileName, progress, data.AddDomainToJobPaths, data.DelayUserInteraction, data.RemoveParamsInJobLinks);
                    }catch (OperationCanceledException)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ProgressLabel.Text = $"Timeout processing {data.Url}";
                        });
                        Console.WriteLine($"Timeout occurred while processing {data.Url}");
                        // Continue with next item instead of breaking the loop
                        continue;
                    }
                }
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => CrawlBtn.IsEnabled = true);
                //MainThread.BeginInvokeOnMainThread(() => ProgressLabel.Text = "Ready"); ;
            }
        }
    }
}
