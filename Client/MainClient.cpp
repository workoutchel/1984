#include "MainClient.hpp"



namespace EmployeeMonitoring
{
	MainClient::MainClient(const std::string& ip, int port) :networkManagerPtr(nullptr), systemInfoPtr(nullptr)
	{
		networkManagerPtr = new NetworkManager(ip, port);

		systemInfoPtr = new SystemInfo();
	}

	MainClient::~MainClient()
	{
		delete networkManagerPtr;

		delete systemInfoPtr;
	}

	void MainClient::Start()
	{
		systemInfoPtr->CollectInfo();

		//networkManagerPtr->Connect();

		while (true)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(500));

			if (systemInfoPtr->IsChanged())
			{
				networkManagerPtr->Connect();

				networkManagerPtr->SendData(systemInfoPtr->Serialize());
			}
		}
	}
}













