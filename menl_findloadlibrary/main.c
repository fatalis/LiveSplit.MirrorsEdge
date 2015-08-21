extern void * __stdcall GetModuleHandleA(const char *lpModuleName);
extern void * __stdcall GetProcAddress(void *hModule, const char *lpProcName);

int main()
{
	return GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryA");
}
