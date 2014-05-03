import std.c.windows.windows;

extern(Windows) {
void OutputDebugStringA(LPCSTR lpPathName);
short GetAsyncKeyState(int vKey);

const int PIPE_ACCESS_INBOUND = 0x00000001;
const int PIPE_ACCESS_OUTBOUND = 0x00000002;
const int PIPE_ACCESS_DUPLEX = 0x00000003;
const int PIPE_WAIT = 0x00000000;
const int PIPE_NOWAIT = 0x00000001;
const int PIPE_READMODE_BYTE = 0x00000000;
const int PIPE_READMODE_MESSAGE = 0x00000002;
const int PIPE_TYPE_BYTE = 0x00000000;
const int PIPE_TYPE_MESSAGE = 0x00000004;
const int PIPE_UNLIMITED_INSTANCES = 255;
const int ERROR_PIPE_LISTENING = 536;
const int ERROR_BROKEN_PIPE = 109;
const int ERROR_PIPE_CONNECTED = 535;

HANDLE CreateNamedPipeA(
    LPCSTR lpName,
    DWORD dwOpenMode,
    DWORD dwPipeMode,
    DWORD nMaxInstances,
    DWORD nOutBufferSize,
    DWORD nInBufferSize,
    DWORD nDefaultTimeOut,
    LPSECURITY_ATTRIBUTES lpSecurityAttributes);

bool ConnectNamedPipe(HANDLE hNamedPipe, LPOVERLAPPED lpOverlapped);
bool DisconnectNamedPipe(HANDLE hNamedPipe);
}
