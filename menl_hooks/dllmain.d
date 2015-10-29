// this is the dll that gets injected into the game process and hooks engine functions
// it communicates with livesplit over a named pipe

import core.sys.windows.windows: FlushFileBuffers;
import core.sys.windows.windows;
import core.sys.windows.dll;
import std.c.stdio: freopen;
import std.cstream;
import std.math;
import std.string;
import std.conv;
import std.file;
import core.stdc.wchar_ : wcslen;
import std.algorithm: canFind;
import std.uni: toLower;

import hook;
import win32;

//extern(C)
//export void stub() { };

__gshared { // don't put globals in Thread Local Storage

HMODULE g_base;
HINSTANCE g_hInst;
HANDLE g_hPipe;
LevelStreamData[] g_levelStreamData;
Thread g_thread;
OffsetDB g_offsetDB;
GameVersion g_version;
string g_waitForSublevel;
void* g_player;
bool g_consoleInitialized;
string g_currentLevel;
bool g_oncePerLevelFlag;
string[] g_levelsSplitOn;

}

const string PIPE_NAME = "LiveSplit.MirrorsEdge";

enum GameVersion
{
    Unknown,
    Steam101,
    RELOADED101,
    RELOADED100,
    OriginOrDVD101,
    DVD100,
}

enum OffsetName
{
    LevelStreamStartFunc,
    StringTablePtr,
    StaticLevelLoadFunc,
    SublevelFinishedLoadingFunc,
    MidLoadingStartFunc,
    MidLoadingEndFunc,
    DeathLoadingStartFunc,
    DeathLoadingEndFunc,
    UnknownImportantPtr,
    UnknownPlayerFunc,
    //TdProfileSettingsSetProfileSettingValue,
    //TdProfileSettingsSetProfileSettingValueInt
}

extern (Windows)
bool DllMain(HINSTANCE hInstance, uint ulReason, void* pvReserved)
{
    final switch (ulReason)
    {
        case DLL_PROCESS_ATTACH:
            dll_process_attach(hInstance, true);

            g_hInst = hInstance;

            if (g_thread is null)
            {
                g_thread = new Thread(&MainThread);
                g_thread.start();
            }

            break;
        case DLL_PROCESS_DETACH:
            dll_process_detach(hInstance, true);
            break;
        case DLL_THREAD_ATTACH:
            dll_thread_attach(true, true);
            break;
        case DLL_THREAD_DETACH:
            dll_thread_detach(true, true);
            break;
    }

    return true;
}

void MainThread()
{
    g_base = GetModuleHandleA(null);
    g_version = DetectGameVersion();

    // show the debug console if compiled for debug or if holding F10 during startup
    //if (GetAsyncKeyState(VK_F10))
    //    InitConsole();
    //else
    debug InitConsole();

    debug WriteConsole("debug: mirrorsedge_hooks.dll loaded into MirrorsEdge.exe successfully");
    debug WriteConsole(format("game version detected = %s", g_version));

    // the offsets are identical
    if (g_version == GameVersion.OriginOrDVD101)
        g_version = GameVersion.RELOADED101;
    else if (g_version == GameVersion.DVD100)
        g_version = GameVersion.RELOADED100;

    InitData();
    InstallHooks();

    debug WriteConsole("hooks: installed");

    RunNamedPipe();
}

// detect game version by file size
GameVersion DetectGameVersion()
{
    try
    {
        auto path = new wchar[MAX_PATH];
        GetModuleFileNameW(g_base, path.ptr, path.length);

        auto entry = DirEntry(to!string(path));

        final switch (entry.size)
        {
            case 60167392:
                return GameVersion.RELOADED101;
            case 31946072:
                return GameVersion.Steam101;
            case 60298504:
                return GameVersion.RELOADED100;
            case 36466688:
                return GameVersion.DVD100;
            case 36484440:
                return GameVersion.OriginOrDVD101;
        }
    }
    catch { }

    // for some reason this occurs for one person with a valid 31946072 byte Steam101 exe
    // file permissions? who knows
    return GameVersion.Steam101;
}

void InitConsole()
{
    AllocConsole();
    freopen("CONOUT$", "w", dout.file);
    freopen("CONIN$", "r", din.file);
    g_consoleInitialized = true;
}

