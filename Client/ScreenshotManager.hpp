#pragma once
#include <vector>
#include <objidl.h>
#include <GdiPlus.h>

#pragma comment(lib, "GdiPlus.lib")



namespace EmployeeMonitoring
{

	class ScreenshotManager
	{
	public:

		ScreenshotManager();

		~ScreenshotManager();

		const std::vector<uint8_t> CaptureScreenshot();

	private:

		int GetEncoderClsid(const WCHAR* format, CLSID* pClsid);

		ULONG_PTR gdiplusToken;
	};
}