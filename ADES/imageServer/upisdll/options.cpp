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

#include "options.h"
#include <stdio.h>
#include <windows.h>
#include <process.h>

// Global server constants
long g_lFrameSkip = 1;
long g_lCurrentFrame = 0;
int g_iImageType = 2;
bool g_bLegacy = 0;

// Set the number of frames to skip
void SetFrameSkip(long skip){
	g_lFrameSkip = skip;
	g_lCurrentFrame = 0;
}

// Set the JPEG compression flag
void SetImageType(int flag) {
	g_iImageType = flag;
}

// Set the legacy mode flag
void SetLegacyMode(bool mode) {
	g_bLegacy = mode;
}

// Get image type from environmental variable
int lookupImageType() {
	char strType[8];

	if (!GetEnvironmentVariable("IMAGEFORMAT", strType, 8)) {
		DWORD err = GetLastError();
		if (err != ERROR_ENVVAR_NOT_FOUND) {
			// Display an error message if the lookup failed
			char errmsg[128];
			sprintf_s(errmsg, 127, "GetEnvironmentVariable failed with error: %i\n", err);
			MessageBox(NULL, errmsg, "USARSim Image Server", MB_OK);	
		}
		return 3;
	}

	if (!_stricmp("RAW", strType)) return 0;
	else if (!_stricmp("SUPER", strType)) return 1;
	else if (!_stricmp("GOOD", strType)) return 2;
	else if (!_stricmp("NORMAL", strType)) return 3;
	else if (!_stricmp("FAIR", strType)) return 4;
	else if (!_stricmp("BAD", strType)) return 5;
	else return 3;
}

// Get frame skip count from environmental variable
long lookupFrameSkip() {
	char type[30];

	if (!GetEnvironmentVariable("FRAMESKIP", type, 30)) {
		DWORD err = GetLastError();
		if (err != ERROR_ENVVAR_NOT_FOUND) {
			// Display an error message if the lookup failed
			char errmsg[128];
			sprintf_s(errmsg, 127, "GetEnvironmentVariable failed with error: %i\n", err);
			MessageBox(NULL, errmsg, "USARSim Image Server", MB_OK);
		}
		return 7;
	}

	return abs(atol(type));
}

// Get legacy mode from environmental variable
bool lookupLegacy() {
	char type[5];

	if (!GetEnvironmentVariable("LEGACY", type, 5)) {
		DWORD err = GetLastError();
		if (err != ERROR_ENVVAR_NOT_FOUND) {
			// Display an error message if the lookup failed
			char errmsg[128];
			sprintf_s(errmsg, 127, "GetEnvironmentVariable failed with error: %i\n", err);
			MessageBox(NULL, errmsg, "USARSim Image Server", MB_OK);
		}
		return FALSE;
	}

	return !strncmp("TRUE", type, 5);
}

void ConfigureOptions() {
	// Pull parameters from environmental variables
	SetImageType(lookupImageType());
	SetFrameSkip(lookupFrameSkip());
	SetLegacyMode(lookupLegacy());
}