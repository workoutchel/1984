#pragma once

#include <iostream>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "Ws2_32.lib")



namespace EmployeeMonitoring
{
    class NetworkManager
    {
    public:

        NetworkManager(const std::string& ip, int port);

        ~NetworkManager();

        bool Connect();

        void SendData(const std::string& data);

    private:

        std::string _ip;

        int _port;

        SOCKET _sock;
    };
}