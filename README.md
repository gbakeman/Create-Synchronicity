# Create-Synchronicity
Create Synchronicity is an easy, fast and powerful backup application. It synchronizes files & folders, has a neat interface, and can schedule backups to keep your data safe. Plus, it's open source, portable, multilingual, and very light.

##Features
- Clean, simple interface
- Comprehensive previews
- Pretty logs
- Fleshed-out documentation
- Support for multiple profiles, including groups
- Precise back up selection: which folders to synchronize, optional sub-folder recursion, and file/folder exclusion using regular expressions
- File inclusion and exclusion based on filename, extension, or regular expressions.
- 3 synchronization methods:
  - Mirror
  - One-way incremental
  - Two-ways incremental
- Full scheduling support (daily, weekly, monthly, at a particular time, etc.)
- Catching up: automatically reschedule missed backups
- Fully portable: settings are stored in individual configuration files
- By-volume-label paths: Create Synchronicity can backup directly to "My Usb"\Documents for example, by automatically locating the drive labeled "My Usb".
- Dynamic destinations (%DATE% recognized)
- Automatic translation of environment variables
- Support for UNC (network) and relative paths
- Loose timing: allow file times to differ by a few seconds
- Look-backwards date filters: discard files after a custom number of days.
- File size comparison
- Integrity checks when scanning and copying
- GZip / Bzip2 compression
- Pre-/Post-sync hook scripts: send yourself an e-mail when a backup completes!
- Command line support:
  - Queue multiple profiles
  - Text-only logs (tab-separated fields)
  - Queue groups of profiles
  - Generate application logs

## About
Create Synchronicity was originally created by [Clément Pit--Claudel](http://pit-claudel.fr/clement/) ([GitHub profile](https://github.com/createsoftware)). The website is found [here](http://synchronicity.sourceforge.net/), and the Sourceforge project page [here](https://sourceforge.net/projects/synchronicity/). Create Synchronicity was written in Visual Basic .NET and received the attention of several prominent software websites as well as a strong user base.

The [latest release version](http://synchronicity.sourceforge.net/latest.html), 6.0, was published in March 2012. With the most recent [SVN commit](https://sourceforge.net/p/synchronicity/code/HEAD/tree/) being around June 2013, development appears to have come to a halt. The [GitHub project's](https://github.com/createsoftware/Create-Synchronicity) last commit matches up with a revision nearly two years older ([r1336](https://sourceforge.net/p/synchronicity/code/1336/tree/) versus HEAD's r1700).

##Goals
My goal in creating this project or fork is to get developers and users alike excited in Create Synchronicity again. I'd like to bring the code up to date, put it on new platforms, and ultimately make the greatest .Net-powered file backup utility there is.

###ToDo
- Port code from VB.Net over to C#
- Upgrade framework dependencies to a reasonable version (4.0+?)
- Collect bugs and feature requests from the [old tracker](https://sourceforge.net/p/synchronicity/_list/tickets) and fix/implement them
