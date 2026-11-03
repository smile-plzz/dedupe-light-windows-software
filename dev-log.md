# Developer Log

## [2026-11-03 14:10] - Planning: Modern UI/UX, Inline Preview, Advanced Sorting, and Exclusion System
- **Task**: Implement preview, sorting, exclusion, supporting features, UI/UX refinement, modern minimalistic aesthetics, and ship the product.
- **Status**: Research complete; Implementation Plan artifact created and awaiting user review.
- **Workflow State**: Docs initialized (`context.md`, `dev-log.md`, `README.md`, `CHANGELOG.md`). Ready for planning approval before code modifications.


## [2026-11-03 14:23] - Execution & Release: DedupeLight 1.1.0 Complete
- **Features Implemented**:
  1. **Inline Preview Inspector**: Added `PreviewInspector.cs` with image aspect-ratio scaling and dimensions display, text/code snippet reading (first 60-80 lines with line counts), binary hex/ASCII viewer fallback, file metadata, and quick action buttons.
  2. **Side-by-Side Duplicate Comparison**: Enhanced `FileReviewDialog.cs` to compare duplicate copies within the same duplicate set side-by-side with one-click keep/mark selection.
  3. **Set-Aware Sorting**: Added coherent sorting by largest reclaimable sets, smallest sets, most copies, file name, newest, and oldest without breaking duplicate set clustering.
  4. **Category Filter Chips**: Added modern interactive filter chips for All, Images, Videos, Audio, Documents, Code, Archives, and Other.
  5. **Advanced Exclusions & Presets**: Expanded `ExclusionRules.cs` and `ExclusionDialog.cs` to support extension patterns (`*.tmp`, `*.bak`, `*.log`), folder name wildcards (`node_modules`, `.git`, `bin`, `obj`), enable/disable rule checkboxes, and one-click standard presets.
  6. **Right-Click Context Menu**: Added full context menu to `MainForm.cs` (Open, Explorer, Compare, Copy path/hash, Toggle select, Keep only this in set, Keep all in set, Exclude, Recycle).
  7. **Rich Keyboard Shortcuts**: Space (toggle), Enter (open), Del (recycle), Ctrl+F (search), Ctrl+P (toggle inspector), Ctrl+C (copy path), F5 (rescan).
  8. **JSON & CSV Export**: Added JSON and enriched CSV export options.
  9. **Modern & Minimalist Styling**: Upgraded design palette with soft rounded pill buttons, subtle alternating group background tinting, and refined typography.
- **Verification**:
  - `dotnet run --project tests/ScannerChecks.csproj -c Release` (All 29 tests passed).
  - `dotnet build -c Release` (Succeeded with 0 errors and 0 warnings).
  - `dotnet publish -c Release -r win-x64 --self-contained true -o publish` (Standalone single-file executable `publish/DedupeLight.exe` generated cleanly).
- **Status**: Ready for shipping.
