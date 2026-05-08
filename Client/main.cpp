#include "MainClient.hpp"

#include <fstream>
#include <string>
#include <unordered_map>
#include <filesystem>
#include <stdexcept>

namespace
{
    constexpr const char* CONFIG_FILE_PATH = "client_config.ini";
    constexpr const char* DEFAULT_SERVER_IP = "127.0.0.1";

    constexpr int DEFAULT_DATA_PORT = 1337;
    constexpr int DEFAULT_SCREEN_PORT = 1338;

    constexpr const char* SERVER_IP_KEY = "server_ip";
    constexpr const char* DATA_PORT_KEY = "data_port";
    constexpr const char* SCREEN_PORT_KEY = "screen_port";

    std::filesystem::path FindClientConfigFile(const std::string& fileName)
    {
        std::filesystem::path currentPath = std::filesystem::current_path();

        while (!currentPath.empty())
        {
            std::filesystem::path directCandidate = currentPath / fileName;

            if (std::filesystem::exists(directCandidate))
            {
                return directCandidate;
            }

            std::filesystem::path clientCandidate = currentPath / "Client" / fileName;

            if (std::filesystem::exists(clientCandidate))
            {
                return clientCandidate;
            }

            std::filesystem::path parentPath = currentPath.parent_path();

            if (parentPath == currentPath)
            {
                break;
            }

            currentPath = parentPath;
        }

        throw std::runtime_error("Файл конфигурации клиента не найден: " + fileName);
    }

    std::unordered_map<std::string, std::string> LoadConfig(
        const std::filesystem::path& path)
    {
        std::unordered_map<std::string, std::string> config;

        std::ifstream file(path);

        if (!file.is_open())
        {
            return config;
        }

        std::string line;

        while (std::getline(file, line))
        {
            size_t pos = line.find('=');

            if (pos == std::string::npos)
                continue;

            std::string key = line.substr(0, pos);
            std::string value = line.substr(pos + 1);

            config[key] = value;
        }

        return config;
    }

    int GetConfigIntValue(
        const std::unordered_map<std::string, std::string>& config,
        const std::string& key,
        int defaultValue)
    {
        const auto iterator = config.find(key);

        if (iterator == config.end())
        {
            return defaultValue;
        }

        return std::stoi(iterator->second);
    }

    std::string GetConfigStringValue(
        const std::unordered_map<std::string, std::string>& config,
        const std::string& key,
        const std::string& defaultValue)
    {
        const auto iterator = config.find(key);

        if (iterator == config.end())
        {
            return defaultValue;
        }

        return iterator->second;
    }
}

//	Если необходимо включить отображение консоли для отладки, необходимо строку 
//	int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
//	поменять на int main()
//	Затем выбрать Client -> Свойства -> Компоновщик -> Система -> Подсистема
//	Параметр Windows (/SUBSYSTEM:WINDOWS) 
//	Поменять на Консоль (/SUBSYSTEM:CONSOLE)
_Use_decl_annotations_
int APIENTRY WinMain(
    HINSTANCE hInstance,
    HINSTANCE hPrevInstance,
    LPSTR lpCmdLine,
    int nCmdShow)
{
    const std::filesystem::path configPath = FindClientConfigFile(CONFIG_FILE_PATH);
    const auto config = LoadConfig(configPath);

    const std::string serverIp = GetConfigStringValue(
        config,
        SERVER_IP_KEY,
        DEFAULT_SERVER_IP
    );

    const int dataPort = GetConfigIntValue(
        config,
        DATA_PORT_KEY,
        DEFAULT_DATA_PORT
    );

    const int screenPort = GetConfigIntValue(
        config,
        SCREEN_PORT_KEY,
        DEFAULT_SCREEN_PORT
    );

    while (true)
    {
        Client::MainClient client(
            dataPort,
            screenPort,
            serverIp.c_str()
        );

        client.Start();
    }
}