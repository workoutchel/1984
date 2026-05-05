#include "MainClient.hpp"


// tcp-порты
#define PORT_DATA 1337
#define PORT_SCREEN 1338
// ip сервера
#define SERVER_IP "10.66.66.2"


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
		Client::MainClient client(PORT_DATA, PORT_SCREEN, SERVER_IP);

		client.Start();
	}
}