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
using DocumentFormat.OpenXml.Presentation;
using System.Threading;


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

        public ObservableCollection<CrawlItem> CrawlData { get; } = new ObservableCollection<CrawlItem>();
        private JobListingsApi _api;
        private const string FixedCsvPath = @"Resources\JobCrawlSites.csv";
        private const string FixedResultFolderPath = @"JobListings";
        const char RESULT_FILE_COLUMN_SEPARATOR = ';';
        private readonly object _statusLock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isCrawling = false;
        
        public MainWindow()
        {
            AsyncLogger.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JobCrawler","logs",$"log_{DateTime.Now:yyyyMMdd}.txt"));
            InitializeComponent();
            DataContext = this;
            CsvDataGrid.ItemsSource = CrawlData;
            LoadCsvData(FixedCsvPath, RESULT_FILE_COLUMN_SEPARATOR);
            LoadFolderContents(Environment.CurrentDirectory + "\\" + FixedResultFolderPath);
            PathTextBox.Text = Environment.CurrentDirectory + "\\" + FixedResultFolderPath;
            AppendStatus("Application started");
        }

        public void AppendStatus(string message)
        {
            AsyncLogger.Log(message);
            // Thread-safe append operation
            lock (_statusLock)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusTextBlock.Text += $"{DateTime.Now:HH:mm:ss} - {message}\n";
                    StatusScrollViewer.ScrollToEnd();
            
                    // Optional: Limit the number of lines kept in memory
                    var lines = StatusTextBlock.Text.Split('\n');
                    if (lines.Length > 100) // Keep last 100 lines
                    {
                        StatusTextBlock.Text = string.Join("\n", lines.Skip(lines.Length - 100));
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        private void Button_Crawl_Info_Click(object sender, RoutedEventArgs e)
        {
           AppendStatus($"Loaded info about sites to crawl from: {FixedCsvPath}");
           LoadCsvData(FixedCsvPath, RESULT_FILE_COLUMN_SEPARATOR);
        }
        private void Button_Chrome_Debug_Click(object sender, RoutedEventArgs e)
        {
            AppendStatus("Opening Chrome in Debug Mode");
            IWebDriver driverToUse = ChromeDebugger.StartChromeInDebugMode();
            _api  = new JobListingsApi(driverToUse);
            AppendStatus("Done: Chrome can now be used for Job crawling");
        }
        private void Button_Start_Crawl_Click(object sender, RoutedEventArgs e)
        {
            if (_isCrawling)
            {
                        // If already crawling, cancel the operation
                _cancellationTokenSource?.Cancel();
                AppendStatus("Cancellation requested...");
                return;
            }

            try
            {
                 _isCrawling = true;
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                // Update UI
                AppendStatus("Starting JobCrawling...");
                ((Button)sender).Content = "Cancel Crawl";  // Change button text

                // Run the crawl operation asynchronously
                Task.Run(() => 
                {
                    try
                    {
                        LoadSitesToCrawl(cancellationToken);
                        AppendStatus("Crawl completed successfully");
                    }
                    catch (OperationCanceledException)
                    {
                        AppendStatus("Crawl was cancelled");
                    }
                    catch (Exception ex)
                    {
                        AppendStatus($"Crawl failed: {ex.Message}");
                    }
                    finally
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _isCrawling = false;
                            ((Button)sender).Content = "Crawl Selected Sites"; // Reset button text
                        });
                    }
                }, cancellationToken);
            }

            catch (Exception ex)
            {
                AppendStatus($"Failed to start crawl: {ex.Message}");
                _isCrawling = false;
                ((Button)sender).Content = "Crawl Selected Sites";
            }
            LoadFolderContents();
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

        private void LoadSitesToCrawl(CancellationToken cancellationToken)
        {
            var selectedItems = CsvDataGrid.ItemsSource
                .Cast<CrawlItem>()
                .Where(item => item.IsSelected)
                .ToList();
            
            if(selectedItems.Count == 0)
            {
                DisplayAlert($"no sites selected, please select some");
            }else
            {
                foreach (var site in selectedItems)
                {
                    // Check for cancellation before processing each site
                    cancellationToken.ThrowIfCancellationRequested();

                    AppendStatus($"Processing: {site.Url}");
        
                    try
                    {
                        AppendStatus($"Begin to crawl {site.Url}");
                        cancellationToken.ThrowIfCancellationRequested();
                        var returnMessage = _api.CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(site.Url, site.SelectorXPathForJobEntry, site.FileName, site.AddDomainToJobPaths, site.DelayUserInteraction, site.RemoveParamsInJobLinks, cancellationToken);
                        AppendStatus($"Done crawling site {site.Url}, {returnMessage}");
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        AppendStatus($"Error crawling {site.Url}: {ex.Message}");
                    }   
                }
            }
        }

        private void DisplayAlert(string message)
        {
            MessageBox.Show(message);
        }

        private void LoadCsvData(string filePath, char separator)
        {
            CrawlData.Clear();
            
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
                
                CrawlData.Add(item);
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Path.Combine(Environment.CurrentDirectory, FixedCsvPath);
        
                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show("No CSV file path specified", "Error",MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!File.Exists(path))
                {
                    MessageBox.Show($"CSV file not found at:\n{path}", "Error",MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open with default associated program
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true  // This is what opens with default program
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open CSV file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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