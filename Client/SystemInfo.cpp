#include "SystemInfo.hpp"

#include <filesystem>
#include <vector>

namespace Client
{
    SystemInfo::SystemInfo()
        : _thread_flag(true),
        _is_changed(true),
        _last_dns_collect_time(std::chrono::steady_clock::now() - std::chrono::seconds(30))
    {
        StartActivityMonitor();
    }

    SystemInfo::~SystemInfo()
    {
        _thread_flag = false;

        if (_monitorThread.joinable())
        {
            _monitorThread.join();
        }
    }

    void SystemInfo::CollectInfo()
    {
        char buffer[UNLEN + 1];
        DWORD size = sizeof(buffer);

        GetUserNameA(buffer, &size);

        _user_name = buffer;

        WSADATA wsaData;

        if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
        {
            std::cerr << WSAGetLastError() << std::endl;
            return;
        }

        char hostname[UNLEN + 1];

        if (gethostname(hostname, sizeof(hostname)) == SOCKET_ERROR)
        {
            std::cerr << WSAGetLastError() << std::endl;
            return;
        }

        _host_name = (std::string)hostname;

        struct addrinfo hints, * info;

        ZeroMemory(&hints, sizeof(hints));

        hints.ai_family = AF_INET;
        hints.ai_socktype = SOCK_STREAM;
        hints.ai_flags = AI_CANONNAME;

        if (getaddrinfo(hostname, NULL, &hints, &info) != 0)
        {
            std::cerr << WSAGetLastError() << std::endl;
            return;
        }

        _domain_name = reinterpret_cast<char*>(info->ai_canonname);

        for (struct addrinfo* p = info; p != nullptr; p = p->ai_next)
        {
            if (p->ai_family == AF_INET)
            {
                struct sockaddr_in* sockaddr_ipv4 = (struct sockaddr_in*)p->ai_addr;

                char ip[INET_ADDRSTRLEN];

                inet_ntop(AF_INET, &(sockaddr_ipv4->sin_addr), ip, sizeof(ip));

                _ip = ip;
                break;
            }
        }

        SetNowTime();

        freeaddrinfo(info);
        WSACleanup();
    }

    std::string SystemInfo::Serialize()
    {
        UpdateDnsCacheInfo();

        std::ostringstream oss;

        oss << _ip << " | "
            << _user_name << " | "
            << _domain_name << " | "
            << _host_name << " | "
            << _last_active_time << " | "
            << _window_title << " | "
            << _process_name << " | "
            << _process_id << " | "
            << _dns_cache_records;

        std::string result = oss.str();

        _is_changed = false;

        return result;
    }

    void SystemInfo::SetNowTime()
    {
        std::time_t now = std::time(nullptr);
        std::tm tm;

        localtime_s(&tm, &now);

        std::ostringstream oss;
        oss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");

        _last_active_time = oss.str();
    }

    void SystemInfo::StartActivityMonitor()
    {
        _monitorThread = std::thread([this]()
            {
                while (_thread_flag)
                {
                    if (IsUserActive())
                    {
                        SetNowTime();
                        UpdateActiveWindowInfo();

                        _is_changed = true;
                    }
                    else
                    {
                        UpdateActiveWindowInfo();
                    }

                    std::this_thread::sleep_for(std::chrono::milliseconds(500));
                }
            });
    }

    bool SystemInfo::IsUserActive()
    {
        LASTINPUTINFO last_input;
        last_input.cbSize = sizeof(LASTINPUTINFO);

        GetLastInputInfo(&last_input);

        ULONGLONG currentTime = GetTickCount64();
        ULONGLONG lastInputTime = last_input.dwTime;

        return (currentTime - lastInputTime) < 500;
    }

    std::string SystemInfo::WideToUtf8(const std::wstring& wstr)
    {
        if (wstr.empty())
            return "";

        int sizeNeeded = WideCharToMultiByte(
            CP_UTF8,
            0,
            wstr.c_str(),
            -1,
            nullptr,
            0,
            nullptr,
            nullptr
        );

        std::string result(sizeNeeded - 1, 0);

        WideCharToMultiByte(
            CP_UTF8,
            0,
            wstr.c_str(),
            -1,
            result.data(),
            sizeNeeded,
            nullptr,
            nullptr
        );

        return result;
    }

    void SystemInfo::UpdateActiveWindowInfo()
    {
        HWND hwnd = GetForegroundWindow();

        if (hwnd == NULL)
            return;

        wchar_t windowTitle[512];
        GetWindowTextW(hwnd, windowTitle, 512);

        DWORD processId = 0;
        GetWindowThreadProcessId(hwnd, &processId);

        HANDLE hProcess = OpenProcess(
            PROCESS_QUERY_LIMITED_INFORMATION,
            FALSE,
            processId
        );

        std::string processName = "unknown";

        if (hProcess != NULL)
        {
            char processPath[MAX_PATH];
            DWORD size = MAX_PATH;

            if (QueryFullProcessImageNameA(hProcess, 0, processPath, &size))
            {
                processName = std::filesystem::path(processPath).filename().string();
            }

            CloseHandle(hProcess);
        }

        std::string newWindowTitle = SanitizeField(WideToUtf8(windowTitle));
        std::string newProcessName = SanitizeField(processName);

        if (_window_title != newWindowTitle ||
            _process_name != newProcessName ||
            _process_id != processId)
        {
            _window_title = newWindowTitle;
            _process_name = newProcessName;
            _process_id = processId;

            _is_changed = true;
        }
    }

