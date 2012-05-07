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
#define INITGUID  // TODO: Optimize this back out of the application (Adds GUIDS for EVERYTHING!)

#include "dx10hook.h"
#include <stdio.h>
#include <windows.h>
#include <process.h>
#include <D3D10.h>
#include <DXGI.h>
#include <detours.h>
#include <FreeImage.h>
#include "simevent.h"
#include "options.h"

//const IID IID_ID3D10Texture2D;
//DEFINE_GUID(IID_ID3D10Texture2D,0x9B7E4C04,0x342C,0x4106,0xA1,0x9F,0x4F,0x27,0x04,0xF6,0x89,0xF0);

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
typedef HRESULT (STDMETHODCALLTYPE * LPD3D10CREATEDEVICE)(IDXGIAdapter *pAdapter, D3D10_DRIVER_TYPE DriverType, HMODULE Software, UINT Flags, 
														  UINT SDKVersion, ID3D10Device **ppDevice);
typedef HRESULT (STDMETHODCALLTYPE * LPCREATESWAPCHAIN)(IDXGIFactory *This, IUnknown *pDevice, DXGI_SWAP_CHAIN_DESC *pDesc, IDXGISwapChain **ppSwapChain);
typedef HRESULT (STDMETHODCALLTYPE * LPCREATEDXGIFACTORY)(REFIID riid, void **ppFactory);
typedef HRESULT (STDMETHODCALLTYPE * LPD3D10CREATEDEVICEANDSWAPCHAIN)(IDXGIAdapter *pAdapter, D3D10_DRIVER_TYPE DriverType, HMODULE Software,
																	  UINT Flags, UINT SDKVersion, DXGI_SWAP_CHAIN_DESC *pSwapChainDesc, 
																	  IDXGISwapChain **ppSwapChain, ID3D10Device **ppDevice);
typedef HRESULT (STDMETHODCALLTYPE * LPPRESENT10)(IDXGISwapChain *This, UINT SyncInterval, UINT Flags);

// Global function pointers to real DirectX functions
LPD3D10CREATEDEVICE				g_fpD3D10CreateDevice = NULL;
LPCREATESWAPCHAIN				g_fpCreateSwapChain = NULL;
LPCREATEDXGIFACTORY				g_fpCreateDXGIFactory = NULL;
LPD3D10CREATEDEVICEANDSWAPCHAIN	g_fpD3D10CreateDeviceAndSwapChain = NULL;
LPPRESENT10						g_fpPresent = NULL;

// Global variables
ID3D10Device*	    g_pD3D10Device;
IDXGISwapChain*	    g_pSwapChain;
ID3D10Texture2D*	g_pBackBufferCopy10;

