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

// Standard includes
#include <stdio.h>
#include <winsock2.h>
#include <process.h>
#include <windows.h>
#include <FreeImage.h>

// Application includes
#include "upisdll.h"
#include "simevent.h"
#include "options.h"

// Include the Winsock2 library
#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "FreeImage.lib")

// Global server constants
extern long g_lFrameSkip;
extern int g_iImageType;
extern bool g_bLegacy;

// Image buffer
extern FIBITMAP *g_fiImage; // External reference to raw frame buffer

// Image request event
extern SimEvent *g_pRequestEvent; // Event indicating that a new image has been captured
extern LONG g_lRequestFlag; // Event indicating that a new image has been requested

// Critical section used to synchronize access to the global image buffer
extern CRITICAL_SECTION g_CriticalSection;

// Display an error message if FreeImage fails for some reason
void FreeImageErrorHandler(FREE_IMAGE_FORMAT fif, const char *message) {
	char errmsg[1024];
	
	if(fif != FIF_UNKNOWN) {
		sprintf_s(errmsg, 1024, "FreeImage: %s Format, %s", FreeImage_GetFormatFromFIF(fif), message);
	} else { 
		sprintf_s(errmsg, 1024, "FreeImage: %s", message);
	}	

	MessageBox(NULL, errmsg, "USARSim Image Server", MB_OK);
}

// Convert from image type to JPEG compression
inline int convertToJpegFlag(int flag) {

	/*
	char msg[1024];
	sprintf_s(msg, 1024, "Flag: %i", flag);
	MessageBox(NULL,msg,"ADES DEBUG", MB_OK);
	*/

	switch (flag) {
		case 1: return JPEG_QUALITYSUPERB;
		case 2: return JPEG_QUALITYGOOD;
		case 3: return JPEG_QUALITYNORMAL;
		case 4: return JPEG_QUALITYAVERAGE;
		case 5: return JPEG_QUALITYBAD;
		default: return JPEG_QUALITYGOOD;
	}
}

// Takes an image from the frame buffer and compresses it
int writeFrame(FIMEMORY *fiBuffer, FIBITMAP *fiImage, unsigned char imageType) {
	/*
	char msg[1024];
	sprintf_s(msg, 1024, "imageType: %i", imageType);
	MessageBox(NULL,msg,"ADES DEBUG", MB_OK);
	*/
	imageType = 2;
	int errStatus = 1;
	u_short imageWidth, imageHeight;

	unsigned width, height, pitch, line;
	BYTE *bits;

	// Package image using correct compression
	switch(imageType) {
	case 0: // Send a raw frame
		// Get image characteristics
		width = FreeImage_GetWidth(fiImage);
		height = FreeImage_GetHeight(fiImage);
		pitch = FreeImage_GetPitch(fiImage);
		line = FreeImage_GetLine(fiImage);

		// Write out width and height
		errStatus = FreeImage_SeekMemory(fiBuffer, 0, SEEK_SET);
		if (errStatus != 1) break;

		imageWidth = htons(width);
		errStatus = FreeImage_WriteMemory( &imageWidth, 2, 1, fiBuffer );
		if (errStatus != 1) break;
		
		imageHeight = htons(height);
		errStatus = FreeImage_WriteMemory( &imageHeight, 2, 1, fiBuffer );
		if (errStatus != 1) break;

		// Write out image (convert the bitmap to raw bits, top-left pixel first)
		bits = (BYTE*)malloc(height * pitch);
		FreeImage_ConvertToRawBits(bits, fiImage, pitch, 24, 
			FI_RGBA_RED_MASK, FI_RGBA_GREEN_MASK, FI_RGBA_BLUE_MASK, TRUE);
		errStatus = FreeImage_WriteMemory( bits, height*pitch*sizeof(BYTE), 1, fiBuffer );
		free(bits);
		if (errStatus != 1) break;
		
		break;
	default: // Send a jpg frame
		errStatus = FreeImage_SeekMemory(fiBuffer, 0, SEEK_SET);
		if (errStatus != 1) break;

		errStatus = FreeImage_SaveToMemory(FIF_JPEG, fiImage, fiBuffer, convertToJpegFlag(imageType));
		if (errStatus != 1) break;
		break;
	}
	
	// Clean up and exit
	return errStatus;
}