void InitData()
{
    // elevators
    g_levelStreamData ~= new LevelStreamData("Escape_Intro",              7, "Escape_Off-R1_Slc_Lgts"          ); // Ch1 A
    g_levelStreamData ~= new LevelStreamData("Escape_Off_Bac",           13, "Edge_SB02_Mus"                   ); // Ch1 C
    g_levelStreamData ~= new LevelStreamData("Stormdrain_StdE",           8, "Stormdrain_Roof_boss_Bac"        ); // Ch2 E
    g_levelStreamData ~= new LevelStreamData("Stormdrain_Roof_spt",       8, "Stormdrain_boss_Spt"             ); // Ch2 G
    g_levelStreamData ~= new LevelStreamData("Cranes_Off-Roof_Building",  8, "Cranes_Plaza_LW"                 ); // Ch3 C
    g_levelStreamData ~= new LevelStreamData("Mall_HW-R1_Slc",            3, "Mall_R1-R2_Lgts"                 ); // Ch5 A
    g_levelStreamData ~= new LevelStreamData("Mall_R1-R2_Slc",           12, "Mall_Mall_Lgts_Pt1"              ); // Ch5 C
    g_levelStreamData ~= new LevelStreamData("Factory_Arena_Spt",         6, "Factory_Bac"                     ); // Ch6 D
    g_levelStreamData ~= new LevelStreamData("Boat_Ind-Cont_Slc",        13, "Boat_Deck_Spt"                   ); // Ch7 A
    g_levelStreamData ~= new LevelStreamData("Convoy_Roof",               9, "Convoy_Chase_Bac2"               ); // Ch8 A
    g_levelStreamData ~= new LevelStreamData("Convoy_Conv",               8, "Convoy_Snipe-Chase_Aud"          ); // Ch8 B
    g_levelStreamData ~= new LevelStreamData("Scraper_Deck_Spt",          8, "Scraper_Roof_Bac2"               ); // Ch9 B
    g_levelStreamData ~= new LevelStreamData("Scraper_Lobby",            14, "Scraper_Duct-Roof_Aud"           ); // Ch9 C
    g_levelStreamData ~= new LevelStreamData("Scraper_Roof_Spt",         16, "Scraper_Ext_Lgts"                ); // Ch9 E
                                                                                                               
    // special cases                                                                                           
    g_levelStreamData ~= new LevelStreamData("Stormdrain_Std",           15, "Stormdrain_StdP-StdE_slc_Lgts",  
        Vector3f(1321f, -30039f, -6635f), 150f);                                                           
    g_levelStreamData ~= new LevelStreamData("Stormdrain_StdP",          10, "Stormdrain_StdE-Out_Blding_slc",
        Vector3f(1488f, -10488f, -7267f), 70f);                                                            
                                                                                                               
    // OoB                                                                                                     
    g_levelStreamData ~= new LevelStreamData("Subway_Stat_Spt",          12, "Subway_Plat_Spt"                 );
    g_levelStreamData ~= new LevelStreamData("Factory_Lbay_Spt",          7, "Factory_Facto"                   );
    g_levelStreamData ~= new LevelStreamData("Scraper_Lobby",            13, "Scraper_Duct"                    );

    g_offsetDB = new OffsetDB();
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.LevelStreamStartFunc,                       0xA0F260);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.StringTablePtr,                             0x1C67898);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.StaticLevelLoadFunc,                        0xDC7650);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.SublevelFinishedLoadingFunc,                0x784970); // sig: 33 50 60 83 E2 01 31 50 60
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.MidLoadingStartFunc,                        0xAC54B0);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.MidLoadingEndFunc,                          0xAC5C80);
    //g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.DeathLoadingStartFunc,                    0xDC4E10);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.DeathLoadingStartFunc,                      0xDC4DE0);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.DeathLoadingEndFunc,                        0xDC5420);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.UnknownImportantPtr,                        0x1C14D64);
    g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.UnknownPlayerFunc,                          0xE68CF0); // sig: 8b 86 9c 00 00 00 8b 88
    //g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.TdProfileSettingsSetProfileSettingValue,    0xDEADBABE); // TODO
    //g_offsetDB.Add(GameVersion.RELOADED101, OffsetName.TdProfileSettingsSetProfileSettingValueInt, 0xDEADBABE); // TODO

    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.LevelStreamStartFunc,                       0xA0F190);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.StringTablePtr,                             0x1C4E7D8);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.StaticLevelLoadFunc,                        0xDC6A70);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.SublevelFinishedLoadingFunc,                0x7848A0);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.MidLoadingStartFunc,                        0xAC53E0);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.MidLoadingEndFunc,                          0xAC5BB0);
    //g_offsetDB.Add(GameVersion.Steam101,    OffsetName.DeathLoadingStartFunc,                    0xDC4C30);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.DeathLoadingStartFunc,                      0xDC4C00);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.DeathLoadingEndFunc,                        0xDC5010);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.UnknownImportantPtr,                        0x1BFBCA4);
    g_offsetDB.Add(GameVersion.Steam101,    OffsetName.UnknownPlayerFunc,                          0xE679D0);
    //g_offsetDB.Add(GameVersion.Steam101,    OffsetName.TdProfileSettingsSetProfileSettingValue,    0xC18690);
    //g_offsetDB.Add(GameVersion.Steam101,    OffsetName.TdProfileSettingsSetProfileSettingValueInt, 0xC187A0);

    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.LevelStreamStartFunc,                       0xA0EE60);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.StringTablePtr,                             0x1C67898);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.StaticLevelLoadFunc,                        0xDC7050);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.SublevelFinishedLoadingFunc,                0x784740);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.MidLoadingStartFunc,                        0xAC50B0);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.MidLoadingEndFunc,                          0xAC5880);
    //g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.DeathLoadingStartFunc,                    0xDC4810);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.DeathLoadingStartFunc,                      0xDC47E0);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.DeathLoadingEndFunc,                        0xDC4E20);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.UnknownImportantPtr,                        0x1C14D5C);
    g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.UnknownPlayerFunc,                          0xE686F0);
    //g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.TdProfileSettingsSetProfileSettingValue,    0xDEADBABE); // TODO
    //g_offsetDB.Add(GameVersion.RELOADED100, OffsetName.TdProfileSettingsSetProfileSettingValueInt, 0xDEADBABE); // TODO
}

