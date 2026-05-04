using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfTcpServer
{
    public class AnalyticsWindow : Window
    {
        private readonly ClientInfo _client;
        private readonly WorkstationAnalyticsViewModel _analytics;

        public AnalyticsWindow(ClientInfo client, WorkstationAnalyticsViewModel analytics)
        {
            _client = client;
            _analytics = analytics;

            Title = $"Аналитика: {client.HostName} / {client.UserName}";
            Width = 760;
            Height = 620;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Content = BuildContent();
        }

        private ScrollViewer BuildContent()
        {
            var root = new StackPanel
            {
                Margin = new Thickness(20)
            };

            root.Children.Add(new TextBlock
            {
                Text = $"Рабочая станция: {_client.HostName}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            root.Children.Add(new TextBlock
            {
                Text = $"Пользователь: {_client.UserName} | IP: {_client.IP}",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 16)
            });

            root.Children.Add(CreateSummaryGrid());

            root.Children.Add(CreateSectionTitle("Соотношение активности и простоя"));
            root.Children.Add(CreateProgressRow("Активность", _analytics.ActivePercent, $"{_analytics.ActivePercent}%"));
            root.Children.Add(CreateProgressRow("Простой", _analytics.IdlePercent, $"{_analytics.IdlePercent}%"));

            root.Children.Add(CreateSectionTitle("Топ приложений за сегодня"));

            if (_analytics.TopApplications.Count == 0)
            {
                root.Children.Add(CreateEmptyText("Нет данных по приложениям."));
            }
            else
            {
                int maxAppSeconds = Math.Max(1, _analytics.TopApplications.Max(x => x.DurationSeconds));

                foreach (var app in _analytics.TopApplications)
                {
                    double percent = (double)app.DurationSeconds / maxAppSeconds * 100;
                    root.Children.Add(CreateProgressRow(app.ProcessName, percent, app.DurationText));
                }
            }

            root.Children.Add(CreateSectionTitle("Активность по часам"));

            if (_analytics.HourlyActivity.Count == 0)
            {
                root.Children.Add(CreateEmptyText("Нет данных по часовой активности."));
            }
            else
            {
                int maxHourSeconds = Math.Max(1, _analytics.HourlyActivity.Max(x => x.ActiveSeconds));

                foreach (var hour in _analytics.HourlyActivity)
                {
                    double percent = (double)hour.ActiveSeconds / maxHourSeconds * 100;
                    root.Children.Add(CreateProgressRow(hour.HourText, percent, hour.DurationText));
                }
            }

            return new ScrollViewer
            {
                Content = root,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private Grid CreateSummaryGrid()
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 18)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            AddSummaryCard(grid, "Активность", ApplicationUsageViewModel.FormatDuration(_analytics.ActiveSeconds), 0, 0);
            AddSummaryCard(grid, "Простой", ApplicationUsageViewModel.FormatDuration(_analytics.IdleSeconds), 0, 1);
            AddSummaryCard(grid, "Процент простоя", $"{_analytics.IdlePercent}%", 0, 2);
            AddSummaryCard(grid, "Веб-события", _analytics.WebVisits.ToString(), 1, 0);
            AddSummaryCard(grid, "Нарушения", _analytics.Violations.ToString(), 1, 1);
            AddSummaryCard(grid, "Всего учтено", ApplicationUsageViewModel.FormatDuration(_analytics.TotalSeconds), 1, 2);

            return grid;
        }

        private void AddSummaryCard(Grid grid, string title, string value, int row, int column)
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(4)
            };

            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                Foreground = Brushes.Gray
            });

            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 0)
            });

            border.Child = panel;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);

            grid.Children.Add(border);
        }

        private TextBlock CreateSectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 18, 0, 8)
            };
        }

        private TextBlock CreateEmptyText(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        private Grid CreateProgressRow(string label, double value, string rightText)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 4, 0, 4)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var progress = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = value,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center
            };

            var valueBlock = new TextBlock
            {
                Text = rightText,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(progress, 1);
            Grid.SetColumn(valueBlock, 2);

            grid.Children.Add(labelBlock);
            grid.Children.Add(progress);
            grid.Children.Add(valueBlock);

            return grid;
        }
    }
}