// Sends an ImageServer frame
// [ImageType(1 byte) ImageSize(4 bytes) ImageData(n bytes)]
int transmitFrame(FIMEMORY *fiBuffer, SOCKET &clientSocket, unsigned char imageType) {
	int socketStatus;
	u_long imageSize;
	BYTE *fiBufferPtr = NULL;
	DWORD fiBufferSize = 0;

	// Get pointer to buffer memory
	socketStatus = FreeImage_AcquireMemory(fiBuffer, &fiBufferPtr, &fiBufferSize);
	if (socketStatus != 1) return SOCKET_ERROR;

	// Send the image type
	socketStatus = send(clientSocket, (char*)&imageType, 1, 0);
	if (socketStatus != 1) return socketStatus;

	// Send the image size (in bytes)
	imageSize = htonl((u_long)fiBufferSize);
	socketStatus = send(clientSocket, (char*)&imageSize, 4, 0);
	if (socketStatus != 4) return socketStatus;

	// Send the image
	socketStatus = send(clientSocket, (char*)fiBufferPtr, fiBufferSize, 0);
	if (socketStatus != fiBufferSize) return socketStatus;
	/*
	char msg[1024];
	sprintf_s(msg, 1024, "imageType: %i imageSize: %i", imageType, imageSize);
	MessageBox(NULL,msg,"ADES DEBUG", MB_OK);
	*/
	return socketStatus;
}

// Transmits a partial frame of imagery to the client
int sendPartialFrame(SOCKET &clientSocket,
			  unsigned int x, unsigned int y, unsigned int width, unsigned int height) {
	int status = 0;
	FIBITMAP *fiImage;
	FIBITMAP *fiImage24;
	FIMEMORY *fiBuffer;
	
	// Signal that a new frame is required and wait for frame
	InterlockedExchange( &g_lRequestFlag, TRUE );
	g_pRequestEvent->waitFor();

	// Enter critical section for frame buffer from UT2004
	// and copy new raw image to local buffer
	EnterCriticalSection( &g_CriticalSection );
	{
		fiImage = FreeImage_Copy(g_fiImage, x, y, x + width, y + height);
	} 
	LeaveCriticalSection( &g_CriticalSection );

	// Convert new image to 24 bits
	fiImage24 = FreeImage_ConvertTo24Bits(fiImage);

	// Create memory reference
	fiBuffer = FreeImage_OpenMemory();
	
	// Convert a raw frame to a useful image
	status = writeFrame( fiBuffer, fiImage24, g_iImageType );
	if (status != 1) status = 0; // TODO: handle error here
	
	// Transmit frame over socket
	status = transmitFrame( fiBuffer, clientSocket, g_iImageType );

	// Delete memory references
	FreeImage_Unload( fiImage );
	FreeImage_Unload( fiImage24 );
	FreeImage_CloseMemory( fiBuffer );

	return status;
}
			
// Transmits an entire frame of imagery to the client
int sendFullFrame(SOCKET &clientSocket) {
	int status = 0;
	FIBITMAP *fiImage;
	FIMEMORY *fiBuffer;

	// Signal that a new frame is required and wait for frame
	InterlockedExchange( &g_lRequestFlag, TRUE );
	g_pRequestEvent->waitFor();
	
	// Enter critical section for frame buffer from UT2004
	// and copy new raw image to local buffer
	EnterCriticalSection( &g_CriticalSection );
	{
		fiImage = FreeImage_ConvertTo24Bits(g_fiImage);
	} 
	LeaveCriticalSection( &g_CriticalSection );

	// Create memory reference
	fiBuffer = FreeImage_OpenMemory();

	// Convert a raw frame to a useful image
	status = writeFrame( fiBuffer, fiImage, g_iImageType );
	if (status != 1) status = 0; // TODO: handle error here
	
	// Transmit frame over socket
	status = transmitFrame( fiBuffer, clientSocket, g_iImageType );

	// Delete memory references
	FreeImage_Unload( fiImage );
	FreeImage_CloseMemory( fiBuffer );

	return status;
}

