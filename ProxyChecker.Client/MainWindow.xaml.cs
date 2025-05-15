using Microsoft.Win32;
using ProxyChecker.Domain.Entity;
using ProxyChecker.Domain.Models;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;

namespace ProxyChecker.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string SelectedFilePath;
        private bool Working;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Выбор файла с прокси"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                FilePathTextBlock.Text = SelectedFilePath;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (Working)
                return;

            this.ProxyResultGrid.Items.Clear();

            Working = true;
            this.LoadFileButton.IsEnabled = false;
            this.StartButton.IsEnabled = false;

            try
            {
                var proxyList = ReadProxyFromFile();

                await CheckProxy(proxyList);
            }
            catch (Exception)
            {

            }
            finally
            {
                Working = false;
                this.LoadFileButton.IsEnabled = true;
                this.StartButton.IsEnabled = true;
            }
        }

        private List<WebProxy> ReadProxyFromFile()
        {
            var proxyUtility = new ProxyUtility(this.FormatTextBox.Text, this.ProxyTypeComboBox.Text);
            var proxies = new List<WebProxy>();

            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                MessageBox.Show("Пожалуйста, выберите файл с прокси.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                return proxies;
            }
            try
            {
                var lines = File.ReadAllLines(SelectedFilePath);
                foreach (var line in lines)
                {
                    var proxy = proxyUtility.ParseFromString(line);
                    if (proxy is not null)
                        proxies.Add(proxy);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return proxies;
        }

        private async Task CheckProxy(List<WebProxy> proxyList)
        {
            var taskList = new List<Task<ProxyResult>>(proxyList.Count);

            var url = this.TestUrlTextBox.Text;
            var timeout = long.Parse(this.TimeoutTextBox.Text);

            var semaphore = new SemaphoreSlim(10);

            var index = 1;

            foreach (var proxy in proxyList)
            {
                var task = Task.Run<ProxyResult>(async () =>
                {
                    await semaphore.WaitAsync();

                    var client = new HttpClient(new HttpClientHandler
                    {
                        Proxy = proxy,
                        UseProxy = true,
                        AllowAutoRedirect = true,
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    });

                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
                    client.Timeout = TimeSpan.FromMilliseconds(timeout);

                    var stopwatch = Stopwatch.StartNew();

                    ProxyResult result = new()
                    {
                        Index = index++,
                        Proxy = proxy.Address?.ToString(),
                    };

                    try
                    {
                        var response = await client.GetAsync(url);
                        stopwatch.Stop();
                        if (response.IsSuccessStatusCode)
                        {
                            result.Time = stopwatch.ElapsedMilliseconds;
                            result.Status = EProxyStatus.Success;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        stopwatch.Stop();
                        result.Time = stopwatch.ElapsedMilliseconds;
                        result.Status = EProxyStatus.Timeout;
                    }
                    catch (Exception ex)
                    {
                        result.Time = 1_000_000;
                        result.Status = EProxyStatus.Error;
                        result.Error = ex.Message;
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.ProxyResultGrid.Items.Add(result);
                    });

                    return result;
                });

                taskList.Add(task);
            }

            await Task.WhenAll(taskList);
        }
    }
}