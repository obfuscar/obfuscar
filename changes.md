# Obfuscar 3.0 — release notes

These notes summarize the main user-facing changes and notable fixes included in the 3.0 beta releases.

## 3.0 Beta 7 (2026-04-28)

Highlights

- Fixed a few assembly writer issues.

## 3.0 Beta 6 (2026-04-06)

Highlights

- Fixed base type missing issue. #590

## 3.0 Beta 5 (2026-04-05)

Highlights

- Added in-place replacement support.
- Resolved missing entry point. #588

## 3.0 Beta 4 (2026-03-07)

Highlights

- Fixed embedded BAML. #584
- Improved exception handling. #583

## 3.0 Beta 3 (2026-02-22)

Highlights

- P/Invoke support: enhanced module reference handling in assembly reader and writer, with comprehensive test coverage for P/Invoke scenarios.

## 3.0 Beta 2 (2026-02-06)

Highlights

- Performance and correctness refactors: multiple internal refactors to type/method resolution, IL processing, and attribute handling that improve correctness and pave the way for future optimizations.
- Bug fixes: improvements to reference resolution, instruction handling, and edge-case IL error handling. Expanded test coverage for complex metadata shapes and obfuscation scenarios.
- Notable PR/issue references: #225, #579, #546, #43, #186, #87, #251, #123, #316, #207, #548

Breaking changes

- #568 is a breaking change that tightens the semantics of `ObfuscationAttribute(ApplyToMembers=false)`. Type-level attributes with `ApplyToMembers=false` will no longer implicitly apply to members. If your code relies on this behavior, you will need to audit your configuration and add explicit member-level attributes where necessary.

## 3.0 Beta 1 (2026-01-22)

Highlights

- SRM migration: core metadata pipeline refactored from Mono.Cecil to SRM-backed mutable metadata abstractions.
- Strict configuration validation: `InPath`/`OutPath`/`LogFile` and related path attributes now require absolute paths; variable substitution like `$(...)` is deprecated and rejected. Tests were added to enforce this.

Breaking changes

- Mono.Cecil is no longer used. All internal metadata handling is now based on SRM. This should be transparent to users, but the internal implementation is completely different, so error handling and edge cases may differ. If you encounter issues, please report them with details about your assembly and configuration.
- Configuration files must use absolute paths. Remove `$(...)` variable placeholders or expand them during your build/CI pipeline before invoking Obfuscar. The workaround for this is to generate an absolute-path configuration file at build time (for example, using MSBuild tasks or scripts) and pass that to Obfuscar. You can study the example repository for guidance on how to do this.
