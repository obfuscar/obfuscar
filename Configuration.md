#summary Describes Obfuscar configuration xml
#labels Featured

# Obfuscar Configuration #

Obfuscar accepts a single command line argument, the path to its configuration file.

The configuration file is used to specify what assemblies should be obfuscated, where to find the dependencies for the assemblies, and where the obfuscated assemblies should be saved.

## Variables, `InPath` and `OutPath` ##

The following is a is an example of a minimal configuration.  It is provided in the release as part of the BasicExample:
```
<?xml version='1.0'?>
<Obfuscator>
  <Var name="InPath" value=".\Obfuscator_Input" />
  <Var name="OutPath" value=".\Obfuscator_Output" />

  <Module file="$(InPath)\BasicExampleExe.exe" />
  <Module file="$(InPath)\BasicExampleLibrary.dll" />
</Obfuscator>
```

In the example configuration, two variables are defined, `InPath` and `OutPath`, using the `Var` element, and two assemblies are listed for obfuscation, an executable and a dll.

Variables defined using the `Var` element will be expanded in strings following the definition...After defining `InPath` as follows:
```
  <Var name="InPath" value=".\Obfuscator_Input" />
```

It can be used in another location:
```
  <Module file="$(InPath)\BasicExampleExe.exe" />
```

In addition to being usable like macros, there are a few special variables that have additional effects.  The variable `InPath` is used when trying to find dependencies (the specified path is searched), and the variable `OutPath` is used as the output path for the obfuscated assemblies and the map.  If either `InPath` or `OutPath` is unspecified, they default to the current path (".").

## Modules ##

For each assembly to be obfuscated, there must be a `Module` element.  Assemblies referenced by an assembly specified by a `Module` element must be resolveable, either via Cecil's regular resolve process, or they must be present in the path specified by `InPath`.

Though additional assemblies are loaded for examination, only the specified assemblies will be obfuscated.

## Exclusion by Configuration ##

It is possible to include additional elements within the `Module` elements to skip types (the `SkipTypes` element), methods (the `SkipMethod` element), fields (`SkipField`), properties (`SkipProperty`), and events (`SkipEvent`, of course). Methods can be excluded from string obfuscation by `SkipStringHiding`.

The `SkipNamespace` element specifies a namespace that should be skipped.  All types, methods, fields, etc., within the namespace will be skipped.

The `SkipType` element specifies the name of the type to skip, including the full namespace.  It can also specify whether to skip the method, fields, properties, and/or events within the type.

The `SkipMethod` element specifies the name of the type containing the method, a protection specifier, and a name or regex to match the method.  The protection specifier is currently ignored, but will eventually be used for additional filtering.

The `SkipField` element specifies the name of the type containing the field, a protection specifier, and a name or regex to match the field.  The protection specifier is currently ignored, but will eventually be used for additional filtering.

The `SkipProperty` element specifies the name of the type containing the property, a protection specifier, and a name or regex to match the property.  The protection specifier is currently ignored, but will eventually be used for additional filtering.

The `SkipEvent` element specifies the name of the type containing the event, a protection specifier, and a name or regex to match the event.  The protection specifier is currently ignored, but will eventually be used for additional filtering.

The `SkipStringHiding` element works like the `SkipMethod` element, but specifies within which methods not to obfuscate the string constants. To make it harder to analyze the code, Obfuscar normally replaces string loads by method calls to lookup functions, which incurs a small performance penalty.


