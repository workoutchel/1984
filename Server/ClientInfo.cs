using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Net.Security;



namespace WpfTcpServer
{
    public class ClientInfo : INotifyPropertyChanged
    {
        public string IP { get; }
        public string UserName { get; }
        public string DomainName { get; }
        public string HostName { get; }
        public string LastActiveTime { get; private set; }

        public bool IsConnected { get; private set; }

        public int WorkstationId { get; private set; }

        public string WindowTitle { get; private set; }
        public string ProcessName { get; private set; }
        public int ProcessId { get; private set; }

        public TcpClient? DataClient { get; private set; }
        public Stream? DataStream { get; private set; }

        public TcpClient? ScreenClient { get; private set; }
        public Stream? ScreenStream { get; private set; }

        private DatabaseManager? _db;

        private string _currentWindowTitle = "";
        private string _currentProcessName = "";
        private int _currentProcessId = 0;

        private DateTime _currentWindowStartTime;
        private int? _currentWindowActivityId = null;

        private const int MinWindowDurationSeconds = 10;



        public ClientInfo(
            string iP,
            string userName,
            string domainName,
            string hostName,
            string lastActiveTime,
            string windowTitle,
            string processName,
            int processId)
        {
            IP = iP;
            UserName = userName;
            DomainName = domainName;
            HostName = hostName;
            LastActiveTime = lastActiveTime;
            WindowTitle = windowTitle;
            ProcessName = processName;
            ProcessId = processId;
        }

        public static ClientInfo ParseClientInfo(string data)
        {
            string[] parts = data.Split('|');

            string ip = parts[0].Trim();
            string userName = parts[1].Trim();
            string domainName = parts[2].Trim();
            string hostName = parts[3].Trim();
            string lastActiveTime = parts[4].Trim();

            string windowTitle = parts.Length > 5 ? parts[5].Trim() : "";
            string processName = parts.Length > 6 ? parts[6].Trim() : "";
            int processId = 0;

            if (parts.Length > 7)
            {
                int.TryParse(parts[7].Trim(), out processId);
            }

            return new ClientInfo(
                ip,
                userName,
                domainName,
                hostName,
                lastActiveTime,
                windowTitle,
                processName,
                processId
            );
        }


        public void Connect(
            TcpClient tcpClientData,
            Stream dataStream,
            TcpClient tcpClientScreen,
            Stream screenStream,
            int workstationId,
            DatabaseManager db)
        {
            WorkstationId = workstationId;
            _db = db;

            DataClient = tcpClientData;
            DataStream = dataStream;

            ScreenClient = tcpClientScreen;
            ScreenStream = screenStream;

            IsConnected = true;

            OnPropertyChanged(nameof(IsConnected));
            Task.Run(() => ReceiveDataAsync());
        }

        private async Task ReceiveDataAsync()
        {
            byte[] buffer = new byte[65536];

            try
            {
                while (IsConnected && DataStream != null && DataClient != null && DataClient.Connected)
                {

                    int bytesRead = await DataStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string receivedData = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (receivedData.Contains(" | "))
                        {
                            string[] parts = receivedData.Split('|');

                            if (parts.Length > 4)
                            {
                                string lastActiveTime = parts[4].Trim();

                                string windowTitle = parts.Length > 5 ? parts[5].Trim() : "";
                                string processName = parts.Length > 6 ? parts[6].Trim() : "";

                                int processId = 0;
                                if (parts.Length > 7)
                                {
                                    int.TryParse(parts[7].Trim(), out processId);
                                }

                                string dnsCacheData = parts.Length > 8 ? parts[8].Trim() : "";

                                UpdateLastActiveTime(lastActiveTime);
                                UpdateWindowInfo(windowTitle, processName, processId);

                                if (_db != null)
                                {
                                    await _db.AddActivityEventAsync(WorkstationId, lastActiveTime);

                                    DateTime parsedTime = DateTime.ParseExact(
                                        lastActiveTime,
                                        "yyyy-MM-dd HH:mm:ss",
                                        CultureInfo.InvariantCulture
                                    );

                                    await HandleWindowActivityAsync(
                                        windowTitle,
                                        processName,
                                        processId,
                                        parsedTime
                                    );

                                    await HandleWebActivityEventAsync(
                                        windowTitle,
                                        processName,
                                        parsedTime
                                    );

                                    string webDomain = ExtractDomainFromWindowTitle(windowTitle);

                                    await HandleDnsCacheRecordsAsync(
                                        dnsCacheData,
                                        parsedTime
                                    );
                                }
                            }
                        }
                    }
                    else
                    {
                        Disconnect();
                        break;
                    }

                }
            }
            catch
            {
                Disconnect();
            }
        }

