@echo off

rem /*
rem  * Copyright 2007 - 2009 Marek Stój
rem  * 
rem  * This file is part of ImmDoc .NET.
rem  *
rem  * ImmDoc .NET is free software; you can redistribute it and/or modify
rem  * it under the terms of the GNU General Public License as published by
rem  * the Free Software Foundation; either version 2 of the License, or
rem  * (at your option) any later version.
rem  *
rem  * ImmDoc .NET is distributed in the hope that it will be useful,
rem  * but WITHOUT ANY WARRANTY; without even the implied warranty of
rem  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
rem  * GNU General Public License for more details.
rem  *
rem  * You should have received a copy of the GNU General Public License
rem  * along with ImmDoc .NET; if not, write to the Free Software
rem  * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
rem  */

rem ------------------------------------------------------------------

if %1 == "Release" goto :MERGE
goto :FINISH

rem ------------------------------------------------------------------

:MERGE
cd %3

del X%4
ren %4 X%4

%2..\..\Tools\ILMerge.exe /out:%4 X%4 "ImmDocNetLib.dll" "Mono.Cecil.dll"

del X%4
del "ImmDocNetLib.dll"
del "Mono.Cecil.dll"
del *.pdb

rem ------------------------------------------------------------------

:FINISH

rem ------------------------------------------------------------------