    std::string SystemInfo::SanitizeField(const std::string& value)
    {
        std::string result = value;

        for (char& ch : result)
        {
            if (ch == '|')
                ch = ' ';
        }

        return result;
    }

    std::string SystemInfo::ExecuteCommandUtf8(const std::string& command)
    {
        SECURITY_ATTRIBUTES sa;

        sa.nLength = sizeof(SECURITY_ATTRIBUTES);
        sa.bInheritHandle = TRUE;
        sa.lpSecurityDescriptor = NULL;

        HANDLE readPipe = NULL;
        HANDLE writePipe = NULL;

        if (!CreatePipe(&readPipe, &writePipe, &sa, 0))
            return "";

        SetHandleInformation(readPipe, HANDLE_FLAG_INHERIT, 0);

        STARTUPINFOA si;
        PROCESS_INFORMATION pi;

        ZeroMemory(&si, sizeof(si));
        ZeroMemory(&pi, sizeof(pi));

        si.cb = sizeof(si);
        si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
        si.hStdOutput = writePipe;
        si.hStdError = writePipe;
        si.wShowWindow = SW_HIDE;

        std::string cmdPath = "C:\\Windows\\System32\\cmd.exe";
        std::string commandLine = "\"" + cmdPath + "\" /C " + command;

        std::vector<char> commandLineBuffer(commandLine.begin(), commandLine.end());
        commandLineBuffer.push_back('\0');

        BOOL success = CreateProcessA(
            cmdPath.c_str(),
            commandLineBuffer.data(),
            NULL,
            NULL,
            TRUE,
            CREATE_NO_WINDOW,
            NULL,
            NULL,
            &si,
            &pi
        );

        CloseHandle(writePipe);

        if (!success)
        {
            CloseHandle(readPipe);
            return "";
        }

        std::string result;
        char buffer[4096];
        DWORD bytesRead = 0;

        while (ReadFile(readPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL) && bytesRead > 0)
        {
            buffer[bytesRead] = '\0';
            result += buffer;
        }

        WaitForSingleObject(pi.hProcess, 3000);

        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        CloseHandle(readPipe);

        return result;
    }

    std::string SystemInfo::SanitizeDnsField(const std::string& value)
    {
        std::string result = value;

        for (char& ch : result)
        {
            if (ch == '|' || ch == ';' || ch == ',')
                ch = ' ';
        }

        return result;
    }

    void SystemInfo::UpdateDnsCacheInfo()
    {
        auto now = std::chrono::steady_clock::now();

        auto secondsPassed = std::chrono::duration_cast<std::chrono::seconds>(
            now - _last_dns_collect_time
        ).count();

        if (secondsPassed < 30)
            return;

        _last_dns_collect_time = now;

        std::string output = ExecuteCommandUtf8("chcp 65001 > nul & ipconfig /displaydns");

        if (output.empty())
            return;

        auto Trim = [](std::string value)
            {
                while (!value.empty() &&
                    (value.front() == ' ' ||
                        value.front() == '\t' ||
                        value.front() == '\r' ||
                        value.front() == '\n'))
                {
                    value.erase(value.begin());
                }

                while (!value.empty() &&
                    (value.back() == ' ' ||
                        value.back() == '\t' ||
                        value.back() == '\r' ||
                        value.back() == '\n'))
                {
                    value.pop_back();
                }

                return value;
            };

        std::istringstream stream(output);
        std::string line;

        std::vector<std::string> records;

        std::string currentDomain;
        bool currentRecordIsA = false;

        std::regex domainRegex(R"(^([a-zA-Z0-9\-]+\.)+[a-zA-Z]{2,}\.?$)");
        std::regex ipv4Regex(R"((\d{1,3}\.){3}\d{1,3})");

        while (std::getline(stream, line))
        {
            line = Trim(line);

            if (line.find("Record Name") != std::string::npos)
            {
                size_t pos = line.find(":");

                if (pos != std::string::npos)
                {
                    std::string domain = Trim(line.substr(pos + 1));

                    if (!domain.empty() && domain.back() == '.')
                        domain.pop_back();

                    if (std::regex_match(domain, domainRegex) &&
                        domain.find("in-addr.arpa") == std::string::npos &&
                        domain.find("ip6.arpa") == std::string::npos)
                    {
                        currentDomain = SanitizeDnsField(domain);
                    }
                    else
                    {
                        currentDomain.clear();
                    }

                    currentRecordIsA = false;
                }
            }
            else if (line.find("Record Type") != std::string::npos)
            {
                currentRecordIsA = line.find(": 1") != std::string::npos;
            }
            else if (line.find("A (Host) Record") != std::string::npos)
            {
                if (currentDomain.empty())
                    continue;

                std::smatch ipMatch;

                if (std::regex_search(line, ipMatch, ipv4Regex))
                {
                    std::string ip = SanitizeDnsField(ipMatch.str());
                    records.push_back(currentDomain + "," + ip);
                }

                currentDomain.clear();
                currentRecordIsA = false;
            }
        }

        std::ostringstream result;

        for (size_t i = 0; i < records.size() && i < 100; ++i)
        {
            if (i > 0)
                result << ";";

            result << records[i];
        }

        _dns_cache_records = result.str();
    }
}