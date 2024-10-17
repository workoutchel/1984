#include "NetworkManager.hpp"



namespace EmployeeMonitoring
{
    NetworkManager::NetworkManager(int port_data, int port_screen, const char* ip) : _port_data(port_data), _port_screen(port_screen), _ip(ip), _sock_data(INVALID_SOCKET), _sock_screen(INVALID_SOCKET), _ScreenshotManagerPtr(new ScreenshotManager())
    {
        WSADATA wsaData;

        if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
        {
            std::cerr << WSAGetLastError() << std::endl;
            return;
        }

        _sock_data = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        _sock_screen = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    }
    
    NetworkManager::~NetworkManager()
    {
        if (_sock_data != INVALID_SOCKET)
        {
            closesocket(_sock_data);
        }

        if (_sock_screen != INVALID_SOCKET)
        {
            closesocket(_sock_screen);
        }

        WSACleanup();
    }



    bool NetworkManager::ConnectData()
    {
        struct sockaddr_in serverAddr;

        serverAddr.sin_family = AF_INET;

        serverAddr.sin_port = htons(_port_data);

        inet_pton(AF_INET, _ip, &serverAddr.sin_addr);

        return connect(_sock_data, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != SOCKET_ERROR;
    }

    bool  NetworkManager::ConnectScreen() 
    {
        struct sockaddr_in serverAddr;

        serverAddr.sin_family = AF_INET;

        serverAddr.sin_port = htons(_port_screen);

        inet_pton(AF_INET, _ip, &serverAddr.sin_addr);

        if(connect(_sock_screen, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != SOCKET_ERROR)
        {
            is_connected = true;
            return true;
        }

        return false;
    }



    void NetworkManager::WaitForScreenshotRequest()
    {
        char buffer[1024];

        while (true)
        {
            memset(buffer, 0, sizeof(buffer));

            int bytesRead = recv(_sock_screen, buffer, sizeof(buffer) - 1, 0);

            if (bytesRead > 0) 
            {
                std::string message(buffer, bytesRead);

                if (message == "SCREENSHOT_PLEASE") 
                {
                    SendScreenshot(_ScreenshotManagerPtr->CaptureScreenshot());
                }
            }
            else
            {
                std::cerr << "Ошибка получения данных по порту: " << _port_screen << std::endl;
                is_connected = false;
                break;
            }
        }
    }

    void NetworkManager::SendScreenshot(const std::vector<BYTE>& vector) const
    {
        int width = 1920;
        int height = 1080;


        if (_sock_screen == INVALID_SOCKET)
        {
            std::cerr << "Сокет по порту " << _port_screen <<" не валиден" << std::endl;
            return;
        }

        try
        {
            size_t imageSize = vector.size();

            size_t totalDataSize = sizeof(width) + sizeof(height) + sizeof(imageSize) + imageSize;
            std::vector<char> buffer(totalDataSize);

            std::memcpy(buffer.data(), &width, sizeof(width));
            std::memcpy(buffer.data() + sizeof(width), &height, sizeof(height));
            std::memcpy(buffer.data() + sizeof(width) + sizeof(height), &imageSize, sizeof(imageSize));
            std::memcpy(buffer.data() + sizeof(width) + sizeof(height) + sizeof(imageSize), vector.data(), imageSize);

            size_t totalBytesSent = 0;

            while (totalBytesSent < totalDataSize)
            {
                int bytesSent = send(_sock_screen, buffer.data() + totalBytesSent, static_cast<int>(totalDataSize - totalBytesSent), 0);
                if (bytesSent == SOCKET_ERROR)
                {
                    std::cerr << "Ошибка при отправке изображения" << std::endl;
                    return;
                }
                totalBytesSent += bytesSent;
            }
        }
        catch (const std::exception& ex)
        {
            std::cerr << "Ошибка при отправке изображения: " << ex.what() << std::endl;
        }
    }


    void NetworkManager::SendData(const std::string& data) const
    {
        send(_sock_data, data.c_str(), static_cast<int>(data.size()), 0);
    }
}