// Private function used to save the backbuffer
void DX10_GetBackBuffer(){
	HRESULT hResult;
	ID3D10Texture2D* pBackBuffer;
	D3D10_TEXTURE2D_DESC surfaceDescription;
	D3D10_MAPPED_TEXTURE2D lockedRect;
	
	// Get back buffer
	hResult = g_pSwapChain->lpVtbl->GetBuffer(g_pSwapChain, 0, IID_ID3D10Texture2D, (LPVOID*)&(pBackBuffer));
	if(hResult != S_OK){
		MessageBox(NULL, "Could not capture back buffer", "USARSim Image Server", MB_OK);
		return;
	}
	
	// Change surface description to be staging buffer
	pBackBuffer->lpVtbl->GetDesc(pBackBuffer, &surfaceDescription);
	surfaceDescription.MipLevels = 1;
    surfaceDescription.ArraySize = 1;
    surfaceDescription.SampleDesc.Count = 1;
    surfaceDescription.Usage = D3D10_USAGE_STAGING;
    surfaceDescription.BindFlags = 0;
	surfaceDescription.CPUAccessFlags = D3D10_CPU_ACCESS_READ;
    surfaceDescription.MiscFlags = 0;

	// Create staging buffer and copy backbuffer into it
	hResult = g_pD3D10Device->lpVtbl->CreateTexture2D(g_pD3D10Device, &surfaceDescription, NULL, &g_pBackBufferCopy10);
	if(FAILED(hResult)) MessageBox(NULL, "CreateTexture2D failed", "USARSim Image Server", MB_OK);
	g_pD3D10Device->lpVtbl->CopyResource(g_pD3D10Device, (ID3D10Resource *)g_pBackBufferCopy10, (ID3D10Resource *)pBackBuffer);
	pBackBuffer->lpVtbl->Release(pBackBuffer);

	// Lock the back buffer copy
	hResult = g_pBackBufferCopy10->lpVtbl->Map(g_pBackBufferCopy10, 0, D3D10_MAP_READ, NULL, &lockedRect);
	if(hResult != S_OK){
		MessageBox(NULL, "Could not lock backbuffer copy", "USARSim Image Server", MB_OK);
		g_pBackBufferCopy10->lpVtbl->Release(g_pBackBufferCopy10);
	}

	// Allocate old and new FreeImage structure
	FIBITMAP * fiImageOld;
	FIBITMAP * fiImageNew = FreeImage_Allocate(surfaceDescription.Width, surfaceDescription.Height, 
												24, FI_RGBA_RED_MASK, FI_RGBA_GREEN_MASK, FI_RGBA_BLUE_MASK);
	
	// Copy image to FreeImage structure
	if (fiImageNew != NULL) {
		BYTE *bits = (BYTE *)lockedRect.pData;
		for (int rows = surfaceDescription.Height - 1; rows >= 0; rows--) {
			BYTE *source = bits;
			BYTE *target = FreeImage_GetScanLine(fiImageNew, rows);
			
			for (int cols = 0; cols < (int)surfaceDescription.Width; cols++) {
				target[FI_RGBA_BLUE] = source[FI_RGBA_RED];
				target[FI_RGBA_GREEN] = source[FI_RGBA_GREEN];
				target[FI_RGBA_RED] = source[FI_RGBA_BLUE];
				
				target += 3;
				source += 4;
			}
			bits += lockedRect.RowPitch;
		}
	}

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
	g_pBackBufferCopy10->lpVtbl->Unmap(g_pBackBufferCopy10, 0);
	g_pBackBufferCopy10->lpVtbl->Release(g_pBackBufferCopy10);
	g_pBackBufferCopy10 = NULL;
}

HRESULT WINAPI PresentDetour(IDXGISwapChain *This, UINT SyncInterval, UINT Flags){
	HRESULT hResult;

	if(g_lCurrentFrame >= g_lFrameSkip) {
		// Check if a frame is requested
		if (InterlockedCompareExchange( &g_lRequestFlag, FALSE, FALSE )) {
			DX10_GetBackBuffer();
			g_lCurrentFrame = 0;
		}
	} else {
		g_lCurrentFrame++;
	}
	
	hResult = g_fpPresent(This, SyncInterval, Flags);
	return(hResult);
}

HRESULT WINAPI D3D10CreateDeviceDetour(IDXGIAdapter *pAdapter, D3D10_DRIVER_TYPE DriverType, HMODULE Software, UINT Flags, 
									   UINT SDKVersion, ID3D10Device **ppDevice) {
	HRESULT hResult;

	hResult = g_fpD3D10CreateDevice(pAdapter, DriverType, Software, Flags, SDKVersion, ppDevice);
	if(FAILED(hResult)) return(hResult);
	g_pD3D10Device = *ppDevice;
	
	return(hResult);
}

HRESULT WINAPI CreateSwapChainDetour(IDXGIFactory *This, IUnknown *pDevice, DXGI_SWAP_CHAIN_DESC *pDesc, IDXGISwapChain **ppSwapChain) {
	HRESULT hResult;

	hResult = g_fpCreateSwapChain(This, pDevice, pDesc, ppSwapChain);
	if(FAILED(hResult)) return(hResult);
	g_pSwapChain = *ppSwapChain;

	// If we don't have this function yet, detour it
	if (g_fpPresent == NULL) {
		g_fpPresent = (*ppSwapChain)->lpVtbl->Present;
		
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)g_fpPresent, (PVOID)(&(PVOID&)PresentDetour));
		if (DetourTransactionCommit() != NO_ERROR){
			MessageBox(NULL, "Detours error", "USARSim Image Server", MB_OK);	
		}
	}
	
	return(hResult);
}

