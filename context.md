# Context

## Product & Goals
DedupeLight is a high-performance, lightweight, native Windows application (.NET 9 WinForms) designed to help users find, review, and safely manage exact duplicate files.

### Primary Objectives
- **Modern & Minimalist Aesthetics**: Refine UI/UX following modern design principles (clean surfaces, harmonious typography, fluent spacing, subtle animations, and intuitive visual cues).
- **Embedded & Enhanced Preview**: Instant inline preview pane for selected items (images, plain text/code snippets, media info, hex preview fallback) alongside a side-by-side comparison modal dialog.
- **Set-Aware Sorting & Smart Filters**: Sort duplicate groups by total reclaimable space, file count, date, or name without breaking set coherence, with category filtering chips (Images, Videos, Audio, Documents, Code, Archives).
- **Flexible Exclusion System**: Expand path exclusions to support pattern wildcards (`*.log`, `*.tmp`), folder name wildcards (`node_modules`, `.git`, `bin`, `obj`), and one-click exclusion presets.
- **Power User Features**: Right-click context menus, rich keyboard shortcuts (Space, Enter, Del, Ctrl+F, Ctrl+A, Ctrl+P, F5), smart selection enhancements, and JSON/CSV reporting.
- **Shipping Ready**: Clean architecture, high-DPI compatibility, comprehensive test suite pass, version bump to 1.1.0, and updated release documentation.

## Tech Stack
- **Framework**: .NET 9.0 Windows Forms (C# 13, Nullable enabled)
- **Target OS**: Windows 10/11 x64
- **Runtime**: Self-contained single-file native Windows executable
- **Testing**: Dedicated verification suite in `tests/ScannerChecks.csproj`
- **Dependencies**: Zero external third-party GUI dependencies; native GDI+ / WinForms controls with custom anti-aliased soft rendering.

## Architecture & Conventions
- `MainForm.cs`: Core window, responsive layout, grid data binding, toolbar actions, and inline inspector panel.
- `ModernControls.cs`: Custom painted controls (`SoftButton`, `SurfaceCard`, `SmoothGrid`, `TagBadge`, `PreviewPanel`).
- `DuplicateScanner.cs`: High-performance background file discovery, 64KB sampling, full SHA-256 calculation, and thread-safe caching.
- `ExclusionRules.cs`: Matching engine for exact paths, directory prefixes, file extension patterns, and folder name wildcards.
- `ExclusionDialog.cs`: Modal management UI for viewing, toggling, importing, and exporting exclusion rules and presets.
- `FileReviewDialog.cs`: Side-by-side comparative inspector for reviewing duplicates within the same group.
- `RecycleOverrideDialog.cs`: High-safety confirmation dialog requiring explicit typed confirmation when all copies in a group are selected.
- `Models.cs` & `AppSettings.cs`: Immutable records and local JSON configuration persistence.