void InstallHooks()
{
    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.StaticLevelLoadFunc),
        cast(ubyte*)&StaticLevelLoadHook,
        cast(ubyte*)&StaticLevelLoadGate,
        JMP_SIZE+2);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.LevelStreamStartFunc),
        cast(ubyte*)&LevelStreamStartHook,
        cast(ubyte*)&LevelStreamStartGate,
        JMP_SIZE+2);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.SublevelFinishedLoadingFunc),
        cast(ubyte*)&SublevelFinishedLoadingHook,
        cast(ubyte*)&SublevelFinishedLoadingGate,
        JMP_SIZE+1);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.MidLoadingStartFunc),
        cast(ubyte*)&MidLoadingStartHook,
        cast(ubyte*)&MidLoadingStartGate,
        JMP_SIZE+5);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.MidLoadingEndFunc),
        cast(ubyte*)&MidLoadingEndHook,
        cast(ubyte*)&MidLoadingEndGate,
        JMP_SIZE+1);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.DeathLoadingStartFunc),
        cast(ubyte*)&DeathLoadingStartHook,
        cast(ubyte*)&DeathLoadingStartGate,
        JMP_SIZE+5);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.DeathLoadingEndFunc),
        cast(ubyte*)&DeathLoadingEndHook,
        cast(ubyte*)&DeathLoadingEndGate,
        JMP_SIZE+1);

    TrampolineHook(
        cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.UnknownPlayerFunc),
        cast(ubyte*)&UnknownPlayerFuncHook,
        cast(ubyte*)&UnknownPlayerFuncGate,
        JMP_SIZE);

    //TrampolineHook(
    //    cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.TdProfileSettingsSetProfileSettingValue),
    //    cast(ubyte*)&TdProfileSettingsSetProfileSettingValueHook,
    //    cast(ubyte*)&TdProfileSettingsSetProfileSettingValueGate,
    //    JMP_SIZE);

    //TrampolineHook(
    //    cast(ubyte*)g_base+g_offsetDB.Get(g_version, OffsetName.TdProfileSettingsSetProfileSettingValueInt),
    //    cast(ubyte*)&TdProfileSettingsSetProfileSettingValueIntHook,
    //    cast(ubyte*)&TdProfileSettingsSetProfileSettingValueIntGate,
    //    JMP_SIZE+1);
}

