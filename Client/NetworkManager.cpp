#include "NetworkManager.hpp"



namespace EmployeeMonitoring
{
    NetworkManager::NetworkManager(const std::string& ip, int port) : _ip(ip), _port(port), _sock(INVALID_SOCKET)
    {
        WSADATA wsaData;

        if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
        {
            std::cerr << WSAGetLastError() << std::endl;
            return;
        }
        _sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    }

    NetworkManager::~NetworkManager()
    {
        if (_sock != INVALID_SOCKET)
        {
            closesocket(_sock);
        }

        WSACleanup();
    }

    bool NetworkManager::Connect()
    {
        struct sockaddr_in serverAddr;

        serverAddr.sin_family = AF_INET;

        serverAddr.sin_port = htons(_port);

        inet_pton(AF_INET, _ip.c_str(), &serverAddr.sin_addr);

        return connect(_sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != SOCKET_ERROR;
    }

    void NetworkManager::SendData(const std::string& data)
    {
        send(_sock, data.c_str(), data.size(), 0);
    }
}











