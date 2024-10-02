#pragma once

#include "NetworkManager.hpp"
#include "SystemInfo.hpp"



namespace EmployeeMonitoring
{
	class MainClient
	{
	public:

		void Start();

		MainClient(const std::string& ip, int port);

		~MainClient();


	private:

		NetworkManager* networkManagerPtr;

		SystemInfo* systemInfoPtr;
	};
}