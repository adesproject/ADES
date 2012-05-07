/*
 * Copyright (C) 2007 Sanford Freeman
 * Copyright (C) 2008 Prasanna Velagapudi
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

#define CINTERFACE
#define DLL_EXPORT

#include "upisdll.h"
#include <stdio.h>
#include <windows.h>
#include <process.h>
#include <detours.h>
#include <FreeImage.h>
#include "dx8hook.h"
#include "dx9hook.h"
#include "dx10hook.h"
#include "simevent.h"

// Programmatically include library files
#pragma comment(lib, "detours.lib")
#pragma comment(lib, "detoured.lib")
#pragma comment(lib, "d3d8.lib")

// Global server constants
extern long g_lFrameSkip;
extern long g_lCurrentFrame;
extern int g_iImageType;
extern bool g_bLegacy;

// Global variables
SimEvent *g_pRequestEvent;
LONG g_lRequestFlag = FALSE;
FIBITMAP *g_fiImage;
CRITICAL_SECTION g_CriticalSection;

// Detoured function pointers
void *g_fpDirect3DCreate8 = NULL;

// Main DLL control function
BOOL APIENTRY DllMain(HANDLE hModule, DWORD dwReason, LPVOID lpReserved){
	HANDLE hThread;
	
	switch(dwReason){
		case DLL_PROCESS_ATTACH:
			DetourRestoreAfterWith();
			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());

			// Add hooks for DirectX
			DirectX8Startup();
			DirectX9Startup();
			DirectX10Startup();
	
			if(DetourTransactionCommit() != NO_ERROR) return(FALSE);

			// Create server thread with a default stack size and no parameters
			InitializeCriticalSection(&g_CriticalSection);
			g_pRequestEvent = new SimEvent(false);
			hThread = (HANDLE)_beginthread(ServerThreadFunction, 0, NULL);
			break;
		case DLL_THREAD_ATTACH:
			break;
		case DLL_THREAD_DETACH:
			break;
		case DLL_PROCESS_DETACH:
			delete(g_pRequestEvent);
			DeleteCriticalSection(&g_CriticalSection);
			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());

			// Remove hooks for DirectX
			DirectX8Shutdown();
			DirectX9Shutdown();
			DirectX10Shutdown();

			DetourTransactionCommit();
			break; 
	};

	return TRUE;
}