        public void UpdateLastActiveTime(string lastActiveTime)
        {
            LastActiveTime = lastActiveTime;
            OnPropertyChanged(nameof(LastActiveTime));
        }

        public async void SendScreenshotRequest()
        {
            if (ScreenStream != null && IsConnected)
            {
                try
                {
                    string message = "SCREENSHOT_PLEASE";
                    byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
                    await ScreenStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    ScreenStream.Flush();
                }
                catch
                {
                    Disconnect();
                }
            }
        }

        public async Task WaitScreenshotAsync()
        {
            Stream stream = ScreenStream;

            if (stream == null)
            {
                MessageBox.Show("TLS-поток скриншотов не инициализирован");
                return;
            }

            byte[] headerBuffer = new byte[sizeof(int) * 4];
            int bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length);

            if (bytesRead < headerBuffer.Length)
            {
                MessageBox.Show("Ошибка чтения заголовка");
                return;
            }

            int width = BitConverter.ToInt32(headerBuffer, 0);
            int height = BitConverter.ToInt32(headerBuffer, sizeof(int));
            long imageSize = BitConverter.ToInt64(headerBuffer, sizeof(int) * 2);

            byte[] imageBuffer = new byte[imageSize];
            bytesRead = 0;

            while (bytesRead < imageSize)
            {
                int read = await stream.ReadAsync(imageBuffer, bytesRead, (int)(imageSize - bytesRead));
                if (read == 0) break;
                bytesRead += read;
            }

            if (bytesRead < imageSize)
            {
                MessageBox.Show("Ошибка чтения изображения");
                return;
            }

