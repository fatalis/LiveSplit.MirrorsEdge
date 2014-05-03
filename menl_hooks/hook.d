import std.c.windows.windows;
import std.c.string: memcpy;

const int JMP_SIZE = 5;

bool memcpy_protected(void* dest, void* src, size_t size)
{
    uint oldProtect;
    if (VirtualProtect(dest, size, PAGE_EXECUTE_READWRITE, &oldProtect))
    {
        memcpy(dest, src, size);
        VirtualProtect(dest, size, oldProtect, &oldProtect);
        return true;
    }

    return false;
}

void JMP(ubyte* src, void* dest, int nops)
{
    uint oldProtect;
    if (VirtualProtect(src, JMP_SIZE+nops, PAGE_EXECUTE_READWRITE, &oldProtect))
    {
        // JMP instruction
        *src = 0xE9; 
        // encode the address
        *cast(void**)(src+1) = cast(void*)(dest - (src+JMP_SIZE));

        for (int i = 0; i < nops; i++)
            *(src + JMP_SIZE + i) = 0x90;

        VirtualProtect(src, JMP_SIZE+nops, oldProtect, &oldProtect);
    }
}

void TrampolineHook(ubyte* src, ubyte* dest, ubyte* gate, int overwritten)
{
    uint oldProtect;
    if (VirtualProtect(gate, overwritten, PAGE_EXECUTE_READWRITE, &oldProtect))
    {
        memcpy(gate, src, overwritten);
        VirtualProtect(gate, overwritten, oldProtect, &oldProtect);
    }

    JMP(gate+overwritten, src+overwritten, 0);
    JMP(src, dest, (overwritten > JMP_SIZE ? overwritten - JMP_SIZE : 0));
}
