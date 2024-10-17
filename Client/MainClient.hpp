#pragma once

#include "NetworkManager.hpp"
#include "SystemInfo.hpp"
#include <tchar.h> 



namespace EmployeeMonitoring
{
	class MainClient
	{
	public:

		MainClient(int port_data, int port_screen, const char* ip);

		~MainClient();

		void AddToStartup();

		void Start();

	private:

		NetworkManager* _NetworkManagerPtr;

		SystemInfo* _SystemInfoPtr;
	};
}