            string screenshotsDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "screenshots",
                WorkstationId.ToString()
            );

            Directory.CreateDirectory(screenshotsDir);

            string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string filePath = Path.Combine(screenshotsDir, fileName);

            await File.WriteAllBytesAsync(filePath, imageBuffer);

            if (_db != null)
            {
                await _db.AddScreenshotAsync(WorkstationId, filePath);
            }

            try
            {
                BitmapImage bitmapImage;

                using (var ms = new MemoryStream(imageBuffer))
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }

                ShowImageWindow(bitmapImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShowImageWindow(BitmapImage bitmapImage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window window = new Window
                {
                    Title = "Скриншот",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Topmost = true
                };

                System.Windows.Controls.Image imgControl = new System.Windows.Controls.Image
                {
                    Source = bitmapImage,
                    Stretch = System.Windows.Media.Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                window.Content = imgControl;

                window.ShowDialog();
            });
        }

        public void Disconnect()
        {
            DataStream?.Close();
            DataClient?.Close();

            ScreenStream?.Close();
            ScreenClient?.Close();

            IsConnected = false;
            OnPropertyChanged(nameof(IsConnected));
        }

        public void UpdateWindowInfo(string windowTitle, string processName, int processId)
        {
            WindowTitle = windowTitle;
            ProcessName = processName;
            ProcessId = processId;

            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(ProcessName));
            OnPropertyChanged(nameof(ProcessId));
        }

        private async Task HandleWindowActivityAsync(
            string windowTitle,
            string processName,
            int processId,
            DateTime eventTime)
        {
            bool isSameWindow =
                _currentWindowTitle == windowTitle &&
                _currentProcessName == processName &&
                _currentProcessId == processId;

            if (!isSameWindow)
            {
                _currentWindowTitle = windowTitle;
                _currentProcessName = processName;
                _currentProcessId = processId;
                _currentWindowStartTime = eventTime;
                _currentWindowActivityId = null;

                return;
            }

            int durationSeconds = (int)(eventTime - _currentWindowStartTime).TotalSeconds;

            if (durationSeconds < MinWindowDurationSeconds)
                return;

            if (_db == null)
                return;

            if (_currentWindowActivityId == null)
            {
                _currentWindowActivityId = await _db.CreateWindowActivityAsync(
                    WorkstationId,
                    windowTitle,
                    processName,
                    processId,
                    _currentWindowStartTime,
                    eventTime
                );
            }
            else
            {
                await _db.UpdateWindowActivityAsync(
                    _currentWindowActivityId.Value,
                    _currentWindowStartTime,
                    eventTime
                );
            }

            string applicationRule = await _db.CheckApplicationRuleAsync(processName);

            if (applicationRule == "blacklist")
            {
                try
                {
                    await _db.AddViolationAsync(
                        WorkstationId,
                        "window_activity",
                        _currentWindowActivityId,
                        "blacklist_application",
                        "medium",
                        $"Запущено запрещённое приложение: {processName}",
                        processName,
                        "application_rules",
                        eventTime
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка записи нарушения:\n" + ex);
                }
            }
        }
        private async Task HandleWebActivityEventAsync(
            string windowTitle,
            string processName,
            DateTime eventTime)
        {
            if (_db == null)
                return;

            if (!IsBrowserProcess(processName))
                return;

            string domain = ExtractDomainFromWindowTitle(windowTitle);
            string detectionMethod = "window_title";

            if (string.IsNullOrWhiteSpace(domain))
            {
                domain = "unknown";
                detectionMethod = "window_title";
            }

            int webActivityId = await _db.AddWebActivityAsync(
                WorkstationId,
                processName,
                windowTitle,
                domain,
                detectionMethod,
                eventTime
            );

            string webRule = await _db.CheckWebResourceRuleAsync(domain);
            if (webRule == "blacklist")
            {
                await _db.AddViolationAsync(
                    WorkstationId,
                    "web_activity",
                    webActivityId,
                    "blacklist_web_resource",
                    "medium",
                    $"Посещение запрещённого веб-ресурса: {domain}",
                    domain,
                    "web_resource_rules",
                    eventTime
                );
            }
        }

        private static bool IsBrowserProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            string name = Path.GetFileName(processName).ToLower();

            return new[]
            {
                "chrome.exe",
                "msedge.exe",
                "firefox.exe",
                "opera.exe",
                "opera_gx.exe",
                "brave.exe",
                "vivaldi.exe",
                "iexplore.exe",
                "browser.exe"
            }.Contains(name);
        }

        private static string ExtractDomainFromWindowTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "";

            var match = Regex.Match(
                title.ToLower(),
                @"([a-z0-9\-]+\.[a-z]{2,})"
            );

            return match.Success ? match.Value : "";
        }

        private async Task HandleDnsCacheRecordsAsync(string dnsCacheData, DateTime recordTime)
        {
            if (_db == null)
                return;

            if (string.IsNullOrWhiteSpace(dnsCacheData))
                return;

            string[] records = dnsCacheData.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string record in records)
            {
                string[] parts = record.Split(',', 2);

                if (parts.Length == 0)
                    continue;

                string domainName = parts[0].Trim();
                string resolvedIp = parts.Length > 1 ? parts[1].Trim() : "";

                if (string.IsNullOrWhiteSpace(domainName))
                    continue;

                await _db.AddDnsCacheRecordAsync(
                    WorkstationId,
                    domainName,
                    resolvedIp,
                    recordTime
                );
            }

            await _db.RefineRecentWebActivityFromDnsAsync(
                WorkstationId,
                recordTime
            );
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}