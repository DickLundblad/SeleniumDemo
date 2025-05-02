using System.Windows;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualStudio.PlatformUI;
using OpenQA.Selenium;
using System.Globalization;
using WebCrawler;
using static JobListingsApi;
using System.IO;
using System.Data;
using Microsoft.Win32;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System;
using System.Windows.Controls;
using System.Diagnostics;


namespace JobCrawlerWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class CrawlItem : INotifyPropertyChanged
        {
            private bool _isSelected;
            public bool IsSelected     
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
            public string Url { get; set; }
            public string SelectorXPathForJobEntry { get; set; }
            public string FileName { get; set; }
            public string AddDomainToJobPaths { get; set; }
            public int DelayUserInteraction { get; set; }
            public bool RemoveParamsInJobLinks { get; set; }


            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<CrawlItem> CsvData { get; } = new ObservableCollection<CrawlItem>();
        private JobListingsApi _api;
        private const string FixedCsvPath = @"Resources\JobCrawlSites.csv";
        private const string FixedResultFolderPath = @"JobListings";
        const char RESULT_FILE_COLUMN_SEPARATOR = ';';
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            CsvDataGrid.ItemsSource = CsvData;
            LoadCsvData(FixedCsvPath, RESULT_FILE_COLUMN_SEPARATOR);
            LoadFolderContents(Environment.CurrentDirectory + "\\" + FixedResultFolderPath);
            PathTextBox.Text = Environment.CurrentDirectory + "\\" + FixedResultFolderPath;
        }

        private void Button_Crawl_Info_Click(object sender, RoutedEventArgs e)
        {
            StatusLabel.Text = $"Loaded info about sites to crawl from: {FixedCsvPath}";

            // Load and display CSV data
           LoadCsvData(FixedCsvPath, RESULT_FILE_COLUMN_SEPARATOR);
        }
        private void Button_Chrome_Debug_Click(object sender, RoutedEventArgs e)
        {
            StatusLabel.Text = "Opened Chrome in Debug";
            IWebDriver driverToUse = ChromeDebugger.StartChromeInDebugMode();
            _api  = new JobListingsApi(driverToUse);
        }

        private void Button_Start_Crawl_Click(object sender, RoutedEventArgs e)
        {
            StatusLabel.Text = "Begin Crawling";
            LoadSitesToCrawl();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Prevent the click from bubbling up to the row
            e.Handled = true;
    
            // Get the checkbox that was clicked
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.DataContext is CrawlItem item)
            {
                // The binding will handle the value change, but we can add additional logic here
                Console.WriteLine($"Checkbox clicked for {item.FileName}, new value: {item.Url}");
            }
        }

        private async void LoadSitesToCrawl()
        { 
            List<CrawlItem> selectedItems = CsvData.Where(item => item.IsSelected).ToList();
            
            if(selectedItems.Count == 0)
            {
                DisplayAlert($"no sites selected, please select some");
            }else
            {
                foreach (var data in selectedItems)
                {
                    var timeout = TimeSpan.FromMinutes(2);
                    try
                    {
                        StatusLabel.Text = "Crawling " + data.Url;
                        //await _api.CrawlWithProgressAsync(data.Url, data.SelectorXPathForJobEntry, data.FileName, progress, data.AddDomainToJobPaths, data.DelayUserInteraction, data.RemoveParamsInJobLinks);

                        //var progress = new Progress<CrawlProgressReport>(async report =>
                        //{
                        //    await MainThread.InvokeOnMainThreadAsync(() =>
                        //    {
                        //        ProgressBar.Progress = report.Percentage / 100.0; // MAUI uses 0.0-1.0 range
                        //        ProgressLabel.Text = report.Message;
                        //    });

                        //    Console.WriteLine(report.Message);
                        //});
                        var progress = new Progress<CrawlProgressReport>();
                        _api.CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(data.Url, data.SelectorXPathForJobEntry, data.FileName, data.AddDomainToJobPaths, data.DelayUserInteraction, data.RemoveParamsInJobLinks);
                        //await _api.CrawlWithProgressAsync(data.Url, data.SelectorXPathForJobEntry, data.FileName, progress,data.AddDomainToJobPaths, data.DelayUserInteraction, data.RemoveParamsInJobLinks);
                        StatusLabel.Text = "Crawled " + data.FileName;
                    }
                    catch (Exception ex)
                    {
                        StatusLabel.Text = "Error: " + ex.Message;
                    }
                }
                LoadFolderContents();
            }
        }

        private void DisplayAlert(string message)
        {
            MessageBox.Show(message);
        }

        private void LoadCsvData(string filePath, char separator)
        {
            CsvData.Clear();
            
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length == 0) return;
            // Skip header row if exists
            int startLine = lines.Length > 0 && lines[0].StartsWith("Url;") ? 1 : 0;

          for (int i = startLine; i < lines.Length; i++)
                {
                string[] values = lines[i].Split(separator);
                
                var item = new CrawlItem
                {
                    // Assign values to fixed columns
                    Url = values.Length > 0 ? values[0] : string.Empty,
                    SelectorXPathForJobEntry = values.Length > 1 ? values[1] : string.Empty,
                    FileName = values.Length > 2 ? values[2] : string.Empty,
                    AddDomainToJobPaths = values.Length > 2 ? values[3] : string.Empty,
                    DelayUserInteraction = values.Length > 1 ? int.Parse(values[4]) : 0,
                    RemoveParamsInJobLinks = values.Length > 4 ? bool.Parse(values[5]) : true,
                };
                
                CsvData.Add(item);
            }
        }
    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var path = Environment.CurrentDirectory + "\\" + FixedResultFolderPath;
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            Process.Start("explorer.exe", path);
        }
        else
        {
            MessageBox.Show("Folder path is invalid or doesn't exist", "Error", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadFolderContents();
    }

    private void LoadFolderContents(string path = "")
    {
        try
        {
            FolderContentsListView.Items.Clear();
            if (path == string.Empty)
            {
                path = PathTextBox.Text;
            }
            
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }

            // Add directories
            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirInfo = new DirectoryInfo(dir);
                FolderContentsListView.Items.Add(new FolderItem
                {
                    Name = dirInfo.Name,
                    Type = "Folder",
                    Size = "",
                    ModifiedDate = dirInfo.LastWriteTime.ToString(),
                    FullPath = dirInfo.FullName
                });
            }

            // Add files
            foreach (var file in Directory.GetFiles(path))
            {
                var fileInfo = new FileInfo(file);
                FolderContentsListView.Items.Add(new FolderItem
                {
                    Name = fileInfo.Name,
                    Type = fileInfo.Extension.ToUpper().TrimStart('.') + " File",
                    Size = $"{Math.Ceiling(fileInfo.Length / 1024.0):0} KB", // Rounded up KB
                    ModifiedDate = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    FullPath = fileInfo.FullName
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading folder: {ex.Message}");
        }
    }
        public class CrawlProgressReport
        {
            public string Message { get; }
            public int Percentage { get; }
    
            public CrawlProgressReport(string message, int percentage)
            {
                Message = message;
                Percentage = percentage;
            }
       }
        public class FolderItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Size { get; set; }
            public string ModifiedDate { get; set; }
            public string FullPath { get; set; }
        }
     }
}