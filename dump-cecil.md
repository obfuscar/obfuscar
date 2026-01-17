# Remove Mono.Cecil Dependency

## Objective
We already have SRM-based readers/writers working in `Metadata/SrmAssemblyReader.cs` / `Metadata/SrmAssemblyWriter.cs`. The goal is to document the migration path so that every remaining reference to `Mono.Cecil` is replaced by SRM abstractions and the package can be removed from all projects, tests, and tools.

## What still talks to Cecil today
- `Obfuscar/Obfuscator.cs` and helpers: the renaming pipeline works on `TypeDefinition`, `MethodDefinition`, `Instruction`, etc. We still log IL info (`ProcessStrings`, `LoadMethodSemantics`) via Cecil types.
- `Obfuscar/AssemblyInfo.cs`, `Project.cs`, `InheritMap.cs`, `TypeTester.cs`: these use `TypeDefinition`/`MethodDefinition` to collect language constructs, evaluate attributes, and determine virtual method groups.
- Helper files (`TypeTester`, `FieldTester`, metadata utilities) rely on Cecil reflection helpers (e.g., `Helper.GetParameterTypeName`, `TypeKey`, `MethodKey`).
- Tests and tools also include `using Mono.Cecil.*` to inspect output assemblies, though the obfuscator itself should be SRM-first.

## Migration detail
1. **Inventory & parity across the tree**
   - Start from the `rg` results to catalog every `Mono.Cecil` symbol that is still in use: `TypeDefinition`, `MethodDefinition`, `Instruction`, `MethodSemanticsAttributes`, `ILProcessor`, `CustomAttribute`, etc. Capture the intent of each occurrence (renaming, attribute inspection, IL rewriting, logging) so nothing is missed when swapping to SRM.
   - Split the matrix into Obfuscar core, metadata helpers, assembly/project analysis, tests, and tooling. Tag each row with the SRM feature that should replace it (`MetadataReader.GetTypeDefinition`, `MetadataReader.GetMethodDefinition`, the new `Metadata.Adapters`, `SrmAssemblyReader`, etc.).
   - For complicated call sites like `Obfuscator.RenameMethods` or `Project.ListVirtuals`, keep side notes about temporary bridging helpers (e.g., `TypeKey`/`FieldKey` wrappers or `SrmHandle*Adapter`) that must surface the Cecil-like contract that higher layers currently expect.
2. **Build out SRM wrapper surface**
   - The newly added `Obfuscar.Metadata.Abstractions` and `Obfuscar.Metadata.Adapters` packages are the foundation. Flesh out adapters for everything the core helpers need: fields, methods, events, properties, parameters, attributes, and IL tokens as SRM handles.
   - Extend `SrmTypeNameProvider` (and any helper that decodes signatures) so that parsing blobs yields simple strings rather than Cecil `TypeReference` instances. The goal is to keep the higher-level components unaware of which metadata backend supplies names.
   - Provide SRM equivalents for the Cecil conveniences consumed by `Helper`, `TypeTester`, `FieldTester`, `TypeKey`, `MethodKey`, `NameParamSig`, and `ParamSig`. These might live in `Obfuscar.Helpers.SrmExtensions` or companion `SignatureDecoder` helpers until SRM itself supports the same APIs.
3. **Refactor metadata helpers and caches**
   - `TypeKey`/`TypeNameCache` need to stop dereferencing `TypeReference` objects. Instead they should consume the `IType` abstractions, derive namespaces/nested names from SRM `TypeDefinitionHandle`, and keep equality/hashcode semantics intact so the matching logic (e.g., `Matches(TypeReference)`) can be re-implemented using SRM handles.
   - `FieldKey`, `MethodKey`, `PropertyKey`, `EventKey`, and other member descriptors should accept `IField`/`IMethod` (or new SRM handle adapters) so cache keys keep pointing at metadata handles rather than Cecil objects.
   - `TypeTester` (and `FieldTester`, `MethodTester`, etc.) must be reworked to iterate SRM type lists, query custom attributes via `MetadataReader`, and build virtual vs. hidden mappings based on SRM attributes. Add small SRM-specific helpers for `FixedBufferAttribute`, `MethodSemantics`, and `CustomAttributeRecord` resolution so downstream rules still get the semantic data they expect.
