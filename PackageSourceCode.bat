@echo off

rem /*
rem  * Copyright 2007 Marek Stój
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

mkdir "Temp"
mkdir "Temp\Project_ImmDocNet"

xcopy /S /Q Res "Temp\Project_ImmDocNet\Res\"
xcopy /S /Q Src "Temp\Project_ImmDocNet\Src\"
xcopy /S /Q Tools "Temp\Project_ImmDocNet\Tools\"

copy README.txt "Temp\Project_ImmDocNet\README.txt"
copy LICENSE.txt "Temp\Project_ImmDocNet\LICENSE.txt"
copy TODO.txt "Temp\Project_ImmDocNet\TODO.txt"
copy PackageSourceCode.bat "Temp\Project_ImmDocNet\PackageSourceCode.bat"
copy PackageBinary.bat "Temp\Project_ImmDocNet\PackageBinary.bat"

rmdir /S /Q "Temp\Project_ImmDocNet\Src\ImmDocNet\ImmDocNet\bin"
rmdir /S /Q "Temp\Project_ImmDocNet\Src\ImmDocNet\ImmDocNetLib\bin"
rmdir /S /Q "Temp\Project_ImmDocNet\Src\ImmDocNet\ImmDocNet\obj"
rmdir /S /Q "Temp\Project_ImmDocNet\Src\ImmDocNet\ImmDocNetLib\obj"
rmdir /S /Q "Temp\Project_ImmDocNet\Src\ImmDocNet\_ReSharper.ImmDocNet"
del ""Temp\Project_ImmDocNet\Src\ImmDocNet\ImmDocNet.resharper"
del ""Temp\Project_ImmDocNet\Src\ImmDocNet\ImmDocNet.resharper.user"

.\Tools\ImmZip.exe Temp Project_ImmDocNet.zip

rmdir /S /Q "Temp"
