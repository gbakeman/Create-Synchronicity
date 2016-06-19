'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Friend Module Branding
    Public Const Brand As String = "Create Software"
    Public Const Name As String = "Create Synchronicity"
    Public Const Web As String = "http://synchronicity.sourceforge.net/"
    Public Const CompanyWeb As String = "http://createsoftware.users.sourceforge.net/"
    Public Const Support As String = "mailto:createsoftware@users.sourceforge.net"
    Public Const License As String = "http://www.gnu.org/licenses/gpl.html"
    Public Const BugReport As String = "http://sourceforge.net/tracker/?group_id=264348&atid=1130882"

    Public Const UpdatesTarget As String = Branding.Web & "update.html"
    Public Const UpdatesUrl As String = Branding.Web & "code/version.txt"
    Public Const UpdatesSchedulerUrl As String = Branding.Web & "code/scheduler-version.txt"
    Public Const UpdatesFallbackUrl As String = Branding.CompanyWeb & "code/synchronicity-version.txt"

    Public Const Help As String = Branding.Web & "help.html"
    Public Const SettingsHelp As String = Branding.Web & "settings-help.html"
    Public Const Contribute As String = Branding.Web & "contribute.html"
End Module