HRESULT WINAPI CreateDXGIFactoryDetour(REFIID riid, void **ppFactory) {
	HRESULT hResult;
	
	hResult = g_fpCreateDXGIFactory(riid, ppFactory);
	if(FAILED(hResult)) return(hResult);

	// If we don't have this function yet, detour it
	if (g_fpCreateSwapChain == NULL) {
		g_fpCreateSwapChain = (*((IDXGIFactory **)ppFactory))->lpVtbl->CreateSwapChain;
		
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)g_fpCreateSwapChain, (PVOID)(&(PVOID&)CreateSwapChainDetour));
		if (DetourTransactionCommit() != NO_ERROR){
			MessageBox(NULL, "Detours error", "USARSim Image Server", MB_OK);	
		}
	}
	
	return(hResult);
}

HRESULT WINAPI D3D10CreateDeviceAndSwapChainDetour(IDXGIAdapter *pAdapter, D3D10_DRIVER_TYPE DriverType, HMODULE Software, UINT Flags, UINT SDKVersion, 
												   DXGI_SWAP_CHAIN_DESC *pSwapChainDesc, IDXGISwapChain **ppSwapChain, ID3D10Device **ppDevice) {
	HRESULT hResult;

	hResult = g_fpD3D10CreateDeviceAndSwapChain(pAdapter, DriverType, Software, Flags, SDKVersion, pSwapChainDesc, ppSwapChain, ppDevice);
	if(FAILED(hResult)) return(hResult);
	g_pD3D10Device = *ppDevice;
	
	if (ppSwapChain != NULL) {
		g_pSwapChain = *ppSwapChain;

		// If we don't have this function yet, detour it
		if (g_fpPresent == NULL) {
			g_fpPresent = (*ppSwapChain)->lpVtbl->Present;
		
			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());
			DetourAttach(&(PVOID&)g_fpPresent, (PVOID)(&(PVOID&)PresentDetour));
			if (DetourTransactionCommit() != NO_ERROR){
				MessageBox(NULL, "Detours error", "USARSim Image Server", MB_OK);	
			}
		}
	}
	
	return(hResult);
}

void DirectX10Startup() {
	g_fpD3D10CreateDevice = (LPD3D10CREATEDEVICE)DetourFindFunction("d3d10.dll", "D3D10CreateDevice");
	if (g_fpD3D10CreateDevice != NULL) {
		DetourAttach(&(PVOID&)g_fpD3D10CreateDevice, D3D10CreateDeviceDetour);
	}

	g_fpCreateDXGIFactory = (LPCREATEDXGIFACTORY)DetourFindFunction("dxgi.dll", "CreateDXGIFactory");
	if (g_fpCreateDXGIFactory != NULL) {
		DetourAttach(&(PVOID&)g_fpCreateDXGIFactory, CreateDXGIFactoryDetour);
	}

	g_fpD3D10CreateDeviceAndSwapChain = (LPD3D10CREATEDEVICEANDSWAPCHAIN)DetourFindFunction("d3d10.dll", "D3D10CreateDeviceAndSwapChain");
	if (g_fpD3D10CreateDeviceAndSwapChain != NULL) {
		DetourAttach(&(PVOID&)g_fpD3D10CreateDeviceAndSwapChain, D3D10CreateDeviceAndSwapChainDetour);
	}
}

void DirectX10Shutdown() {
	if (g_fpD3D10CreateDevice != NULL) {
		DetourDetach(&(PVOID&)g_fpD3D10CreateDevice, D3D10CreateDeviceDetour);
	}

	if (g_fpCreateDXGIFactory != NULL) {
		DetourDetach(&(PVOID&)g_fpCreateDXGIFactory, CreateDXGIFactoryDetour);
	}

	if (g_fpD3D10CreateDeviceAndSwapChain != NULL) {
		DetourDetach(&(PVOID&)g_fpD3D10CreateDeviceAndSwapChain, D3D10CreateDeviceAndSwapChainDetour);
	}
	
	g_fpD3D10CreateDevice = NULL;
	g_fpCreateSwapChain = NULL;
	g_fpCreateDXGIFactory = NULL;
	g_fpD3D10CreateDeviceAndSwapChain = NULL;
	g_fpPresent = NULL;
}