@echo off
if "%1" == "/?" goto help
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
echo This file is used by Create Synchronicity's mail.bat script to store your
echo e-mail credentials. If you haven't already done so, please edit it to
echo reflect your current SMTP settings.

:start
rem ========================
rem =  SMTP Configuration  =
rem ========================
rem 
rem Customize the following values to match your personal configuration.
rem If you leave username or password empty, Create Synchronicity will try to
rem connect to your SMTP server without authentication.
rem
rem Do not add whitespace after the '=' signs.
rem

set username=
set password=
set server=
set port=25

set sender=
set recipient=

rem ==================
rem =  You're done!  =
rem ==================