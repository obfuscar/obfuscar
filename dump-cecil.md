# Remove Mono.Cecil Dependency

## Objective
We already have SRM-based readers/writers working in `Metadata/SrmAssemblyReader.cs` / `Metadata/SrmAssemblyWriter.cs`. The goal is to document the migration path so that every remaining reference to `Mono.Cecil` is replaced by SRM abstractions and the package can be removed from all projects, tests, and tools.

## What still talks to Cecil today
- `Obfuscar/Obfuscator.cs` and helpers: the renaming pipeline works on `TypeDefinition`, `MethodDefinition`, `Instruction`, etc. We still log IL info (`ProcessStrings`, `LoadMethodSemantics`) via Cecil types.
- `Obfuscar/AssemblyInfo.cs`, `Project.cs`, `InheritMap.cs`, `TypeTester.cs`: these use `TypeDefinition`/`MethodDefinition` to collect language constructs, evaluate attributes, and determine virtual method groups.
- Helper files (`TypeTester`, `FieldTester`, metadata utilities) rely on Cecil reflection helpers (e.g., `Helper.GetParameterTypeName`, `TypeKey`, `MethodKey`).
- Tests and tools also include `using Mono.Cecil.*` to inspect output assemblies, though the obfuscator itself should be SRM-first.

## Current progress (latest)
- Added SRM abstractions/adapters for properties/events (`IProperty`, `IEvent`, Cecil + SRM handle adapters) and wired property/event skip logic to SRM attribute reader when handles are available.
- Added SRM property/event handle tracking in `SrmAssemblyReader` and SRM assembly-level ObfuscateAssemblyAttribute parsing; `GetAllTypeDefinitions` now consults SRM metadata when available.
- Introduced `MethodSemantics` enum and updated `IMethod`/adapters + `AssemblyInfo` special-name handling to avoid reflection semantics mismatches across TFMs.
- Updated inheritance map base-type/interface discovery to use `IType.BaseTypeFullName` and `IType.InterfaceTypeFullNames`, with adapter caching by full name.
- Improved SRM full-name handling for nested types in `SrmHandleTypeAdapter`/`SrmHandleFieldAdapter` for consistent keys.
- Build status: `dotnet build Obfuscar/Obfuscar.csproj` succeeds (warnings only).

## Migration detail
The migration succeeds when every remaining Cecil contract is matched by SRM adapters, the big metadata consumers adopt the adapters instead of raw Cecil definitions, and unit tests continue passing. Follow these sequential phases:

1. **Inventory & parity across the tree**
   - Run `rg --no-heading -n "Mono.Cecil" Obfuscar Tests -g"*.cs"` and `dotnet list package` on the projects (`Obfuscar/Obfuscar.csproj`, `Tests/ObfuscarTests.csproj`, and any helper CLI) to catalog each Cecil symbol or package dependency. Track whether each occurrence participates in renaming, attribute inspection, IL rewriting, or logging/data emission.
   - Assemble a parity matrix grouped by layers: obfuscation pipeline (`Obfuscator`, `HideStrings`, `ObfuscationAttribute`), metadata helpers/caches (`Helper`, `TypeTester`, `TypeNameCache`), assembly/project analysis (`AssemblyInfo`, `Project`, `AssemblyCache`, `InheritMap`), and tests/tools. For every row attach the SRM surface that should replace the Cecil concept (`SrmHandleTypeAdapter`, `MetadataReader.GetMethodDefinition`, `SrmIlProcessor`, etc.) so nothing is forgotten.
   - Capture the essential “Cecil contracts” currently in use: what `TypeKey`/`MethodKey` rely on, how `Helper.GetParameterTypeName` behaves, what `FieldTester` expects about `FieldDefinition.Attributes`, and how `InheritMap` discovers method overrides. Treat this as the spec for the SRM adapters and regression tests.

