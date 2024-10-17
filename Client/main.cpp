#include "MainClient.hpp"


////////////////////////////////////////////////////////////////////////////////////////////////////
//
//	Если необходимо включить отображение консоли для отладки, необходимо строку 
//	int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
//	поменять на int main()
//	Затем выбрать Client -> Свойства -> Компоновщик -> Система -> Подсистема
//	Параметр Windows (/SUBSYSTEM:WINDOWS) 
//	Поменять на Консоль (/SUBSYSTEM:CONSOLE)
//	
_Use_decl_annotations_
int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	while (true)
	{
		////////////////////////////////////////////////////////////////////////////////////////////////////
		//
		//	Вставить сюда необходимые порты(один для отслеживания активности, второй для передачи скриншота) и IP сервера в локальной сети
		//										   |
		//										   ↓
		EmployeeMonitoring::MainClient client(1337, 1338, "");

		client.AddToStartup();

		client.Start();
	}
}



