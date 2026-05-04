using WpfTcpServer;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;



namespace WpfTcpServer
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            }

            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }       
    }



    public partial class MainWindow : Window
    {
        private ObservableCollection<ClientInfo> _clients = new ObservableCollection<ClientInfo>();
        private ObservableCollection<RuleViewModel> _applicationRules = new();
        private ObservableCollection<RuleViewModel> _webRules = new();

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly DatabaseManager _db = new DatabaseManager();

        private TcpListener? ListenerData;
        private TcpListener? ListenerScreen;

        private X509Certificate2 _serverCertificate = new X509Certificate2(@"C:\certs\server.pfx", "12345678");

        public MainWindow()
        {
            InitializeComponent();
            ClientsListView.ItemsSource = _clients;
            ApplicationRulesDataGrid.ItemsSource = _applicationRules;
            WebRulesDataGrid.ItemsSource = _webRules;

            ApplicationRuleTypeComboBox.SelectedIndex = 0;
            WebRuleTypeComboBox.SelectedIndex = 0;

            _ = LoadRulesAsync();

            StartServer(1337, 1338);
        }
    



        private void StartServer(int portData, int portScreen)
        {
            ListenerData = new TcpListener(IPAddress.Any, portData);
            ListenerData.Start();

            ListenerScreen = new TcpListener(IPAddress.Any, portScreen);
            ListenerScreen.Start();

            Task.Run(() => ListenForClients(_cancellationTokenSource.Token));
        }

        private void AddOrUpdateClientInfo(string data)
        {
            var newClientInfo = ClientInfo.ParseClientInfo(data);

            if (newClientInfo != null)
            {
                Dispatcher.Invoke(() =>
                {
                    var existingClient = _clients.FirstOrDefault(client => client.IP == newClientInfo.IP);

                    if (existingClient != null)
                    {
                        existingClient.UpdateLastActiveTime(newClientInfo.LastActiveTime);
                    }
                    else
                    {
                        _clients.Add(newClientInfo);
                    }
                });
            }
        }

        private async Task ListenForClients(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpClientData = await ListenerData.AcceptTcpClientAsync(cancellationToken);

                    SslStream sslDataStream = new SslStream(
                        tcpClientData.GetStream(),
                        false
                    );

                    await sslDataStream.AuthenticateAsServerAsync(
                        _serverCertificate,
                        clientCertificateRequired: false,
                        enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12,
                        checkCertificateRevocation: false
                    );

                    TcpClient tcpClientScreen = await ListenerScreen.AcceptTcpClientAsync(cancellationToken);

                    SslStream sslScreenStream = new SslStream(
                        tcpClientScreen.GetStream(),
                        false
                    );

                    await sslScreenStream.AuthenticateAsServerAsync(
                        _serverCertificate,
                        clientCertificateRequired: false,
                        enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12,
                        checkCertificateRevocation: false
                    );

                    string clientData = await ReadClientDataAsync(sslDataStream);

                    ClientInfo clientInfo = ClientInfo.ParseClientInfo(clientData);

                    int workstationId = await _db.AddOrUpdateWorkstationAsync(clientInfo);
                    await _db.AddActivityEventAsync(workstationId, clientInfo.LastActiveTime);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var existingClient = _clients.FirstOrDefault(c => c.IP == clientInfo.IP);

                        if (existingClient != null)
                        {
                            existingClient.Connect(tcpClientData, sslDataStream, tcpClientScreen, sslScreenStream, workstationId, _db);
                        }
                        else
                        {
                            clientInfo.Connect(tcpClientData, sslDataStream, tcpClientScreen, sslScreenStream, workstationId, _db);
                            _clients.Add(clientInfo);
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                    if (cancellationToken.IsCancellationRequested) break;
                }
            }
        }

        private static async Task<string> ReadClientDataAsync(SslStream stream)
        {
            byte[] buffer = new byte[65536];

            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }



        private void RequestScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var clientIP = button?.Tag as string;

            ClientInfo client = _clients.FirstOrDefault(x => x.IP == clientIP);

            if (client != null && client.IsConnected)
            {
                Task.Run(() => client.WaitScreenshotAsync());

                client.SendScreenshotRequest();
            }
        }

        private async Task LoadRulesAsync()
        {
            _applicationRules.Clear();
            _webRules.Clear();

            var appRules = await _db.LoadApplicationRulesAsync();
            var webRules = await _db.LoadWebResourceRulesAsync();

            foreach (var rule in appRules)
                _applicationRules.Add(rule);

            foreach (var rule in webRules)
                _webRules.Add(rule);
        }

        private async void AddApplicationRuleButton_Click(object sender, RoutedEventArgs e)
        {
            string value = ApplicationRuleTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("Введите имя приложения.");
                return;
            }

            string ruleType = ((ComboBoxItem)ApplicationRuleTypeComboBox.SelectedItem).Content.ToString();

            await _db.AddApplicationRuleAsync(value, ruleType);

            ApplicationRuleTextBox.Clear();
            await LoadRulesAsync();
        }

        private async void DeleteApplicationRuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationRulesDataGrid.SelectedItem is not RuleViewModel selectedRule)
            {
                MessageBox.Show("Выберите правило приложения.");
                return;
            }

            await _db.DeleteApplicationRuleAsync(selectedRule.Id);
            await LoadRulesAsync();
        }

        private async void AddWebRuleButton_Click(object sender, RoutedEventArgs e)
        {
            string value = WebRuleTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("Введите домен.");
                return;
            }

            string ruleType = ((ComboBoxItem)WebRuleTypeComboBox.SelectedItem).Content.ToString();

            await _db.AddWebResourceRuleAsync(value, ruleType);

            WebRuleTextBox.Clear();
            await LoadRulesAsync();
        }

        private async void DeleteWebRuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebRulesDataGrid.SelectedItem is not RuleViewModel selectedRule)
            {
                MessageBox.Show("Выберите правило сайта.");
                return;
            }

            await _db.DeleteWebResourceRuleAsync(selectedRule.Id);
            await LoadRulesAsync();
        }

        private async void ApplicationRulesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is not RuleViewModel selectedRule)
                return;

            if (e.EditingElement is CheckBox checkBox)
            {
                bool newValue = checkBox.IsChecked == true;

                await _db.UpdateApplicationRuleActiveAsync(
                    selectedRule.Id,
                    newValue
                );

                selectedRule.IsActive = newValue;
            }
        }

        private async void WebRulesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is not RuleViewModel selectedRule)
                return;

            if (e.EditingElement is CheckBox checkBox)
            {
                bool newValue = checkBox.IsChecked == true;

                await _db.UpdateWebResourceRuleActiveAsync(
                    selectedRule.Id,
                    newValue
                );

                selectedRule.IsActive = newValue;
            }
        }
        private async void AnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not ClientInfo client)
                return;

            if (client.WorkstationId <= 0)
            {
                MessageBox.Show("Для клиента не найден идентификатор рабочей станции.");
                return;
            }

            try
            {
                var analytics = await _db.GetWorkstationAnalyticsAsync(client.WorkstationId);

                var window = new AnalyticsWindow(client, analytics, _db);
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки аналитики:\n" + ex);
            }
        }
    }


    public class RuleViewModel
    {
        public int Id { get; set; }
        public string Value { get; set; } = "";
        public string RuleType { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}