2. **Expand the SRM wrapper surface**
   - Ensure `Obfuscar.Metadata.Abstractions` and `Obfuscar.Metadata.Adapters` cover every metadata concept still pulled from Cecil, including nested types, methods, properties, events, fields, parameters, IL handles, custom attributes, and method semantics. Each adapter (`SrmHandleTypeAdapter`, `SrmHandleMethodAdapter`, `SrmHandleFieldAdapter`, `SrmHandlePropertyAdapter`, `SrmHandleEventAdapter`) must expose the properties needed by `AssemblyInfo`, `InheritMap`, or `Helper`.
   - Keep the adapters dual-mode during migration: let the Cecil-backed reader populate the adapter interface now, and switch to SRM handles later. This keeps `AssemblyInfo.Definition`, `Project.LoadAssemblies`, and the metadata predicates connected while the underlying reader flips.
   - Harden the metadata decoders (`SrmTypeNameProvider`, `SrmSignatureDecoder`, `SrmHandleParameterAdapter`) so they produce descriptor structs and strings from SRM blobs. Update helper methods such as `Helper.GetParameterTypeName`, `Helper.GetStringTypeName`, and `TypeNameCache` to consume those descriptors rather than creating temporary Cecil `TypeReference` objects.

3. **Refactor helpers, caches, and metadata testers**
   - Rebuild the key helpers (`TypeKey`, `MethodKey`, `FieldKey`, `PropertyKey`, `EventKey`) so their constructors take `IType`, `IMethod`, or `IField` (the adapter interfaces) instead of raw Cecil definitions. Provide transition helpers that can still accept a `TypeDefinition`/`MethodDefinition` so the change is incremental.
   - Update predicate helpers like `Helper`, `TypeTester`, `FieldTester`, and `TypeNameCache` to iterate SRM handles, query custom attributes via `MetadataReader`, and determine overrides by interpreting `MethodSemantics` tables. Re-implement special-case logic (e.g., `[FixedBuffer]` detection, compiler-generated member tests) using SRM attribute readers and metadata tokens, and wrap hard-to-port Cecil behavior in targeted helper classes for better test coverage.
   - Expand `InheritMap` and `TypeNameCache` caches to key on metadata tokens/handles (for example `(TypeDefinitionHandle, String)` or `(MethodDefinitionHandle, String)`). Record when each cache entry is populated from SRM data to simplify debugging and to make these caches resilient once Cecil is gone.
   - Focus the “predicate/testing helpers” (the classes that feed `Tests/*` assertions) to rely entirely on adapter interfaces. For instance, `TypeTester` should pull base types and interfaces through `IType.BaseTypeHandle`/`IType.GetInterfaces()` instead of reading `TypeDefinition.BaseType`. Update the helpers used by `SkipTypeByDecoratorTests`, `SpecializedGenericsTests`, and `FunctionOverridingTests` so they consume adapter-wrapped metadata handles.

4. **Update assembly/project analysis flows**
   - Have `Project.LoadAssemblies`, `AssemblyInfo`, `AssemblyCache`, and `InheritMap` rely on `Metadata.AssemblyReaderFactory.CreateReader()` and immediately capture the `MetadataReader`, symbol provider, and the adapter root objects (e.g., `SrmHandleTypeAdapter`). Avoid exposing Cecil `AssemblyDefinition`/`TypeDefinition` outside the reader layer.
   - Incrementally replace loops like `info.Definition.MainModule.Types` with adapter-based enumerations (`IAssembly.GetTypes()`, `IType.GetNestedTypes()`). Ensure `AssemblyInfo.Definition` exposes the same data as before (types, custom attributes, nested type hierarchies) while consuming SRM handles internally.
   - Rebuild `InheritMap` using SRM handles: compute base type chains, interface lists, and override relationships through the adapter helpers (`SrmHandleTypeAdapter.BaseTypes`, `SrmHandleMethodAdapter.Attributes`, `SrmHandleMethodAdapter.GetSemantics()`). Store intermediate results keyed by metadata tokens so the cache remains stable as Cecil types disappear.
   - Where `Project.cs` reads assemblies for references or attributes, hook those reads through adapter helper methods so metadata consumers never need to instantiate `TypeDefinition`/`MethodDefinition` directly.

5. **Migrate the obfuscation pipeline**
   - Replace the remaining Cecil-heavy code in `Obfuscator` (method bodies, instructions, attributes, symbol writers) with SRM-friendly abstractions. Introduce or extend `SrmIlProcessor` to parse IL blobs, track branch targets/operands via metadata tokens, and emit modifiable instruction structures without referencing Cecil `Instruction`.
   - Update phases such as `Obfuscator.RenameMethods`, `HideStrings`, `StringSqueeze`, and `ObfuscationAttribute` so they read metadata through adapter interfaces (`IType.Name`, `IMethod.Signature`, `IField.Attributes`). Keep the rename map, obfuscation status, and logging unchanged by translating SRM handles to the existing `ObfuscationMap` keys.
   - Funnel symbol writing through `SrmAssemblyWriter` or a new `ISymbolWriterProvider` abstraction that can switch between the current Cecil `PortablePdbWriterProvider`/`PdbWriterProvider` and the forthcoming SRM emitter. Keep logging (e.g., `ProcessStrings` IL dumps) and ensure they can still produce the expected `StartOfRules`/`PublicClass` output by exposing SRM-based name providers.