A more complete example:
```
  <Module file="$(InPath)\AssemblyX.exe">
    <!-- skip a namespace -->
    <SkipNamespace name="Company.PublicBits" />

    <!-- to skip a namespace recursively, just put * on the end -->
    <SkipNamespace name="Company.PublicBits*" />

    <!-- skip field by name -->
    <SkipField type="Full.Namespace.And.TypeName"
      attrib="public" name="Fieldname" />

    <!-- skip field by regex -->
    <SkipField type="Full.Namespace.And.TypeName"
      attrib="public" rx="Pub.*" />

    <!-- skip type...will still obfuscate its methods -->
    <SkipType name="Full.Namespace.And.TypeName2" />

    <!-- skip type...will skip its methods next -->
    <SkipType name="Full.Namespace.And.TypeName3" />
    <!-- skip TypeName3's public methods -->
    <SkipMethod type="Full.Namespace.And.TypeName3"
      attrib="public" rx=".*" />
    <!-- skip TypeName3's protected methods -->
    <SkipMethod type="Full.Namespace.And.TypeName3"
      attrib="family" rx=".*" />

    <!-- skip type and its methods -->
    <SkipType name="Full.Namespace.And.TypeName4" skipMethods="true" />
    <!-- skip type and its fields -->
    <SkipType name="Full.Namespace.And.TypeName4" skipFields="true" />
    <!-- skip type and its properties -->
    <SkipType name="Full.Namespace.And.TypeName4" skipProperties="true" />
    <!-- skip type and its events -->
    <SkipType name="Full.Namespace.And.TypeName4" skipEvents="true" />
    <!-- skip attributes can be combined (this will skip the methods and fields) -->
    <SkipType name="Full.Namespace.And.TypeName4" skipMethods="true" skipFields="true" />
    <!-- skip the hiding of strings in this type's methods -->
    <SkipType name="Full.Namespace.And.TypeName4" skipStringHiding="true" />

    <!-- skip a property in TypeName5 by name -->
    <SkipProperty type="Full.Namespace.And.TypeName5"
      name="Property2" />
    <!-- skip a property in TypeName5 by regex -->
    <SkipProperty type="Full.Namespace.And.TypeName5"
      attrib="public" rx="Something\d" />

    <!-- skip an event in TypeName5 by name -->
    <SkipProperty type="Full.Namespace.And.TypeName5"
      name="Event2" />
    <!-- skip an event in TypeName5 by regex -->
    <SkipProperty type="Full.Namespace.And.TypeName5"
      rx="Any.*" />

    <!-- avoid the hiding of strings in TypeName6 on all methods -->
    <SkipStringHiding type="Full.Namespace.And.TypeName6" name="*" />
  </Module>
```

