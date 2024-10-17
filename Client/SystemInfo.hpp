#pragma once

#define WIN32_LEAN_AND_MEAN

#include <atomic>
#include <iomanip>
#include <iostream>
#include <Lmcons.h.> 
#include <sstream>
#include <string>
#include <thread>
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "Ws2_32.lib")



namespace EmployeeMonitoring
{
	class SystemInfo
	{
	public:

		SystemInfo();

		~SystemInfo();

		void CollectInfo();

		const std::string Serialize();

		inline bool IsChanged()
		{
			return _is_changed;
		}

	private:

		void SetNowTime();

		void StartActivityMonitor();

		bool IsUserActive();



		std::thread  _monitorThread;

		std::atomic<bool> _thread_flag;

		std::atomic<bool> _is_changed;

		std::string _ip;

		std::string _domain_name;

		std::string _last_active_time;

		std::string _user_name;

		std::string _host_name;
	};
}