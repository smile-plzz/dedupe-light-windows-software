<p align="center"><img src="assets/dedupelight.svg" width="96" alt="DedupeLight icon"></p>

# DedupeLight

**Less clutter. More space.**

A lightweight, native Windows app for finding, reviewing, and handling exact
duplicate files. DedupeLight verifies matches with SHA-256, reuses cached hashes,
and lets you protect intentional copies. Nothing is deleted automatically.

**Version 1.1.0 · Windows x64 · MIT license**

## Download

[Download the latest release](https://github.com/smile-plzz/dedupe-light/releases/latest)

Choose **DedupeLight-1.1.0-win-x64.zip**, extract it to a folder, and run
`DedupeLight.exe`. No installer or separate .NET installation is needed.
Release assets include a SHA-256 checksum. Windows may show a SmartScreen
notice because this release is unsigned.

## Use

1. Browse to a folder/drive, or drag it into the window.
2. Choose minimum size (default: 1 MB) and hidden/system scanning preferences.
3. Select **Scan now**. You can cancel while scanning.
4. Review duplicate sets in the coherent set-grouped list.
5. Filter by category chips (`All`, `Images`, `Videos`, `Audio`, `Documents`, `Code`, `Archives`, `Other`).
6. Inspect previews live in the collapsible **Inspector** panel (`Ctrl+P`) or click **Compare** to view duplicates side-by-side.
7. Use **Select extras** to automatically keep the oldest, newest, or shortest-path copy.
8. Right-click any row for instant context actions (open, reveal in Explorer, copy path/hash, keep only this copy, exclude).
9. Recycle, move, copy, or export results (CSV or JSON) when you are ready.

Use **Exclude file**, **Exclude folder**, or pattern rules (`*.tmp`, `node_modules`) for intentional copies.
**Excluded sources** manages saved rules, presets, and imports/exports them as JSON.
Toggle or remove a rule and rescan to restore that source to the results.

## Features

- Exact duplicate detection: file size → sample fingerprint → full SHA-256.
- Cached fingerprints/hashes for unchanged paths, sizes, and modification times.
- Background scans and disk operations, cancellation, skipped inaccessible paths.
- **Embedded Inspector**: Live preview pane for images (with dimensions), text/code files (with line counts), media info, and binary hex fallback.
- **Side-by-Side Comparison**: Compare duplicate copies in the same set side-by-side with one-click keep/recycle selection.
- **Set-Aware Sorting**: Coherent group sorting by reclaimable size, total size, copy count, name, and modification date.
- **Category Filter Chips**: Instant filtering across Images, Videos, Audio, Documents, Code, Archives, and Other.
- **Flexible Exclusions**: Supports exact files, recursive folders, extension patterns (`*.tmp`, `*.bak`), and folder name wildcards (`node_modules`, `.git`, `bin`, `obj`) with built-in presets.
- **Right-Click Context Menu**: Full context menu with Explorer reveal, copy path/hash, selective set keeping, and exclusions.
- **Keyboard Shortcuts**: Space to toggle, Enter to open, Del to recycle, Ctrl+F to search, Ctrl+P to toggle preview, Ctrl+C to copy path, F5 to rescan.
- Double-click opening, Explorer reveal, move/copy, Recycle Bin and CSV/JSON export.
- Explicit per-operation override for recycling every listed copy of a set.
- Modern minimalist native UI, alternating set tinting, rounded controls, hover transitions and keyboard focus.

## Requirements and data

- Windows 10/11, 64-bit.
- Portable, self-contained .NET 9 build.
- Preferences, exclusions and hashes: `%LOCALAPPDATA%\\DedupeLight`.
- Scanning and hashing are local; the application has no network requests.
- Import/export exclusions and exported files contain local paths.

## Documentation

- [User guide and troubleshooting](docs/USER_GUIDE.md)
- [Developer and release guide](docs/DEVELOPMENT.md)
- [Release status and validation](docs/RELEASE_STATUS.md)
- [Changelog](CHANGELOG.md)
- [MIT license](LICENSE)
