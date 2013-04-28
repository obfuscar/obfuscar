#Obfuscar 

[Obfuscar](http://code.google.com/p/obfuscar/) hasn't seen any updates in a long time.  There were a few features that I thought would be useful to improve the bbfuscation of the .Net assemblies that my project (#SNMP Pro editions, http://sharpsnmp.com).  I've done my best to add them to this fork of Obfuscar, and also merged all patches I could find.

The changes since last commit on Google Code include,

* Merged all patches from RemObjects' fork.
  ** Unicode chars can be used as obfuscated names.
  ** Better dependency resolution.
  ** Various other small fixes (I could not yet verify every one, as there is no corresponding unit test cases).

* Merged patches from @AngryAnt (Emil Johansen)'s fork
  ** Korean chars can be used as obfuscated names.
  ** New tags are introduced (I could not yet verify every one, as there is no corresponding unit test cases).

* Automatic exclusion of public types and members so that public API is kept (will make this optional later and disable by default if you don't like this change).

* Better obfuscated name generation, so that derived type members reuse base type members' names.

**Note:** Only a few test cases are currently broken. Obfuscar was migrated from Mono.Cecil old release to version 0.9, so some old cases are no longer valid due to Cecil API breaking changes.

#TODO
1. Clean up all reported issues on Google Code (already cleaned most of them).
1. Document the architecture of the code base and insert more unit test cases.

#Issues and Discussions
If you have a patch to contribute, a feature to request, or a bug to report, please post to https://github.com/lextm/obfuscar/issues

#Donation
If you want to donate to my efforts on this project, please use the following link,

https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=TZATDDPGZUSPL