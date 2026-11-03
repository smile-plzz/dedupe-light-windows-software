# Changelog

## 1.1.0 — 2026-11-03

Major feature release introducing embedded inspection, side-by-side comparison, set-aware sorting, advanced wildcard exclusions, and modern minimalist UI/UX.

- **Embedded Preview Inspector**: Collapsible live inspector panel (`Ctrl+P`) for images (dimensions, auto-zoom), text/code snippets (line numbers, encoding), media info, and hex binary fallback.
- **Side-by-Side Duplicate Comparison**: Compare duplicate copies within the same duplicate set side-by-side with one-click keep/mark actions.
- **Set-Aware Sorting**: Coherent group sorting by largest sets, smallest sets, most copies, file name, newest, and oldest without breaking duplicate set clustering.
- **Category Filter Chips**: Fast one-click category filtering for All, Images, Videos, Audio, Documents, Code, Archives, and Other.
- **Pattern & Wildcard Exclusions**: Exclude file extension patterns (`*.tmp`, `*.bak`, `*.log`), folder name wildcards (`node_modules`, `.git`, `bin`, `obj`), with rule enable/disable checkboxes and built-in presets.
- **Right-Click Context Menu**: Full context menu on duplicate grid for opening, revealing in Explorer, comparing copies, copying paths/hashes, selective marking, and exclusions.
- **Rich Keyboard Shortcuts**: Space to toggle, Enter to open, Delete to recycle, Ctrl+F to search, Ctrl+P to toggle preview, Ctrl+C to copy path, F5 to rescan.
- **Modern Minimalist Visuals**: Refined WinForms design system with soft pill buttons, alternating duplicate set tinting, and streamlined controls.
- **JSON & CSV Export**: Export comprehensive results with group metadata, file counts, and hashes in JSON and CSV formats.

## 1.0.0 — 2026-10-13

Initial finalized portable Windows release.

- Exact duplicates verified by size, sample fingerprints and full SHA-256.
- Cached hashes, background scanning/operations and responsive cancellation.
- Search, sorting, keep-copy rules, space estimates, CSV and file actions.
- Persistent file/folder exclusions with JSON import/export and restoration.
- Per-operation typed override for recycling every listed copy.
- In-app image and optional media review; default-app double-click opening.
- Minimal native styling, hover transitions, detail line and tooltips.
- Multi-resolution icon, product metadata, About information and user guides.
- Regression checks, versioned ZIP packaging, checksums and GitHub releases.