6. **Shift tests and tools to SRM**
   - Rework tests (`Tests/HideStringsTests`, `Tests/SpecializedGenericsTests`, `Tests/SkipTypeByDecoratorTests`, `Tests/FunctionOverridingTests`, etc.) to construct `SrmHandle*Adapter` instances from the obfuscated assembly and to perform assertions through adapter helpers. Provide a shared helper (e.g., `Tests/MetadataHelpers.cs`) that wraps the SRM reader and exposes the properties the tests expect from Cecil.
   - Update helper files and predicates so they rely exclusively on the new adapters. Where the tests previously used `Helper.GetParameterTypeName`/`Helper.GetCustomAttributes`, switch them over to the SRM-backed `Helper` overloads, ensuring the tests observe the same metadata metadata semantics post-migration.
   - Once parity is proven, remove the `Mono.Cecil` `using` directives from test files and delete the package references. Also update documentation or CLI tools (`dump-cecil` helper scripts, console reporting) so they describe the SRM approach instead of referencing Cecil types.

7. **Tidy dependencies and verify parity**
   - After the codebase is SRM-first, remove every `Mono.Cecil` package reference (`Obfuscar.csproj`, `Tests/ObfuscarTests.csproj`, preview tools, ILSpy helpers). Delete unused `ThirdParty/ILSpy` files if no longer needed.
   - Run `dotnet test Tests/ObfuscarTests.csproj` (and the filtered commands that hit the existing failures) after each migration batch, confirming the same errors (e.g., `CheckClassHasAttribute` expectations) no longer occur. Drop temporary debugging outputs (like `/tmp/obfuscar_debug.log`) once SRM matches Cecil parity.
   - Keep a follow-up checklist of remaining SRM helper tasks (IL rewriting, custom attribute decoding, symbol emission) in an issue tracker so the final Cecil removal is auditable.

## Refactoring steps by area
- **Helpers & metadata adapters**: annotate `Helper`, `TypeTester`, `FieldTester`, `TypeKey`, and `MethodKey` to rely on the `Obfuscar.Metadata.Abstractions` interfaces. Add adapter helpers that mirror the Cecil APIs seen in `Helper.GetParameterTypeName` or `TypeDefinition.HasCustomAttributes`, and use them throughout `Tests/*` predicates.
- **Assembly analysis**: upgrade `AssemblyInfo`, `Project`, and `AssemblyCache` to use `SrmHandleTypeAdapter`/`SrmHandleMethodAdapter` for attributes, nested types, and interface walks. Drive `InheritMap` purely from SRM metadata tokens but expose the same lookup tables that the renamer pipeline expects.
- **Obfuscation pipeline**: gradually convert `Obfuscator.RenameFields`, `RenameParameters`, `RenameProperties`, and `RenameMethods` to operate on adapter interfaces (`IField`, `IMethod`, `IType`). Keep the logging (e.g., `ProcessStrings`, `LoadMethodSemantics`) and status tracking inside `ObfuscationMap` unchanged while swapping in SRM metadata feeds.
- **Tests/tools**: make helper scripts or CLI tooling build on SRM readers (through `Metadata/` helpers) so the entire repo stops depending on Cecil for verification or diagnostics.

## Validation/testing
- `dotnet test Tests/ObfuscarTests.csproj` must pass without referencing Mono.Cecil.
- Build the console/tool projects (`Console`, `Obfuscar`, `GlobalTools`) to confirm nothing still pulls in the package.
- Run any SRM-specific example from `Metadata/` to ensure parity with the old Cecil behavior.

## Follow‑ups
- Track remaining gaps where SRM lacks a helper (IL rewriting, custom attribute replacement) and add targeted tasks for each.
- Once Cecil is removed, revisit logging/output to remove references to `Mono.Cecil` names and to consolidate the SRM-based pipeline in a single document for future contributors.
