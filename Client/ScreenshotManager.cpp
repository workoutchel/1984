#include "ScreenshotManager.hpp"



namespace EmployeeMonitoring
{
	ScreenshotManager::ScreenshotManager() 
	{
		Gdiplus::GdiplusStartupInput gdiplusStartupInput;
		Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);
	}

	ScreenshotManager::~ScreenshotManager()
	{
		Gdiplus::GdiplusShutdown(gdiplusToken);
	}



    int ScreenshotManager::GetEncoderClsid(const WCHAR* format, CLSID* pClsid)
    {
        uint32_t num = 0;
        uint32_t size = 0;

        Gdiplus::ImageCodecInfo* pImageCodecInfo = NULL;

        Gdiplus::GetImageEncodersSize(&num, &size);

        if (size == 0)
            return -1;

        pImageCodecInfo = (Gdiplus::ImageCodecInfo*)(malloc(size));
        if (pImageCodecInfo == NULL)
            return -1;

        Gdiplus::GetImageEncoders(num, size, pImageCodecInfo);

        for (uint32_t j = 0; j < num; ++j)
        {
            if (wcscmp(pImageCodecInfo[j].MimeType, format) == 0)
            {
                *pClsid = pImageCodecInfo[j].Clsid;
                free(pImageCodecInfo);
                return j;
            }
        }

        free(pImageCodecInfo);
        return -1;
    }

    const std::vector<uint8_t> ScreenshotManager::CaptureScreenshot()
   {
        std::vector<uint8_t> byteArray;

        int32_t screenX = 1920;
        int32_t screenY = 1080;

        HDC hScreenDC = GetDC(NULL);
        HDC hMemoryDC = CreateCompatibleDC(hScreenDC);

        HBITMAP hBitmap = CreateCompatibleBitmap(hScreenDC, screenX, screenY);
        HBITMAP hOldBitmap = (HBITMAP)SelectObject(hMemoryDC, hBitmap);

        BitBlt(hMemoryDC, 0, 0, screenX, screenY, hScreenDC, 0, 0, SRCCOPY);
        SelectObject(hMemoryDC, hOldBitmap);

        DeleteDC(hMemoryDC);
        ReleaseDC(NULL, hScreenDC);

        Gdiplus::Bitmap bitmap(hBitmap, NULL);

        IStream* pStream = NULL;
        CreateStreamOnHGlobal(NULL, TRUE, &pStream);

        CLSID pngClsid;
        GetEncoderClsid(L"image/png", &pngClsid);

        bitmap.Save(pStream, &pngClsid, NULL);

        LARGE_INTEGER liZero = {};
        ULARGE_INTEGER liSize;
        pStream->Seek(liZero, STREAM_SEEK_END, &liSize);
        pStream->Seek(liZero, STREAM_SEEK_SET, NULL);

        byteArray.resize((size_t)liSize.QuadPart);
        ULONG bytesRead = 0;
        pStream->Read(byteArray.data(), (ULONG)byteArray.size(), &bytesRead);

        pStream->Release();
        DeleteObject(hBitmap);

        return byteArray;
   }
}