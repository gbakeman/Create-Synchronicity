@echo OFF

if "%1" == "/?" goto help
if "%1" == "" goto help
if "%2" == "" goto help
goto start

:help
echo This file is part of Create Synchronicity.
echo Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
echo Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
echo You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see http://www.gnu.org/licenses/.
echo Created by:   Clément Pit--Claudel.
echo Web site:     http://synchronicity.sourceforge.net.
echo.
echo Usage: build.bat v5.0 CHECKSUMS or build.bat r2873 SVN-CHECKSUMS.
echo This script builds all versions of Create Synchronicity.
echo Requires 7z and devenv and md5sum installed in your path.
goto end

:start
set TAG=%~1
set CHECKSUMS=%~2

set ROOT=%CD%
set BUILD=%ROOT%\build
set BIN=%ROOT%\Create Synchronicity\bin

set NAME=Create Synchronicity
set FILENAME=Create_Synchronicity
set LOG="%BUILD%\buildlog-%TAG%.txt"

mkdir "%BUILD%"

(echo Packaging log for %TAG% & date /t & time /t & echo.) > %LOG%

rem Visual Studio doesn't let you define a common font, but some users do not have Verdana. Always build using a common font for all forms, and make sure that it's supported.
echo (**) Changing "Verdana" to Main.LargeFont in interface files.
(
	echo.
	echo -----
	
	for /R "Create Synchronicity\Interface" %%f IN (*.vb) do (
		copy "%%f" "%%f.bak"
		sed -i "s/Me.Font = .*/Me.Font = Main.LargeFont/" "%%f"
	)
) >> %LOG%

echo (**) Updating revision number
(
	echo.
	echo -----
	
	cd "%ROOT%\Create Synchronicity"
	subwcrev.exe "%ROOT%" Revision.template.vb Revision.vb
	cd "%ROOT%"
	
	echo.
	echo -----
) >> %LOG%

echo (**) Building program (release)
	devenv "%ROOT%\Create Synchronicity.sln" /Rebuild Release /Out %LOG%

echo (**) Building program (debug)
	devenv "%ROOT%\Create Synchronicity.sln" /Rebuild Debug /Out %LOG%

echo (**) Building program (Linux)
	devenv "%ROOT%\Create Synchronicity.sln" /Rebuild Linux /Out %LOG%

echo (**) Building installer
(
	echo.
	echo -----
	
	"C:\Program Files (x86)\NSIS\makensis.exe" "%ROOT%\Create Synchronicity\setup_script.nsi"
		
	echo.
	echo -----
	
	move %FILENAME%_Setup.exe "%BUILD%\%FILENAME%_Setup-%TAG%.exe"
) >> %LOG%

echo (**) Building zip files
(
	echo.
	echo -----
	
	cd "%BIN%\Release"
	7z a "%BUILD%\%FILENAME%-%TAG%.zip" "%NAME%.exe" "Release notes.txt" "COPYING" "languages\*"
	7z a "%BUILD%\%FILENAME%-%TAG%-Extensions.zip" "compress.dll" "ICSharpCode.SharpZipLib.dll"
	7z a "%BUILD%\%FILENAME%-%TAG%-Scripts.zip" "scripts\*"
	copy "Release notes.txt" "%BUILD%\release-notes.txt" 
	cd "%ROOT%"

	cd "%BIN%\Debug"
	7z a "%BUILD%\%FILENAME%-%TAG%-DEBUG.zip" "%NAME%.exe" "Release notes.txt" "COPYING" "languages\*"
	cd "%ROOT%"

	cd "%BIN%\Linux"
	7z a "%BUILD%\%FILENAME%-%TAG%-Linux.zip" "%NAME%.exe" "Release notes.txt" "run.sh" "COPYING" "languages\*"
	cd "%ROOT%"
) >> %LOG%

echo (**) Computing checksums
(
	echo.
	echo -----
	echo Updating %CHECKSUMS%
	
	cd %BUILD%
	md5sum "%FILENAME%-%TAG%.zip" "%FILENAME%-%TAG%-DEBUG.zip" "%FILENAME%_Setup-%TAG%.exe" "%FILENAME%-%TAG%-Linux.zip" "%FILENAME%-%TAG%-Extensions.zip" "%FILENAME%-%TAG%-Scripts.zip" >> %CHECKSUMS%
	cd %ROOT%
) >> %LOG%

echo (**) Changing font name back from Main.LargeFont to "Verdana" in interface files.
(
	echo.
	echo -----
	
	for /R "Create Synchronicity\Interface" %%f IN (*.vb) do move /Y "%%f.bak" "%%f"
) >> %LOG%

:end
