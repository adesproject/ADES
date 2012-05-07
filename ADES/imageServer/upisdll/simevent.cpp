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

#include "simevent.h"

/*
 * This code is based on a tutorial by Martin Harvey on multi-threading:
 * http://www.eonclash.com/Tutorials/Multithreading/MartinHarvey1.1/Ch11.html
 */

SimEvent::SimEvent( bool isSignalled ) {
	m_BlockCount = 0;
	m_Signalled = isSignalled;
	InitializeCriticalSection( &m_CritSec );
	m_hBlockSem = CreateSemaphore( NULL, 0, MAX_THREADS, NULL );
}

SimEvent::~SimEvent() {
	DeleteCriticalSection( &m_CritSec );
	CloseHandle( m_hBlockSem );
}

void SimEvent::setEvent() {
	EnterCriticalSection( &m_CritSec ); 
	{
		m_Signalled = true;
		while (m_BlockCount > 0) {
			if (m_BlockCount > MAX_THREADS) {
				ReleaseSemaphore( m_hBlockSem, MAX_THREADS, NULL );
				m_BlockCount -= MAX_THREADS;
			} else {
				ReleaseSemaphore( m_hBlockSem, m_BlockCount, NULL );
				m_BlockCount = 0;
			}
		}
	} 
	LeaveCriticalSection( &m_CritSec );
}

void SimEvent::resetEvent() {
	EnterCriticalSection( &m_CritSec ); 
	{
		m_Signalled = false;
	} 
	LeaveCriticalSection( &m_CritSec );
}

void SimEvent::pulseEvent() {
	EnterCriticalSection( &m_CritSec ); 
	{
		while (m_BlockCount > 0) {
			if (m_BlockCount > MAX_THREADS) {
				ReleaseSemaphore( m_hBlockSem, MAX_THREADS, NULL );
				m_BlockCount -= MAX_THREADS;
			} else {
				ReleaseSemaphore( m_hBlockSem, m_BlockCount, NULL );
				m_BlockCount = 0;
			}
		}
	} 
	LeaveCriticalSection( &m_CritSec );
}

void SimEvent::waitFor() {
	bool signalled = false;

	EnterCriticalSection( &m_CritSec ); 
	{
		signalled = m_Signalled;
		if (!signalled) m_BlockCount++;
	} 
	LeaveCriticalSection( &m_CritSec );

	if (!signalled) {
		WaitForSingleObject( m_hBlockSem, INFINITE );
	}
}

