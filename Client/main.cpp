#include "MainClient.hpp"



int main()
{
	///////////////////////////////////////////////////////////////////////
	//
	//					  СЮДА НЕОБХОДИМО ПОДСТАВИТЬ ЛОКАЛЬНЫЙ АЙПИ СЕРВЕРА
	//											|
	//											↓
	EmployeeMonitoring::MainClient client("192.168.1.70", 1337);

	client.Start();
}