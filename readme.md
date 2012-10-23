#Obfuscar revisited

[Obfuscar](http://code.google.com/p/obfuscar/) hasn't seen any updates in a long time.  There were a few features that I thought would be useful to improve the Obfuscation of the .Net assemblies that [Unity3D](http://unity3d.com) compiles.  I've done my best to add them to this fork of Obfuscar.

New tags:

* `typeinherits`: This means that the skip rule applies if the type or declaring type of a field or method inherits from this type.  The type must be fully qualified (e.g. `System.Object`)
* `static`: This is a true/false value where the skip rule only applies if the type, field, or method is static or not.
* `serializable`: This is a true/false value where the skip rule applies if the type is serializable, or the field is a public field of a serializable type.
* `decorator`: This means that the skip rules applies if the field has this specific attribute assigned to it (e.g. `System.Diagnostics.Conditional`).

Other changes:

* When Obfuscar renames types, field, methods, etc. it will use characters from the Korean unicode character set.  This is to make things slightly more annoying for people in the western hemisphere trying to reverse engineer your code.
* The core of Obfuscar is now a .dll so it can be easily run from inside Unity.

**Note:** All tests are currently broken.  I didn't do that, it happened when Obfuscar moved to 2.0 Beta and started using Mono.Cecil version 0.9.
