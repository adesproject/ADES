/*
* Copyright (C) 2009 Prasanna Velagapudi
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

#include "dx9hook.h"
#include <stdio.h>
#include <windows.h>
#include <process.h>
#include <d3d9.h>
#include <detours.h>
#include <FreeImage.h>
#include "simevent.h"
#include "options.h"

// Global server constants
extern long g_lFrameSkip;
extern long g_lCurrentFrame;
extern int g_iImageType;
extern bool g_bLegacy;

// Global variables
extern SimEvent *g_pRequestEvent;
extern LONG g_lRequestFlag;
extern FIBITMAP *g_fiImage;
extern CRITICAL_SECTION g_CriticalSection;

// Function Pointer Typedefs
typedef IDirect3D9* (WINAPI * LPDIRECT3DCREATE9)(UINT);
typedef HRESULT (STDMETHODCALLTYPE * LPCREATEDEVICE9)(IDirect3D9 * This, UINT Adapter, D3DDEVTYPE DeviceType, HWND hFocusWindow, DWORD BehaviorFlags,
									 	   D3DPRESENT_PARAMETERS* pPresentationParameters, IDirect3DDevice9** ppReturnedDeviceInterface);
typedef HRESULT (STDMETHODCALLTYPE * LPPRESENT9)(IDirect3DDevice9 * This, CONST RECT* pSourceRect, CONST RECT* pDestRect, HWND hDestWindowOverride, CONST RGNDATA* pDirtyRegion);

// Global function pointers to real DirectX functions
LPDIRECT3DCREATE9 g_fpDirect3DCreate9 = NULL;
LPCREATEDEVICE9	  g_fpCreateDevice9 = NULL;
LPPRESENT9		  g_fpPresent9 = NULL;

// Global variables
IDirect3DDevice9*  g_pDirect3DDevice9;
IDirect3DSurface9* g_pBackBufferCopy9;

// Private function used to save the backbuffer
void DX9_GetBackBuffer(){
	HRESULT hResult;
	IDirect3DSurface9* pBackBuffer;
	D3DSURFACE_DESC surfaceDescription;
	D3DLOCKED_RECT lockedRect;
	
	// Get back buffer
	hResult = g_pDirect3DDevice9->lpVtbl->GetBackBuffer(g_pDirect3DDevice9, 0, 0, D3DBACKBUFFER_TYPE_MONO, &pBackBuffer);
	if(hResult != D3D_OK){
		MessageBox(NULL, "Could not capture back buffer", "USARSim Image Server", MB_OK);
		return;
	}
	
	hResult = pBackBuffer->lpVtbl->GetDesc(pBackBuffer, &surfaceDescription);
	if(hResult != D3D_OK){
		MessageBox(NULL, "Could not get description of the back buffer", "USARSim Image Server", MB_OK);
		pBackBuffer->lpVtbl->Release(pBackBuffer);
	}

	hResult = g_pDirect3DDevice9->lpVtbl->CreateOffscreenPlainSurface(g_pDirect3DDevice9, surfaceDescription.Width, surfaceDescription.Height, surfaceDescription.Format, D3DPOOL_SYSTEMMEM, &g_pBackBufferCopy9, NULL);
	if(FAILED(hResult)) MessageBox(NULL, "CreateOffscreenPlainSurface failed", "USARSim Image Server", MB_OK);
	hResult = g_pDirect3DDevice9->lpVtbl->GetRenderTargetData(g_pDirect3DDevice9, pBackBuffer, g_pBackBufferCopy9);
	if(FAILED(hResult)) MessageBox(NULL, "GetRenderTargetData failed", "USARSim Image Server", MB_OK);
	pBackBuffer->lpVtbl->Release(pBackBuffer);

	// Lock the back buffer copy
	hResult = g_pBackBufferCopy9->lpVtbl->LockRect(g_pBackBufferCopy9, &lockedRect, NULL, D3DLOCK_READONLY);
	if(hResult != D3D_OK){
		MessageBox(NULL, "Could not lock backbuffer copy", "USARSim Image Server", MB_OK);
		g_pBackBufferCopy9->lpVtbl->Release(g_pBackBufferCopy9);
	}

	// Load image in FreeImage structure
	FIBITMAP * fiImageOld;
	FIBITMAP * fiImageNew = FreeImage_ConvertFromRawBits((BYTE *)lockedRect.pBits, 
									surfaceDescription.Width, surfaceDescription.Height, 
									lockedRect.Pitch, 8*DX9_RAW_VIDEO_BPP,
									FI_RGBA_RED_MASK, FI_RGBA_GREEN_MASK, FI_RGBA_BLUE_MASK, true);
	
	// Swap global image pointer and trigger image event
	EnterCriticalSection( &g_CriticalSection );
	{
		fiImageOld = g_fiImage;
		g_fiImage = fiImageNew;
	}
	LeaveCriticalSection( &g_CriticalSection );

	// Signal that new frame was captured
	InterlockedExchange( &g_lRequestFlag, FALSE );
	g_pRequestEvent->pulseEvent();

	// Clean up old buffer
	FreeImage_Unload( fiImageOld );

	// Unlock the back buffer
	hResult = g_pBackBufferCopy9->lpVtbl->UnlockRect(g_pBackBufferCopy9);
	if(hResult != D3D_OK){
		MessageBox(NULL, "Could not unlock backbuffer copy", "USARSim Image Server", MB_OK);
		g_pBackBufferCopy9->lpVtbl->Release(g_pBackBufferCopy9);
	}	

	g_pBackBufferCopy9->lpVtbl->Release(g_pBackBufferCopy9);
	g_pBackBufferCopy9 = NULL;
}

HRESULT WINAPI PresentDetour(IDirect3DDevice9* This, CONST RECT* pSourceRect, CONST RECT* pDestRect, HWND hDestWindowOverride, CONST RGNDATA* pDirtyRegion){
	HRESULT hResult;
	if(g_lCurrentFrame >= g_lFrameSkip) {
		// Check if a frame is requested
		if (InterlockedCompareExchange( &g_lRequestFlag, FALSE, FALSE )) {
			DX9_GetBackBuffer();
			g_lCurrentFrame = 0;
		}
	} else {
		g_lCurrentFrame++;
	}
	
	hResult = g_fpPresent9(This, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
	return(hResult);
}

HRESULT WINAPI CreateDeviceDetour(IDirect3D9* This, UINT Adapter, D3DDEVTYPE DeviceType, HWND hFocusWindow, DWORD BehaviorFlags, 
								  D3DPRESENT_PARAMETERS* pPresentationParameters, IDirect3DDevice9** ppReturnedDeviceInterface){
	HRESULT hResult;

	hResult = g_fpCreateDevice9(This, Adapter, DeviceType, hFocusWindow, BehaviorFlags, pPresentationParameters, ppReturnedDeviceInterface);
	if(FAILED(hResult)) return(hResult);
	g_pDirect3DDevice9 = *ppReturnedDeviceInterface;

	// If we don't have this function yet, detour it
	if (g_fpPresent9 == NULL) {
		g_fpPresent9 = (*ppReturnedDeviceInterface)->lpVtbl->Present;
		
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)g_fpPresent9, (PVOID)(&(PVOID&)PresentDetour));
		if (DetourTransactionCommit() != NO_ERROR){
			MessageBox(NULL, "Detours error", "USARSim Image Server", MB_OK);	
		}
	}

	return(hResult);
}

IDirect3D9* WINAPI Direct3DCreate9Detour(UINT SDKVersion){
	IDirect3D9* pD3D;

	pD3D = g_fpDirect3DCreate9(SDKVersion);

	// If we don't have this function yet, detour it
	if (g_fpCreateDevice9 == NULL) {
		g_fpCreateDevice9 = pD3D->lpVtbl->CreateDevice;

		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)g_fpCreateDevice9, (PVOID)(&(PVOID&)CreateDeviceDetour));
		if(DetourTransactionCommit() != NO_ERROR){
			MessageBox(NULL, "Direct3DCreate9Detour error", "USARSim Image Server", MB_OK);	
		}
	}
	
	return(pD3D);
}

void DirectX9Startup() {
	g_fpDirect3DCreate9 = (LPDIRECT3DCREATE9)DetourFindFunction("d3d9.dll", "Direct3DCreate9");
	if (g_fpDirect3DCreate9 != NULL) {
		DetourAttach(&(PVOID&)g_fpDirect3DCreate9, Direct3DCreate9Detour);		
	}
}

void DirectX9Shutdown() {
	if (g_fpDirect3DCreate9 != NULL) {
		DetourDetach(&(PVOID&)g_fpDirect3DCreate9, Direct3DCreate9Detour);
	}
	
	g_fpDirect3DCreate9 = NULL;
	g_fpCreateDevice9 = NULL;
	g_fpPresent9 = NULL;
}