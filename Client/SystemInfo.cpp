#include "SystemInfo.hpp"



namespace EmployeeMonitoring
{
	SystemInfo::SystemInfo() : _thread_flag(true), _is_changed(true)
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

		freeaddrinfo(info);

		WSACleanup();
	}

	const std::string SystemInfo::Serialize()
	{
		std::ostringstream oss;

		oss << _ip << " | "
			<< _user_name << " | "
			<< _domain_name << " | "
			<< _host_name << " | "
			<< _last_active_time;

		std::string result = oss.str();

		_is_changed = false;

		return result;
	}



	void SystemInfo::StartActivityMonitor()
	{
		_monitorThread = std::thread([this]()
			{
				while (_thread_flag)
				{
					if (IsUserActive())
					{
						std::time_t now = std::time(nullptr);

						std::tm tm;

						localtime_s(&tm, &now);

						std::ostringstream oss;

						oss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");

						_last_active_time = oss.str();

						_is_changed = true;

						std::this_thread::sleep_for(std::chrono::milliseconds(500));
					}
				}
			});
	}

	bool SystemInfo::IsUserActive()
	{
		LASTINPUTINFO last_input;

		last_input.cbSize = sizeof(LASTINPUTINFO);

		GetLastInputInfo(&last_input);

		DWORD currentTime = GetTickCount64();

		return (currentTime - last_input.dwTime) < 100;
	}
}