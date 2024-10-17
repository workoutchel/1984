#pragma once

#define WIN32_LEAN_AND_MEAN

#include "ScreenshotManager.hpp"

#include <iostream>
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "Ws2_32.lib")


namespace EmployeeMonitoring
{
    class NetworkManager
    {
    public:

        NetworkManager(int port_data, int port_screen, const char* ip);

        ~NetworkManager();


        bool ConnectData();

        bool ConnectScreen();

        inline int GetDataPort() const { return _port_data; }

        inline int GetScreenPort() const { return _port_screen; }

        inline bool IsConnected() { return is_connected.load(); }

        void SendData(const std::string& data) const;

        void WaitForScreenshotRequest();

        void SendScreenshot(const std::vector<BYTE>& vector) const;

    private:

        std::atomic<bool> is_connected;

        const char* _ip;


        int _port_data;

        SOCKET _sock_data;

        int _port_screen;

        SOCKET _sock_screen;

        ScreenshotManager* _ScreenshotManagerPtr;
    };
}