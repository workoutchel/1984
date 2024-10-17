#include "MainClient.hpp"



namespace EmployeeMonitoring
{
	MainClient::MainClient(int port_data, int port_screen, const char* ip) : _NetworkManagerPtr(new NetworkManager(port_data, port_screen, ip)), _SystemInfoPtr(new SystemInfo())
	{}

	MainClient::~MainClient() 
	{
		delete _NetworkManagerPtr;
		delete _SystemInfoPtr;
	}

	
	void MainClient::AddToStartup()
	{
		TCHAR szPath[MAX_PATH];
		GetModuleFileName(NULL, szPath, MAX_PATH);

		HKEY hKey;
		LONG result = RegOpenKeyEx(HKEY_CURRENT_USER,
			_T("Software\\Microsoft\\Windows\\CurrentVersion\\Run"),
			0,
			KEY_WRITE,
			&hKey);

		if (result == ERROR_SUCCESS)
		{
			RegSetValueEx(hKey,
				_T("MyProgram"),      
				0,
				REG_SZ,
				(uint8_t*)szPath,         
				(lstrlen(szPath) + 1) * sizeof(TCHAR));

			RegCloseKey(hKey);
		}
	}
	

	void MainClient::Start()
	{
		_SystemInfoPtr->CollectInfo();

		if (!_NetworkManagerPtr->ConnectData())
		{
			std::cerr << "Ошибка при подключению по порту:" << _NetworkManagerPtr->GetDataPort() << std::endl;
		}

		else if(!_NetworkManagerPtr->ConnectScreen())
		{
			std::cerr << "Ошибка при подключению по порту:" << _NetworkManagerPtr->GetScreenPort() << std::endl;
		}

		else
		{
			std::thread screenshotThread(&NetworkManager::WaitForScreenshotRequest, _NetworkManagerPtr);

			while (_NetworkManagerPtr->IsConnected())
			{
				if (_SystemInfoPtr->IsChanged())
				{
					_NetworkManagerPtr->SendData(_SystemInfoPtr->Serialize());
				}

				std::this_thread::sleep_for(std::chrono::milliseconds(500));
			}

			screenshotThread.join();
		}
	}
}