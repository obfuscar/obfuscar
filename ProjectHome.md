Obfuscar is a basic obfuscator for .NET assemblies.  It uses massive overloading to rename metadata in .NET assemblies (including the names of methods, properties, events, fields, types and namespaces) to a minimal set, distinguishable in most cases only by signature.

For example, if a class contains only methods that accept different parameters, they can all be renamed 'A'.  If another method is added to the class that accepts the same parameters as an existing method, it could be named 'a'.

It makes decompiled code _very_ difficult to follow.  The wiki has more details about WhatItDoes.

The current stable release is [Obfuscar 1.5.4](http://code.google.com/p/obfuscar/downloads/detail?name=Obfuscar_1.5.4.zip).

There is also the [Obfuscar 2.0.0 Beta](http://code.google.com/p/obfuscar/downloads/detail?name=Obfuscar_2.0.0.zip) release. This is a port of Obfuscar 1.5.4 to the new Mono.Cecil 0.9 library. By use of this new library Obfuscar now supports .NET 4.0 assemblies. Because there are a lot of subtle changes in Cecil's new API this release of Obfuscar must be considered beta.

**Note:** Since version 1.5 the `attrib` attribute is evaluated correctly. Be sure to check if there are any unintended `attrib` values from the example in your [configuration](http://code.google.com/p/obfuscar/wiki/Configuration) file.

Obfuscar works its magic with the help of [Jb Evain](http://evain.net/blog/)'s fantastic [Cecil](http://www.mono-project.com/Cecil) library, and uses the [C5 Generic Collection Library](http://www.itu.dk/research/c5/) to hold its data.