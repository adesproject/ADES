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

#ifndef __SIMEVENT__
#define __SIMEVENT__

#include <windows.h>

#define MAX_THREADS 2000

class SimEvent {
private:
	LONG m_BlockCount;
	bool m_Signalled;
	CRITICAL_SECTION m_CritSec;
	HANDLE m_hBlockSem;
public:
	SimEvent(bool isSignalled);
	~SimEvent();
	void setEvent();
	void resetEvent();
	void pulseEvent();
	void waitFor();
};

#endif
