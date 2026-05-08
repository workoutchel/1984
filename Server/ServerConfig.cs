using System.IO;

namespace WpfTcpServer
{
    public class ServerConfig
    {
        private readonly Dictionary<string, string> _values = new();

        public static ServerConfig Load(string fileName = "server_config.ini")
        {
            string configPath = FindConfigFile(fileName);

            var config = new ServerConfig();

            foreach (string line in File.ReadAllLines(configPath))
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                    continue;

                int separatorIndex = trimmedLine.IndexOf('=');

                if (separatorIndex < 0)
                    continue;

                string key = trimmedLine.Substring(0, separatorIndex).Trim();
                string value = trimmedLine.Substring(separatorIndex + 1).Trim();

                config._values[key] = value;
            }

            return config;
        }

        public string GetRequiredString(string key)
        {
            if (!_values.TryGetValue(key, out string? value))
            {
                throw new InvalidOperationException(
                    $"В файле конфигурации отсутствует обязательный параметр: {key}"
                );
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Параметр конфигурации не должен быть пустым: {key}"
                );
            }

            return value;
        }

        private static string FindConfigFile(string fileName)
        {
            string? currentDirectory = AppContext.BaseDirectory;

            while (!string.IsNullOrWhiteSpace(currentDirectory))
            {
                string candidatePath = Path.Combine(currentDirectory, fileName);

                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            throw new FileNotFoundException(
                $"Файл конфигурации сервера не найден: {fileName}"
            );
        }

        public int GetRequiredInt(string key)
        {
            string value = GetRequiredString(key);

            if (!int.TryParse(value, out int result))
            {
                throw new InvalidOperationException(
                    $"Параметр конфигурации должен быть целым числом: {key}"
                );
            }

            return result;
        }
    }
}