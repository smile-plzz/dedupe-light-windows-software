# DedupeLight 1.1.0 release status

Version 1.1.0 is a major feature update delivering modern minimalist UI/UX,
embedded live inspection, side-by-side duplicate comparison, set-aware sorting,
category filter chips, pattern and wildcard exclusions, right-click context menu,
and rich keyboard shortcuts.

## Evidence & Verification

- Scanner and regression test suite passed in .NET 9.0 (`tests/ScannerChecks.csproj`).
- Regression checks cover exact duplicate verification, warm cache hits, full hash invalidation, corrupt cache recovery, exact file exclusions, folder prefix boundaries, extension pattern exclusions, folder name wildcards, rule disable toggles, file categories, presets population, settings persistence, and cancellation.
- Standalone publication succeeded for `win-x64` with self-contained single-file compilation (`publish/DedupeLight.exe`).
- Verified zero compiler warnings and zero build errors under C# nullable and Windows Forms analyzer rules.

## Additions in 1.1.0

- **Embedded Preview Inspector**: Collapsible live inspector panel (`Ctrl+P`) for images, text/code snippets, media info, and binary hex fallback.
- **Side-by-Side Duplicate Comparison**: Side-by-side comparison modal with one-click keep/mark selection.
- **Set-Aware Sorting**: Coherent group sorting by largest sets, smallest sets, most copies, file name, newest, and oldest.
- **Category Filter Chips**: One-click category filtering for All, Images, Videos, Audio, Documents, Code, Archives, and Other.
- **Pattern & Wildcard Exclusions**: Support for extension patterns (`*.tmp`, `*.bak`) and folder name wildcards (`node_modules`, `.git`, `bin`, `obj`) with one-click presets.
- **Right-Click Context Menu & Shortcuts**: Explorer reveal, copy path/hash, selective set keeping, Space/Enter/Del shortcuts.
- **JSON & CSV Export**: Export comprehensive scan results with group metadata, file counts, and hashes.
