@echo off
:setup
set basepath=%~dp0
set blat=%basepath%mail\blat.exe
set sendemail=%basepath%mail\sendEmail.exe
set log=%basepath%mail\log.txt
set credentials=%basepath%mail\credentials.bat

:arguments
if "%1" == "/?" goto help
if "%1" == ""   goto help
if not exist "%credentials%" goto help

goto start

:help
echo This file is part of Create Synchronicity.
echo Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
echo Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
echo You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see http://www.gnu.org/licenses/.
echo Created by:   Clément Pit--Claudel.
echo Web site:     http://synchronicity.sourceforge.net.
echo.
echo.
echo.
echo This program implements the common post-sync script interface defined in
echo Create Synchronicity's manual. It serves as an interface between Create
echo Synchronicity and a smtp client such as blat.exe, the current default.
echo.
echo The output of blat is sent to "mail\log.txt".
echo Your credentials file is "%credentials%"
echo.
echo To use this script, you need to do this: (total estimated time: ^< 5mn)
echo 1. Open your installation folder, and locate the *credentials-template.bat*
echo    file, in the "scripts\mail" folder. It contains an "SMTP configuration"
echo    section, which you should edit to reflect your own SMTP settings.
echo 2. Rename *credentials-template.bat* to *credentials.bat*. This is to
echo    prevent future updates from overwriting your configuration file.
echo 3. Launch Create Synchronicity, and press Ctrl+O. You are taken to your
echo    configuration folder. Open the .sync file corresponding to the profile 
echo    which you wish to send logs for, and add the following line to it:
echo    Post-sync action:scripts\mail.bat
echo 4. Enjoy!
echo.
echo.
pause

goto end

:start
call "%credentials%"

set profilename=%~1
set success=%~2
set errors=%~3
set body=%~6

if "%success%" equ "True" (
	if "%errors%" equ "0" (
		set subject=Create Synchronicity - %profilename% completed successfully!
	) else (
		set subject=Create Synchronicity - %profilename% completed with %errors% error^(s^).
	)
) else (
	set subject=Create Synchronicity - %profilename% could not complete.
)

if "%username%" neq "" (
	set usr=-u "%username%"
) else (
	set usr=
)

if "%password%" neq "" (
	set pwd=-pw "%password%"
) else (
	set pwd=
)

echo Create Synchronicity: sending logs from %sender% to %recipient%.
"%blat%" "%body%" -charset "utf-8" -8bitmime -overwritelog -server "%server%:%port%" %usr% %pwd% -f "%sender%" -to "%recipient%" -subject "%subject%" -log "%log%"
rem echo. >> "%log%
rem "%sendemail%" -f "%sender%" -t "%recipient%" -u "%subject%" -s "%server%:%port%" -o message-file="%body%" -o message-charset="utf-8" >> "%log%"
goto end

:end

rem Implementations notes
rem =====================
rem Write 'set var=value', not 'set var =value' or 'set var = value'.
rem To access variables in their definition scope (e.g. an 'if' block), call
rem 'Setlocal EnableDelayedExpansion', and !var! instead of %var%. Use
rem 'EndLocal' at the end of the file.
