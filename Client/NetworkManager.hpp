#pragma once

#define WIN32_LEAN_AND_MEAN
#define SECURITY_WIN32

#include "ScreenshotManager.hpp"

#include <iostream>
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <schannel.h>
#include <security.h>

#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Secur32.lib")
#pragma comment(lib, "Crypt32.lib")


namespace Client
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

        bool InitializeTlsForDataSocket();

        bool SendTlsData(const std::string& data) const;

        const char* _ip;

        bool _tlsInitialized = false;

        int _port_data;
        int _port_screen;

        std::atomic<bool> is_connected;

        CredHandle _credHandle;
        CtxtHandle _contextHandle;

        SecPkgContext_StreamSizes _streamSizes;

        SOCKET _sock_data;
        SOCKET _sock_screen;

        ScreenshotManager* _ScreenshotManagerPtr;
    };
}