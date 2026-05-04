#include "NetworkManager.hpp"



namespace Client
{
    NetworkManager::NetworkManager(int port_data, int port_screen, const char* ip) : _port_data(port_data), _port_screen(port_screen), _ip(ip), _sock_data(INVALID_SOCKET), _sock_screen(INVALID_SOCKET), _ScreenshotManagerPtr(new ScreenshotManager())
    {
        WSADATA wsaData;

        if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
        {
            std::cerr << WSAGetLastError() << std::endl;
            return;
        }

        _sock_data = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        _sock_screen = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

        _tlsInitialized = false;
        ZeroMemory(&_credHandle, sizeof(_credHandle));
        ZeroMemory(&_contextHandle, sizeof(_contextHandle));
        ZeroMemory(&_streamSizes, sizeof(_streamSizes));
    }
    
    NetworkManager::~NetworkManager()
    {
        if (_tlsInitialized)
        {
            DeleteSecurityContext(&_contextHandle);
            FreeCredentialsHandle(&_credHandle);
        }

        if (_sock_data != INVALID_SOCKET)
        {
            closesocket(_sock_data);
        }

        if (_sock_screen != INVALID_SOCKET)
        {
            closesocket(_sock_screen);
        }

        WSACleanup();
    }



    bool NetworkManager::ConnectData()
    {
        struct sockaddr_in serverAddr;

        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(_port_data);

        inet_pton(AF_INET, _ip, &serverAddr.sin_addr);

        if (connect(_sock_data, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
        {
            return false;
        }

        return InitializeTlsForDataSocket();
    }

    bool  NetworkManager::ConnectScreen() 
    {
        struct sockaddr_in serverAddr;

        serverAddr.sin_family = AF_INET;

        serverAddr.sin_port = htons(_port_screen);

        inet_pton(AF_INET, _ip, &serverAddr.sin_addr);

        if(connect(_sock_screen, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != SOCKET_ERROR)
        {
            is_connected = true;
            return true;
        }

        return false;
    }



    void NetworkManager::WaitForScreenshotRequest()
    {
        char buffer[1024];

        while (true)
        {
            memset(buffer, 0, sizeof(buffer));

            int bytesRead = recv(_sock_screen, buffer, sizeof(buffer) - 1, 0);

            if (bytesRead > 0) 
            {
                std::string message(buffer, bytesRead);

                if (message == "SCREENSHOT_PLEASE") 
                {
                    SendScreenshot(_ScreenshotManagerPtr->CaptureScreenshot());
                }
            }
            else
            {
                std::cerr << "Ошибка получения данных по порту: " << _port_screen << std::endl;
                is_connected = false;
                break;
            }
        }
    }

    void NetworkManager::SendScreenshot(const std::vector<BYTE>& vector) const
    {
        int width = 1920;
        int height = 1080;


        if (_sock_screen == INVALID_SOCKET)
        {
            std::cerr << "Сокет по порту " << _port_screen <<" не валиден" << std::endl;
            return;
        }

        try
        {
            size_t imageSize = vector.size();

            size_t totalDataSize = sizeof(width) + sizeof(height) + sizeof(imageSize) + imageSize;
            std::vector<char> buffer(totalDataSize);

            std::memcpy(buffer.data(), &width, sizeof(width));
            std::memcpy(buffer.data() + sizeof(width), &height, sizeof(height));
            std::memcpy(buffer.data() + sizeof(width) + sizeof(height), &imageSize, sizeof(imageSize));
            std::memcpy(buffer.data() + sizeof(width) + sizeof(height) + sizeof(imageSize), vector.data(), imageSize);

            size_t totalBytesSent = 0;

            while (totalBytesSent < totalDataSize)
            {
                int bytesSent = send(_sock_screen, buffer.data() + totalBytesSent, static_cast<int>(totalDataSize - totalBytesSent), 0);
                if (bytesSent == SOCKET_ERROR)
                {
                    std::cerr << "Ошибка при отправке изображения" << std::endl;
                    return;
                }
                totalBytesSent += bytesSent;
            }
        }
        catch (const std::exception& ex)
        {
            std::cerr << "Ошибка при отправке изображения: " << ex.what() << std::endl;
        }
    }


    void NetworkManager::SendData(const std::string& data) const
    {
        SendTlsData(data);
    }

    bool NetworkManager::InitializeTlsForDataSocket()
    {
        SCHANNEL_CRED schannelCred;
        ZeroMemory(&schannelCred, sizeof(schannelCred));

        schannelCred.dwVersion = SCHANNEL_CRED_VERSION;
        schannelCred.grbitEnabledProtocols = SP_PROT_TLS1_2_CLIENT;
        schannelCred.dwFlags =
            SCH_CRED_NO_DEFAULT_CREDS |
            SCH_CRED_MANUAL_CRED_VALIDATION;

        TimeStamp expiry;

        SECURITY_STATUS status = AcquireCredentialsHandleA(
            NULL,
            const_cast<LPSTR>("Microsoft Unified Security Protocol Provider"),
            SECPKG_CRED_OUTBOUND,
            NULL,
            &schannelCred,
            NULL,
            NULL,
            &_credHandle,
            &expiry
        );

        if (status != SEC_E_OK)
        {
            std::cerr << "AcquireCredentialsHandle error: " << status << std::endl;
            return false;
        }

        DWORD contextFlags =
            ISC_REQ_SEQUENCE_DETECT |
            ISC_REQ_REPLAY_DETECT |
            ISC_REQ_CONFIDENTIALITY |
            ISC_REQ_ALLOCATE_MEMORY |
            ISC_REQ_STREAM;

        DWORD contextAttributes = 0;

        SecBuffer outBuffer;
        SecBufferDesc outBufferDesc;

        outBuffer.BufferType = SECBUFFER_TOKEN;
        outBuffer.cbBuffer = 0;
        outBuffer.pvBuffer = NULL;

        outBufferDesc.ulVersion = SECBUFFER_VERSION;
        outBufferDesc.cBuffers = 1;
        outBufferDesc.pBuffers = &outBuffer;

        status = InitializeSecurityContext(
            &_credHandle,
            NULL,
            NULL,
            contextFlags,
            0,
            SECURITY_NATIVE_DREP,
            NULL,
            0,
            &_contextHandle,
            &outBufferDesc,
            &contextAttributes,
            &expiry
        );

        if (status != SEC_I_CONTINUE_NEEDED)
        {
            std::cerr << "InitializeSecurityContext first error: " << status << std::endl;
            return false;
        }

        if (outBuffer.cbBuffer > 0 && outBuffer.pvBuffer != NULL)
        {
            send(
                _sock_data,
                static_cast<const char*>(outBuffer.pvBuffer),
                outBuffer.cbBuffer,
                0
            );

            FreeContextBuffer(outBuffer.pvBuffer);
            outBuffer.pvBuffer = NULL;
        }

        std::vector<char> inputBuffer(16384);

        while (true)
        {
            int received = recv(
                _sock_data,
                inputBuffer.data(),
                static_cast<int>(inputBuffer.size()),
                0
            );

            if (received <= 0)
            {
                std::cerr << "TLS handshake recv error" << std::endl;
                return false;
            }

            SecBuffer inBuffers[2];

            inBuffers[0].BufferType = SECBUFFER_TOKEN;
            inBuffers[0].pvBuffer = inputBuffer.data();
            inBuffers[0].cbBuffer = received;

            inBuffers[1].BufferType = SECBUFFER_EMPTY;
            inBuffers[1].pvBuffer = NULL;
            inBuffers[1].cbBuffer = 0;

            SecBufferDesc inBufferDesc;
            inBufferDesc.ulVersion = SECBUFFER_VERSION;
            inBufferDesc.cBuffers = 2;
            inBufferDesc.pBuffers = inBuffers;

            outBuffer.BufferType = SECBUFFER_TOKEN;
            outBuffer.cbBuffer = 0;
            outBuffer.pvBuffer = NULL;

            outBufferDesc.ulVersion = SECBUFFER_VERSION;
            outBufferDesc.cBuffers = 1;
            outBufferDesc.pBuffers = &outBuffer;

            status = InitializeSecurityContext(
                &_credHandle,
                &_contextHandle,
                NULL,
                contextFlags,
                0,
                SECURITY_NATIVE_DREP,
                &inBufferDesc,
                0,
                &_contextHandle,
                &outBufferDesc,
                &contextAttributes,
                &expiry
            );

            if (outBuffer.cbBuffer > 0 && outBuffer.pvBuffer != NULL)
            {
                send(
                    _sock_data,
                    static_cast<const char*>(outBuffer.pvBuffer),
                    outBuffer.cbBuffer,
                    0
                );

                FreeContextBuffer(outBuffer.pvBuffer);
                outBuffer.pvBuffer = NULL;
            }

            if (status == SEC_E_OK)
            {
                break;
            }

            if (status != SEC_I_CONTINUE_NEEDED)
            {
                std::cerr << "TLS handshake error: " << status << std::endl;
                return false;
            }
        }

        status = QueryContextAttributes(
            &_contextHandle,
            SECPKG_ATTR_STREAM_SIZES,
            &_streamSizes
        );

        if (status != SEC_E_OK)
        {
            std::cerr << "QueryContextAttributes error: " << status << std::endl;
            return false;
        }

        _tlsInitialized = true;
        return true;
    }

    bool NetworkManager::SendTlsData(const std::string& data) const
    {
        if (!_tlsInitialized)
        {
            std::cerr << "TLS не инициализирован" << std::endl;
            return false;
        }

        DWORD totalSize =
            _streamSizes.cbHeader +
            static_cast<DWORD>(data.size()) +
            _streamSizes.cbTrailer;

        std::vector<char> message(totalSize);

        memcpy(
            message.data() + _streamSizes.cbHeader,
            data.data(),
            data.size()
        );

        SecBuffer buffers[4];

        buffers[0].BufferType = SECBUFFER_STREAM_HEADER;
        buffers[0].pvBuffer = message.data();
        buffers[0].cbBuffer = _streamSizes.cbHeader;

        buffers[1].BufferType = SECBUFFER_DATA;
        buffers[1].pvBuffer = message.data() + _streamSizes.cbHeader;
        buffers[1].cbBuffer = static_cast<unsigned long>(data.size());

        buffers[2].BufferType = SECBUFFER_STREAM_TRAILER;
        buffers[2].pvBuffer = message.data() + _streamSizes.cbHeader + data.size();
        buffers[2].cbBuffer = _streamSizes.cbTrailer;

        buffers[3].BufferType = SECBUFFER_EMPTY;
        buffers[3].pvBuffer = NULL;
        buffers[3].cbBuffer = 0;

        SecBufferDesc messageDesc;
        messageDesc.ulVersion = SECBUFFER_VERSION;
        messageDesc.cBuffers = 4;
        messageDesc.pBuffers = buffers;

        SECURITY_STATUS status = EncryptMessage(
            const_cast<CtxtHandle*>(&_contextHandle),
            0,
            &messageDesc,
            0
        );

        if (status != SEC_E_OK)
        {
            std::cerr << "EncryptMessage error: " << status << std::endl;
            return false;
        }

        DWORD encryptedSize =
            buffers[0].cbBuffer +
            buffers[1].cbBuffer +
            buffers[2].cbBuffer;

        int sent = send(
            _sock_data,
            message.data(),
            encryptedSize,
            0
        );

        return sent != SOCKET_ERROR;
    }
}