// This function is used to create a new thread to handle a new client
void HandleClientThreadFunction(void * parameters){
	SOCKET clientSocket = (SOCKET)parameters;
	
	char receivedByte;
	bool done = false;
	
	// Error reporting variables
	int status;
	char msg[1024];

	// If legacy mode, send an image at startup
	if (g_bLegacy) {
		status = sendFullFrame(clientSocket);
		if (status == SOCKET_ERROR) done = true;
	}
	
	// Loop until the socket has been closed (recv returned 0) or an error occured (recv returned SOCKET_ERROR)
	while(!done){
		// Get the first byte from the client		
		status = recv(clientSocket, &receivedByte, 1, 0);
		if(status != 1) break;

		// Determine the type of request from the first byte
		switch(receivedByte) {

		case 'O':
			// Confirm that this is part of 'OK'
			if (recv(clientSocket, &receivedByte, sizeof(char), 0) != sizeof(char)) break;
			if (receivedByte != 'K') break;

			// Send entire frame to client
			status = sendFullFrame(clientSocket);
			if (status == SOCKET_ERROR) done = true;

			// Flush any queued requests
			break;

		case 'U':
			// Get requested rectangle bounds
			u_long reqX, reqY, reqWidth, reqHeight;
			if(recv(clientSocket, (char*)&reqX, sizeof(u_long), 0) != sizeof(u_long)) break;
			if(recv(clientSocket, (char*)&reqY, sizeof(u_long), 0) != sizeof(u_long)) break;
			if(recv(clientSocket, (char*)&reqWidth, sizeof(u_long), 0) != sizeof(u_long)) break;
			if(recv(clientSocket, (char*)&reqHeight, sizeof(u_long), 0) != sizeof(u_long)) break;

			// Send partial frame to client
			status = sendPartialFrame( clientSocket,
				ntohl(reqX), ntohl(reqY), ntohl(reqWidth), ntohl(reqHeight) );
			if (status == SOCKET_ERROR) done = true;

			// Flush any queued requests
			break;

		default:
			continue;
		}
	}

	// Display an error message if the socket failed
	if(status == SOCKET_ERROR){
		if(WSAGetLastError() != WSAECONNABORTED && WSAGetLastError() != WSAECONNRESET){ 
			sprintf_s(msg, 1023, "WinSock Error: %i", WSAGetLastError());
			MessageBox(NULL, msg, "USARSim Image Server", MB_OK);
		}
	}

	// Thread clean-up
	shutdown(clientSocket, SD_SEND);
	closesocket(clientSocket);
	_endthread();
}

// This is the main server thread function.  As clients connect to the server,
// the HandleClientThreadFunction is called (within a new thread) to handle the
// new client.
void ServerThreadFunction(void * parameters){
	WSADATA winsock;
	SOCKET listenSocket, clientSocket;
	sockaddr_in socketAddress;
	BOOL bReuseAddr = TRUE;

	// Configure FreeImage error handler
	FreeImage_SetOutputMessage( FreeImageErrorHandler );

	// Pull parameters from environmental variables
	ConfigureOptions();
	
	// Setup WinSock
	if(WSAStartup (0x0202, &winsock) != 0) return;
	if (winsock.wVersion != 0x0202){
		MessageBox(NULL, "Incorrect Winsock version", "USARSim Image Server", MB_OK);	
		WSACleanup();
		return;
	}

	// Configure the socket for TCP
	listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if(listenSocket == INVALID_SOCKET){
		MessageBox(NULL, "Socket failed", "USARSim Image Server", MB_OK);	
		WSACleanup();
		return;
	}

	if (setsockopt(listenSocket, SOL_SOCKET, SO_REUSEADDR, (char*)&bReuseAddr, sizeof(bReuseAddr)) == SOCKET_ERROR) {
		MessageBox(NULL, "SetSockOpt failed", "USARSim Image Server", MB_OK);
		WSACleanup();
		return;
	}

	// Bind to port 5003
	memset(&socketAddress, 0, sizeof(sockaddr_in));
	socketAddress.sin_family = AF_INET;
	socketAddress.sin_port = htons(5003);
	socketAddress.sin_addr.s_addr = htonl(INADDR_ANY);
	if (bind(listenSocket, (LPSOCKADDR)&socketAddress, sizeof(socketAddress)) == SOCKET_ERROR){
	  MessageBox(NULL, "Bind Failed", "USARSim Image Server", MB_OK);
	  WSACleanup();
	  return;
	}

	// Listen on the socket for new clients
	if (listen(listenSocket, 732) == SOCKET_ERROR){
      MessageBox(NULL, "Listen failed", "USARSim Image Server", MB_OK);	
	  WSACleanup();
	  return;
	}

	for(;;){
		// Wait for a new client
		clientSocket = accept(listenSocket, NULL, NULL);

		// Check if new client is valid
		if(clientSocket == INVALID_SOCKET){
			MessageBox(NULL, "Accept failed", "USARSim Image Server", MB_OK);
			closesocket(listenSocket);
			break;
		}

		// Start a new thread to handle the new client
		_beginthread(HandleClientThreadFunction, 0, (void*)clientSocket);
	}
	
	// Process cleanup
	MessageBox(NULL, "ServerThread finished", "USARSim Image Server", MB_OK);
	WSACleanup();
	return;
}