4. **Update assembly/project analysis flows**
   - `Project.LoadAssemblies`, `AssemblyInfo`, `AssemblyCache`, and `InheritMap` should rely entirely on `Metadata.AssemblyReaderFactory.CreateReader()` plus the new adapters. As soon as an assembly is opened, cache the `MetadataReader`, materialize handle collections, and hand out `IType`/`IField` instances to the rest of the pipeline.
   - When collecting `AssemblyInfo.Definition` and `Project.AssemblyList`, clamp the caches so both Cecil-backed (for backward compatibility) and SRM-backed readers can feed the same `AssemblyInfo` object; gradually switch producer code from `info.Definition.MainModule.Types` to the SRM equivalents before dropping the Cecil heap.
   - Rebuild `InheritMap`, `AssemblyCache`, and `TypeTester` from the SRM perspective: get base types, interface lists, and virtual overrides by walking handles instead of Cecil references. If needed, store intermediate maps keyed by `MetadataToken` or new handle wrappers to ease future lookups.
5. **Refactor the obfuscation pipeline**
   - Replace `Obfuscator`’s remaining Cecil dependencies (`Mono.Cecil.Cil.Instruction`, `MethodDefinition.Body`, `MethodSemanticsAttributes`, `SymbolWriterProvider`, etc.) with SRM-friendly abstractions. Introduce or expand `SrmIlProcessor` so `RenameMethods`, `StringSqueeze`, and `HideStrings` can mutate IL via a consistent interface.
   - For IL operations, define a small data structure (`InstructionInfo` with opcode, operand handle) that maps to SRM `Instruction` sequences decoded from the metadata blob. Keep a translator that understands branch targeting, variable refs, and parameter refs based on metadata handles so the renamer logic stays intact.
   - Manage symbol writing by letting `SrmAssemblyWriter` decide whether to feed the `Mono.Cecil` PDB/portable PDB backend; once serializers exist for SRM-only metadata, drop the transient `Cecil` bridge altogether.
6. **Migrate tests and tooling**
   - Every test under `Tests/*` and supporting helper such as `Tests/AssemblyHelper`, `TesterTests`, and `SkipTypeByDecoratorTests` should read assemblies via `Metadata.AssemblyReaderFactory.CreateReader()` and the SRM adapters instead of importing `Mono.Cecil`. Keep these test helpers focused on the new abstractions (e.g., `new SrmHandleTypeAdapter(reader, handle)`) so they can run against both SRM and any remaining Cecil-backed code during the transition.
   - Update helper scripts or documentation that currently mention Cecil (e.g., `docs/support`, `Cecil` references in tests) to refer to SRM, and adjust `Tests/ObfuscarTests.csproj` to stop referencing the package once the tests no longer need the Cecil API.
7. **Tidy dependencies**
   - Once the entire call graph resolves through SRM, remove the `Mono.Cecil` package from `Obfuscar/Obfuscar.csproj`, `Tests/ObfuscarTests.csproj`, any preview CLI tool manifests, and the ILSpy helpers no longer used. Clean up `ThirdParty` copies if they are no longer referenced.
   - Run `dotnet test Tests/ObfuscarTests.csproj --filter ...` (as suggested) after each major refactor to confirm the SRM adapters behave like the old Cecil paths. Retain temporary debug logging (like the `/tmp/obfuscar_debug.log` traces) only until coverage parity is confirmed, then remove the logging references.

## Refactoring steps by area
- **Helpers & metadata adapters**: replace `Mono.Cecil` constructors and property access in `TypeKey`, `MethodKey`, and predicate helpers with SRM wrappers. Provide SRM equivalents for `Helper.GetParameterTypeName`, `TypeDefinition.HasCustomAttributes`, etc.
- **Assembly analysis**: update `AssemblyInfo` to rely on SRM `AssemblyDefinition` adapters for caching, attribute enumeration, and type reference collection. Ensure `Project` and `AssemblyCache` are SRM-first and build the `InheritMap` entirely via SRM metadata.
- **Obfuscation pipeline**: gradually convert `Obfuscator.RenameFields/Params/Properties/Events/Methods` to SRM, verifying each phase by keeping the same statuses in `ObfuscationMap`. Add feature flags/logging for IL sections until SRM replacements are stable.
- **Tests/tools**: where possible, update tests to read obfuscated assemblies via SRM instead of Cecil; use SRM in helper scripts or tools to ensure the entire repo no longer needs the Cecil binary.

## Validation/testing
- `dotnet test Tests/ObfuscarTests.csproj` must pass without referencing Mono.Cecil.
- Build the console/tool projects (`Console`, `Obfuscar`, `GlobalTools`) to confirm nothing still pulls in the package.
- Run any SRM-specific example from `Metadata/` to ensure parity with the old Cecil behavior.

## Follow‑ups
- Track remaining gaps where SRM lacks a helper (IL rewriting, custom attribute replacement) and add targeted tasks for each.
- Once Cecil is removed, revisit logging/output to remove references to `Mono.Cecil` names and to consolidate the SRM-based pipeline in a single document for future contributors.