To prevent all properties from being obfuscated, set the `RenameProperties` variable to "false" (it's an xsd boolean).  To prevent specific properties from being renamed, use the `SkipProperty` element.  It will also skip the property's accessors, `get_XXX` and `set_XXX`.

To prevent all events from being obfuscated, set the `RenameEvents` variable to "false" (it's also xsd boolean).  To prevent specific events from being renamed, use the `SkipEvent` element.  It will also skip the event's accessors, `add_XXX` and `remove_XXX`.

### Name Matching ###

The `SkipMethod`, `SkipProperty`, `SkipEvent`, `SkipField`, and `SkipStringHiding` elements accept an `rx` attribute that specifies a regular expression used to match the name of the thing to be skipped.
The `SkipType`, `SkipMethod`, `SkipProperty`, `SkipEvent`, `SkipField`, and `SkipStringHiding` elements all accept a `name` attribute that specifies a string with optional wildcards or a regular expression used to match the name of the thing to be skipped.
For elements where both the `name` and `rx` attributes are specified, the `rx` attribute is ignored.
The `name` attribute can specify either a string or a regular expression to match the name of the thing to be skipped.  If the value of the `name` attribute begins with a '`^`' character, the value (including the '`^`') will be treated as a regular expression (e.g., the name '`^so.*g`' will match the string `something`).  Otherwise, the value will be used as a wildcard string, where '`*`' matches zero or more characters, and '`?`' matches a single character (e.g., the wildcard string `som?t*g` will match the string `something`).

This behavior also applies to the value of the `type` attribute of the `SkipMethod`, `SkipProperty`, `SkipEvent`, `SkipField`, and `SkipStringHiding` elements.

### Accessibility Check ###
The `SkipMethod`, `SkipProperty`, `SkipEvent`, `SkipField`, and `SkipStringHiding` elements also accept an `attrib` attribute.
  * Not specified or `attrib=''`: All members are skipped from obfuscation.
  * `attrib='public'`: Only public members are skipped.
  * `attrib='protected'`: Only public and protected members are skipped.
  * All other values for `attrib` generate an error by now.
Members which are `internal` or `protected internal` are not skipped when `attrib` is `public` or `protected`.

Properties and events do not directly have an accessibility attribute, but their underlying methods (getter, setter, add, remove) have. For properties the attribute of the getter and for events the attribute of the add method is used.

## Exclusion by Code ##

There's also some functionality where you can mark types with an attribute to prevent them from being obfuscated...reference Obfuscar.exe and add the `Obfuscate` attribute to your types.  For example, to suppress obfuscation of `X`, its methods, fields, resources, etc.:
```
  [Obfuscate( false )]
  class X { }
```

The `Obfuscate` attribute has a flag, `ShouldObfuscate`, that defaults to true if not set.  The following are equivalent:
```
  [Obfuscate]
  class X { }

  [Obfuscate( true )]
  class Y { }

  [Obfuscate( ShouldObfuscate = true )]
  class X { }
```

And if you only want specific classes obfuscated, you can set the `MarkedOnly` variable to "true" (also an xsd boolean), and apply the `Obfuscate` attribute to the things you want obfuscated.  This is done in the ObfuscarTests project (included w/ the source...it's intended to be a place for unit tests, but for now does little) to obfuscate a subset of the classes.  For example, if `MarkedOnly` is set to true, to include obfuscation of `X`, its methods, fields, resources, etc.:
```
  [Obfuscate]
  class X { }
```

## Control Generation of Obfuscated Names ##

By default all new type and member names generated by Obfuscar are only unique within their scopes. A type with name `A` may be part of namespace `A.A` and `A.B`. The same holds true for type members. Multiple types may have fields and properties with the same name.

When using `System.Xml.Serialization.XmlSerializer` on obfuscated types, the names of generated Xml elements and attributes have to be specified with one of the `XmlXXXXXAttribute` attributes. This is because the original type and member names do not exist any more after obfuscation. For some reasons the XmlSerializer uses the obfuscated names internally even though they are overridden by attributes. Because of that it fails on duplicate names. The same is true for the XML Serializer Generator-Tool (Sgen.exe).

You can work around this problem by setting the `ReuseNames` variable to `false`. In this case the obfuscator does not reuse names for types, fields and properties. The generated names are unique over all assemblies. This setting does not apply to methods.

Add the following line to the configuration file to enable unique names:

```
  <Var name="ReuseNames" value="false" />
```

## Control Hiding of Strings ##
By default Obfuscar hides all string constants by replacing the string load (LDSTR opcode) by calls to methods which return the string from a buffer. This buffer is allocated on startup (in a static constructor) by reading from a XOR-encoded UTF8 byte array containing all strings. This comes with a small performance cost.
You can disable this feature completely by adding the following line to the configuration file:

```
  <Var name="HideStrings" value="false" />
```

If you only want to disable it on specific methods use the `SkipStringHiding` elements.

## Signing of Strongly Named Assemblies ##
Signed assemblies will not work after obfuscation and must be re-signed.

Add the following line to the configuration file to specify the path to your key file. When given a `KeyFile` in the configuration, Obfuscar will sign a **previously** signed assembly with the given key. Relative paths are searched from the current directory and, if not found, from the directory containing the particular assembly.

```
  <Var name="KeyFile" value="key.snk" />
```

If no `KeyFile` is specified, Obfuscar normally throws an exception on signed assemblies. If an assembly is marked _delay signed_, the signing step will be skipped in case no key file is given.

With the special key file name `auto`, Obfuscar uses the value of the AssemblyKeyFileAttribute instead (if existing).