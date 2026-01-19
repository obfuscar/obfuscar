# Refactor proposal: structured cleanup after SRM migration

Goals
-----

- Reduce technical debt from the Cecil-to-SRM migration by tightening module boundaries.
- Standardize logging/configuration to remove ad-hoc behavior.
- Improve readability and test determinism without changing obfuscation semantics.

Guiding principles
------------------

- Small, safe slices with tests or targeted validation.
- Prefer clear APIs and explicit ownership over helper sprawl.
- Avoid broad mechanical changes without a concrete payoff.

Scope overview
--------------
Phase 1: Diagnostics and logging cleanup
- Replace ad-hoc debug logging (file writes, stray Console output) with LoggerService.
- Wire LoggerService at entrypoints so logs are consistently routed.

Phase 2: Configuration contract hardening
- Enforce absolute paths and document the breaking change.
- Add tests that reject relative paths and variable placeholders.

Phase 3: Resolver modernization
- Replace AssemblyCache with an SRM-native resolver.
- Add targeted resolution tests for tricky cases.

Phase 4: Migration bridge removal
- Remove/replace MigrationBridge and Cecil adapters.
- Tighten boundaries to prevent accidental Cecil usage outside ThirdParty.

Phase 5: Test determinism and IO hygiene
- Centralize temp folder creation/cleanup.
- Add integration tests for SRM read/write and resolution.

Phase 6: API cleanup and docs
- Reduce long methods (e.g., writer) and add XML docs for public APIs.
- Update docs and release notes for v3.

Status tracker
--------------

- [done] Phase 1.1: Remove /tmp/obfuscar_debug.log writes; use LoggerService with env-gated debug output.
- [done] Phase 1.2: Wire LoggerService defaults at app entrypoints (Console/, GlobalTools/, example/).
- [done] Phase 2.1: Document absolute-path config requirement and add tests for placeholder/relative path rejection.
- [done] Phase 3.1: Draft SRM-native resolver design and tests.
- [done] Phase 3.2: Add resolution tests for framework packs, forwarding, and multi-target references.
- [done] Phase 4.1: Inventory and prioritize MigrationBridge call sites; remove adapter shims where only mutable types remain.
- [done] Phase 4.2: Sweep for any remaining adapter-only paths and verify no external callers depend on them.
- [done] Phase 5.1: Normalize temp IO in tests with per-test workspaces and fixture seeding.
- [done] Phase 5.2: Evaluate parallel test execution after IO isolation changes; serialize NameMaker-dependent tests.
- [done] Phase 6.1: Refactor operand encoding in MutableAssemblyWriter for readability.
- [done] Phase 6.2: Split WriteTypeDefinitionsSecondPass into focused helpers.
- [done] Phase 6.3: Split WriteCustomAttributes into focused helpers.
- [done] Phase 6.4: Split EncodeCustomAttributeValue into focused helpers.
- [done] Phase 6.5: Split EncodeTypeToBuilder into focused type-encoding helpers.
- [done] Phase 6.6: Split EncodeIL into offset assignment and instruction encoding helpers.

Notes
-----

- Status will be updated as each slice lands.
- Larger refactors (resolver/bridge removal) should be preceded by tests that capture current behavior.
