#pragma once

#define WIN32_LEAN_AND_MEAN

#include <atomic>
#include <chrono>
#include <iomanip>
#include <iostream>
#include <Lmcons.h>
#include <regex>
#include <sstream>
#include <string>
#include <thread>
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "Ws2_32.lib")

namespace Client
{
    class SystemInfo
    {
    public:
        SystemInfo();
        ~SystemInfo();

        void CollectInfo();
        std::string Serialize();

        inline bool IsChanged()
        {
            return _is_changed;
        }

    private:
        void SetNowTime();
        void StartActivityMonitor();
        void UpdateActiveWindowInfo();
        void UpdateDnsCacheInfo();

        bool IsUserActive();

        std::string SanitizeField(const std::string& value);
        std::string WideToUtf8(const std::wstring& wstr);
        std::string ExecuteCommandUtf8(const std::string& command);
        std::string SanitizeDnsField(const std::string& value);

        std::chrono::steady_clock::time_point _last_dns_collect_time;

        std::thread _monitorThread;

        std::atomic<bool> _thread_flag;
        std::atomic<bool> _is_changed;

        DWORD _process_id = 0;

        std::string _ip;
        std::string _domain_name;
        std::string _last_active_time;
        std::string _user_name;
        std::string _host_name;
        std::string _window_title;
        std::string _process_name;
        std::string _dns_cache_records;
    };
}