void RunNamedPipe()
{
    HANDLE hPipe = CreateNamedPipeA(
        (r"\\.\pipe\" ~ PIPE_NAME).toStringz(),
        PIPE_ACCESS_DUPLEX,
        PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_NOWAIT,
        1,
        1024, 1024,
        0,
        null);

    if (hPipe == INVALID_HANDLE_VALUE)
        return;

    scope(exit) CloseHandle(hPipe);
    g_hPipe = hPipe;

    while (true)
    {
        debug WriteConsole("named pipe: waiting connection");

        DisconnectNamedPipe(hPipe);
        while (!ConnectNamedPipe(hPipe, null) && GetLastError() != ERROR_PIPE_CONNECTED)
        {
            Sleep(1);
        }

        debug WriteConsole("named pipe: connected");

        byte tmp;
        uint read;
        while (ReadFile(hPipe, &tmp, 1, &read, null) || GetLastError() != ERROR_BROKEN_PIPE)
        {
            // ERROR_BROKEN_PIPE on disconnect
            // ERROR_NO_DATA nothing to read
            Sleep(1);
        }

        debug WriteConsole("named pipe: disconnected");
    }
}

// TODO: the original function doesn't return until the cutscene has been skipped,
// so frames of accuracy are lost until the player cancels the cutscene.
// we need to detect when the cutscene is skippable.
extern(Windows)
int StaticLevelLoadHook(void* levelInfo, int unk, void* unk2)
{
    void* this_; asm { mov this_, ECX; }

    wchar* wc = *cast(wchar**)(levelInfo+0x1C);
    string name = to!string(wc[0..wcslen(wc)]);
    g_currentLevel = name;
    g_oncePerLevelFlag = false;

    CheckSplit(name);

    debug WriteConsole(format("static level load started: %s", name));
    SetPausedState(true);

    // they quit out, wipe the state
    if (name == "TdMainMenu")
        g_waitForSublevel = null;

    debug WriteConsole("resetting level stream data");
    foreach (LevelStreamData d; g_levelStreamData)
    {
        d.Reset();
    }

    asm { mov ECX, this_; }
    int ret = StaticLevelLoadGate(levelInfo, unk, unk2);

    SetPausedState(false);

    debug WriteConsole("static level load finished");

    return ret;
}

void CheckSplit(string level)
{
    static string[] splitLevels = ["escape_p", "stormdrain_p", "cranes_p", "subway_p", "mall_p", "factory_p",
"boat_p", "convoy_p", "scraper_p"];

    if (level.toLower() == "tutorial_p")
    {
        debug WriteConsole("start");
        g_levelsSplitOn = [];
        WritePipe("start");
    }
    else if (splitLevels.canFind(level.toLower()) && !g_levelsSplitOn.canFind(level.toLower()))
    {
        g_levelsSplitOn ~= level.toLower();
        debug WriteConsole("split");
        WritePipe("split");
    }
}

extern(Windows)
int StaticLevelLoadGate(void* levelInfo, int unk, void* unk2) {
    asm { naked;
        nop; nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop; }         // jmp
}

extern(C)
int MidLoadingStartHook()
{
    int ret = MidLoadingStartGate();

    debug WriteConsole("mid loading start detected");
    SetPausedState(true);

    return ret;
}

extern(C)
int MidLoadingStartGate() {
    asm { naked;
        nop; nop; nop; nop; nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop; }                        // jmp
}

extern(C)
int MidLoadingEndHook()
{
    int ret = MidLoadingEndGate();

    debug WriteConsole("mid loading end detected");
    SetPausedState(false);

    return ret;
}

extern(C)
int MidLoadingEndGate() {
    asm { naked;
        nop; nop; nop; nop; nop; nop;  // overwritten bytes
        nop; nop; nop; nop; nop; }     // jmp
}

extern(C)
int DeathLoadingStartHook()
{
    int ret = DeathLoadingStartGate();

    debug WriteConsole("death loading start detected");
    SetPausedState(true);

    return ret;
}

extern(C)
int DeathLoadingStartGate() {
    asm { naked;
        nop; nop; nop; nop; nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop; }                        // jmp
}

extern(C)
int DeathLoadingEndHook()
{
    int ret = DeathLoadingEndGate();

    debug WriteConsole("death loading end detected");
    SetPausedState(false);

    return ret;
}

extern(C)
int DeathLoadingEndGate() {
    asm { naked;
        nop; nop; nop; nop; nop; nop;  // overwritten bytes
        nop; nop; nop; nop; nop; }     // jmp
}


extern(Windows)
void SublevelFinishedLoadingHook(void* levelInfo, void* unk)
{
    SublevelFinishedLoadingGate(levelInfo, unk);

    ubyte flags = *cast(ubyte*)(levelInfo+0x60);
    if (flags & 0x80)
    {
        int sublevelStrID = *cast(int*)(levelInfo+0x3C);
        string sublevelName = GetStringByID(sublevelStrID);

        debug WriteConsole(format("sublevel finished loading: %s", sublevelName));

        foreach (LevelStreamData d; g_levelStreamData)
        {
            if (d.LastLoadSublevel == sublevelName)
            {
                debug WriteConsole("cancelled waiting to reach required pos because the target sublevel finished loading");
                d.Reset();
                break;
            }
        }

        if (g_waitForSublevel is null)
            return;
                                                 // hack fix for ch6d elevator in SS
        if (g_waitForSublevel == sublevelName || (g_waitForSublevel == "Factory_Bac" && sublevelName == "Factory_Pursu_lgts"))
        {
            g_waitForSublevel = null;
            debug WriteConsole("--elevator/oob finished loading--");
            SetPausedState(false);
        }
    }
}

extern(Windows)
void SublevelFinishedLoadingGate(void* levelInfo, void* unk) {
    asm { naked;
        nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop; }    // jmp
}

extern(Windows)
int LevelStreamStartHook()
{
    void* this_; asm { mov this_, ECX; }

    const byte LOADTYPE_UNLOADING = 0;
    const byte LOADTYPE_LOADING = 1;
    const byte FLAG_LOADED = 1;

    int ret = LevelStreamStartGate();

    // get the list of sublevels to be unloaded/loaded
    string[] sublevels;
    int sublevelCount = *cast(int*)(this_+0xF4);
    for (int i = 0; i < sublevelCount; i++)
    {
        ubyte* ptr = *cast(ubyte**)(this_+0xF0);
        ptr += (i * 12);
        int sublevelStringID = *cast(int*)(ptr+4);
        sublevels ~= GetStringByID(sublevelStringID);
    }

    byte loadingType = *(*cast(byte**)(this_+0x88)+0xC); // 1 = loading, 0 = unloading

    debug WriteConsole(loadingType == LOADTYPE_LOADING ? "-load list-" : "-unload list-");
    foreach (string sublevel; sublevels)
    {
        debug WriteConsole(format("%s: %X", sublevel, GetSublevelStatusFlag(sublevel)));
    }

    foreach (LevelStreamData d; g_levelStreamData)
    {
        if (loadingType == LOADTYPE_UNLOADING && d.UnloadCount == sublevels.length && d.FirstUnloadSublevel == sublevels[0])
        {
            // check if this is a real unload
            int numLoaded = 0;
            foreach (string sublevel; sublevels)
            {
                if ((GetSublevelStatusFlag(sublevel) & FLAG_LOADED))
                    numLoaded++;
            }
            // if at least one sublevel in the unload list is currently loaded, it's a real unload
            // ch4 oob is an exception because none of the items on the unload list are loaded
            if (numLoaded < 1 && sublevels[0] != "Factory_Lbay_Spt")
                break;

            if (d.IsPositional() && !d.RequiredPositionReached)
            {
                debug WriteConsole("load started but required pos hasnt been reached yet. waiting until required area is reached before pausing");
                d.LoadingBeforeRequiredPosition = true;
            }
            else
            {
                SetPausedState(true);
                debug WriteConsole("--elevator/oob load start detected--");
                g_waitForSublevel = d.LastLoadSublevel;
            }

            debug WriteConsole(format("num loaded on unload list: %d/%d", numLoaded, sublevels.length));

            break;
        }
    }

    return ret;
}

extern(Windows)
int LevelStreamStartGate() {
    asm { naked;
        nop; nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop; }         // jmp
}

// some function that runs every frame and it's this ptr can be used to find player position
// use this to find player ptr and do stuff we need to do once per frame
extern(Windows)
int UnknownPlayerFuncHook(float frametime)
{
    asm { mov g_player, ECX; }

    int ret = UnknownPlayerFuncGate(frametime);

    Vector3f* pos = GetPlayerPos();
    if (pos is null)
        return ret;

    foreach (LevelStreamData d; g_levelStreamData)
    {
        if (d.CheckPosition(pos))
        {
            debug WriteConsole("pausing because reached required pos and level not finished streaming yet");
            SetPausedState(true);
            g_waitForSublevel = d.LastLoadSublevel;
            break;
        }
    }

    static auto endingPos = Vector3f(-6987.5, 9672.7, 75237.5);
    static auto stormdrainExitButtonPos = Vector3f(925.2, -6835.7, -3130.8);
    if (!g_oncePerLevelFlag && endingPos.Distance(pos) < 12.0 && g_currentLevel == "Scraper_p")
    {
        WritePipe("end");
        debug WriteConsole("end");
        g_oncePerLevelFlag = true;
    }
    if (!g_oncePerLevelFlag && stormdrainExitButtonPos.Distance(pos) < 100.0 && g_currentLevel == "Stormdrain_p")
    {
        WritePipe("stormdrain");
        debug WriteConsole("stormdrain");
        g_oncePerLevelFlag = true;
    }

    debug
    {
        //string test = format("%f %f %f", pos.X, pos.Y, pos.Z);
        //SetConsoleTitleA(test.toStringz());
    }

    return ret;
}

extern(Windows)
bool UnknownPlayerFuncGate(float frametime) {
    asm { naked;
        nop; nop; nop; nop; nop;   // overwritten bytes
        nop; nop; nop; nop; nop; } // jmp
}

// checkpoint split code
/*extern(Windows)
int TdProfileSettingsSetProfileSettingValueHook(int setting, wchar** value)
{
    void* this_; asm { mov this_, ECX; }

    const int TDPID_LastSavedMap = 900;
    const int TDPID_LastSavedCheckpoint = 901;

    int ret = TdProfileSettingsSetProfileSettingValueGate(setting, value);

    if (setting == TDPID_LastSavedMap)
    {
        debug WriteConsole(format("TDPID_LastSavedMap changed to '%s'", WCharToString(*value)));
    }
    else if (setting == TDPID_LastSavedCheckpoint)
    {
        debug WriteConsole(format("TDPID_LastSavedCheckpoint changed to '%s'", WCharToString(*value)));
    }

    return ret;
}

extern(Windows)
int TdProfileSettingsSetProfileSettingValueGate(int setting, wchar** value) {
    asm { naked;
        nop; nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop; } // jmp
}*/

// old start detect (on press enter)
/*extern(Windows)
int TdProfileSettingsSetProfileSettingValueIntHook(int setting, int value)
{
    void* this_; asm { mov this_, ECX; }

    const int TDPID_Game_NumGiveBulletDamage = 905;

    int ret = TdProfileSettingsSetProfileSettingValueIntGate(setting, value);

    // TdGame.u > TdUIScene_MainMenu > TdUIScene_DifficultySettings > OnAccept > ResetStats > TdStatsManager.uc > ResetStats > 
    // P.SetProfileSettingValueInt(TDPID_Game_NumGiveBulletDamage, 0);
    if (setting == TDPID_Game_NumGiveBulletDamage && value == 0 && g_currentLevel == "TdMainMenu")
    {
        debug WriteConsole("new game");
        g_levelsSplitOn = [];
        WritePipe("start");
    }

    return ret;
}

extern(Windows)
int TdProfileSettingsSetProfileSettingValueIntGate(int setting, int value) {
    asm { naked;
        nop; nop; nop; nop; nop; nop; nop; // overwritten bytes
        nop; nop; nop; nop; nop;           // jmp
        nop; }                             // extra bytes
}*/

void WriteConsole(string message)
{
    if (g_consoleInitialized)
        std.stdio.writeln(message);
}

void SetPausedState(bool paused)
{
    paused ? WritePipe("pause") : WritePipe("unpause");
}

bool WritePipe(string message)
{
    if (g_hPipe is null)
        return false;

    message ~= "\n";

    uint written;
    if (!WriteFile(g_hPipe, message.ptr, message.length, &written, null) || written != message.length)
        return false;
    FlushFileBuffers(g_hPipe);

    return true;
}

string WCharToString(const wchar* wc)
{
    if (wc is null)
        return "";
    return to!string(wc[0..wcslen(wc)]);
}

string GetStringByID(int id)
{
    if (id == 0)
        return "";

    void* ptr = *cast(void**)(g_base+g_offsetDB.Get(g_version, OffsetName.StringTablePtr));
    ptr = *cast(void**)(ptr + (id*4));
    ptr += 0x10;

    wchar* wc = cast(wchar*)ptr;

    return WCharToString(wc);
}

byte GetSublevelStatusFlag(string level)
{
    void* ptr = g_base+g_offsetDB.Get(g_version, OffsetName.UnknownImportantPtr);
    ptr = *cast(void**)ptr;
    ptr = *cast(void**)(ptr+0x50);
    ptr = **cast(void***)(ptr+0x3C);

    int numSublevels = *cast(int*)(ptr+0xBF0);
    void* sublevelsBase = *cast(void**)(ptr+0xBEC);

    for (int i = 0; i < numSublevels; i++)
    {
        void* sublevel = *cast(void**)(sublevelsBase+(i*4));
        if (sublevel is null)
            continue;
        int sublevelStrID = *cast(int*)(sublevel+0x3C);
        string sublevelStr = GetStringByID(sublevelStrID);
        if (sublevelStr == level)
            return *cast(byte*)(sublevel+0x60);
    }
    return 0;
}

Vector3f* GetPlayerPos()
{
    if (g_player is null)
        return null;

    void* ptr = *cast(void**)(g_player + 0x4a4);
    ptr = *cast(void**)(ptr + 0x214);
    if (ptr is null) // time trial mode crash fix
        return null;
    ptr += 0xE8;

    return cast(Vector3f*)ptr;
}

class LevelStreamData
{
    string FirstUnloadSublevel;
    int UnloadCount;
    string LastLoadSublevel;
    string AltLastLoadSublevel;

    bool LoadingBeforeRequiredPosition;
    bool RequiredPositionReached;
    Vector3f RequiredPosition;
    float RequiredPositionRadius;

    this(string firstUnloadSublevel, int unloadCount, string lastLoadSublevel)
    {
        this.FirstUnloadSublevel = firstUnloadSublevel;
        this.UnloadCount = unloadCount;
        this.LastLoadSublevel = lastLoadSublevel;
    }

    this(string firstUnloadSublevel, int unloadCount, string lastLoadSublevel, Vector3f requiredPos, float requiredPosRadius)
    {
        this(firstUnloadSublevel, unloadCount, lastLoadSublevel);
        this.RequiredPosition = requiredPos;
        this.RequiredPositionRadius = requiredPosRadius;
    }

    bool IsPositional()
    {
        return this.RequiredPosition.Initialized;
    }

    void Reset()
    {
        this.LoadingBeforeRequiredPosition = false;
        this.RequiredPositionReached = false;
    }

    bool CheckPosition(Vector3f* player)
    {
        if (!this.IsPositional())
            return false;

        if (!this.RequiredPositionReached && player.Distance(&this.RequiredPosition) < this.RequiredPositionRadius)
        {
            debug WriteConsole("required position reached");
            this.RequiredPositionReached = true;

            if (this.LoadingBeforeRequiredPosition)
                return true;
        }

        return false;
    }
}

class OffsetDB
{
    private Offset[] _offsets;

    void Add(GameVersion ver, OffsetName name, uint offset)
    {
        _offsets ~= new Offset(ver, name, offset);
    }

    uint Get(GameVersion ver, OffsetName name)
    {
        foreach (Offset offset; _offsets)
        {
            if (offset.Version == ver && offset.Name == name)
                return offset.Offset;
        }

        throw new Exception("Offset not in DB.");
    }

    private class Offset
    {
        GameVersion Version;
        OffsetName Name;
        uint Offset;

        this(GameVersion ver, OffsetName name, uint offset)
        {
            this.Version = ver;
            this.Name = name;
            this.Offset = offset;
        }
    }
}

struct Vector3f
{
    float X;
    float Y;
    float Z;

    bool Initialized;

    this(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.Initialized = true;
    }

    float Distance(const Vector3f* other)
    {
        float result = (this.X - other.X) * (this.X - other.X) +
            (this.Y - other.Y) * (this.Y - other.Y) + 
            (this.Z - other.Z) * (this.Z - other.Z);
        return sqrt(result);
    }

    float DistanceXY(const Vector3f* other)
    {
        float result = (this.X - other.X) * (this.X - other.X) +
            (this.Y - other.Y) * (this.Y - other.Y);
        return sqrt(result);
    }
}
