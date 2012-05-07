// upisEx.h

#pragma once

// Include the detours library
#pragma comment(lib, "detours.lib")

// Include the UPIS library
#pragma comment(lib, "upisdll.lib") 

#define DEFAULT_FORMAT "SUPER"  // SUPER GOOD
#define DEFAULT_FRAME_SKIP "1"


#define APP_PARAMS " 127.0.0.1?spectatoronly=1?quickstart=true -ini=usarsim.ini"
#define PROCESS_NAME "c:\\ut2004\\system\\UT2004.exe"
#define DETOUR_DLL "detoured.dll"
#define UPIS_DLL "upisdll.dll"

#include <sys/stat.h>
#include <windows.h>
#include <detours.h>

using namespace System;

namespace upisEx {

	public ref class Upis
	{
	public:

		Upis()
		{
			if (!SetEnvironmentVariable((LPCTSTR)"IMAGEFORMAT", (LPCTSTR)DEFAULT_FORMAT))
				System::Console::WriteLine("SetEnvironmentVariable failed with error:  " + GetLastError());
			if (!SetEnvironmentVariable((LPCTSTR)"FRAMESKIP", (LPCTSTR)DEFAULT_FRAME_SKIP))
				System::Console::WriteLine("SetEnvironmentVariable failed with error:  " + GetLastError());
			if (!SetEnvironmentVariable((LPCTSTR)"LEGACY", (LPCTSTR)"FALSE"))
				System::Console::WriteLine("SetEnvironmentVariable failed with error:  " + GetLastError());
			
			STARTUPINFO si;
			PROCESS_INFORMATION pi;
			ZeroMemory(&si,sizeof(si));
			si.cb=sizeof(si);
			ZeroMemory(&pi,sizeof(pi));
			WCHAR buf[MAX_PATH+1]={0};
			if (!DetourCreateProcessWithDllA(LPCSTR(PROCESS_NAME),
				LPSTR(APP_PARAMS),
				NULL,
				NULL,
				TRUE,
				CREATE_DEFAULT_ERROR_MODE,
				NULL,
				(LPCSTR)"C:\\ut2004\\system\\",
				(LPSTARTUPINFOA)&si,
				&pi,
				DETOUR_DLL,
				UPIS_DLL,
				0))
			{
				System::Console::WriteLine("DetourCreateProcessWithDllA failed with error:  " + GetLastError());
			}
		};

	};
}
