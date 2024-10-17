using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Server
{
    public class ClientInfo : INotifyPropertyChanged
    {
        public string IP { get; }
        public string UserName { get; }
        public string DomainName { get; }
        public string HostName { get; }
        public string LastActiveTime { get; private set; }

        public bool IsConnected { get; private set; }


        public TcpClient? DataClient { get; private set; }
        public NetworkStream? DataStream { get; private set; }

        public TcpClient? ScreenClient { get; private set; }
        public NetworkStream? ScreenStream { get; private set; }



        public ClientInfo(string iP, string userName, string domainName, string hostName, string lastActiveTime)
        {
            IP = iP;
            UserName = userName;
            DomainName = domainName;
            HostName = hostName;
            LastActiveTime = lastActiveTime;
        }

        public static ClientInfo ParseClientInfo(string data)
        {
            string[] parts = data.Split('|');

            return new ClientInfo(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim(), parts[4].Trim());
        }


        public void Connect(TcpClient tcpClientData, TcpClient tcpClientScreen)
        {
            DataClient = tcpClientData;
            DataStream = DataClient.GetStream();

            ScreenClient = tcpClientScreen;
            ScreenStream = tcpClientScreen.GetStream();

            IsConnected = true;

            OnPropertyChanged(nameof(IsConnected));
            Task.Run(() => ReceiveDataAsync());
        }

        private async Task ReceiveDataAsync()
        {
            byte[] buffer = new byte[1024];

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
                                UpdateLastActiveTime(parts[4].Trim());
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
            NetworkStream stream = ScreenClient.GetStream();

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



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
