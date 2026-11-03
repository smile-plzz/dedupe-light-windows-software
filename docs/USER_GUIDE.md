# DedupeLight 1.1 User Guide

## Install and update

Download the portable ZIP from GitHub Releases. Extract all files, then run
`DedupeLight.exe`. Updating means closing the old app and extracting the new
package. Preferences, cached hashes, and exclusions remain in `%LOCALAPPDATA%\DedupeLight`.
No administrator privileges or separate .NET runtime are required.

## Scan and select

1. Choose a folder or drive via **Browse**, paste a path, or drag and drop a folder into the window.
2. The default minimum size is 1 MB; set 0 to include smaller files. Hidden and system files are omitted unless enabled. Directory junctions and reparse points are not followed to avoid recursion loops. Inaccessible locations are skipped safely.
3. Select **Scan now** (or press `Enter` in the path box or `F5`).
4. Duplicate sets are displayed with subtle alternating background tinting so copies belonging to the same set are unmistakably distinct.

## Sorting and category filtering

- **Category chips**: Click `All`, `Images`, `Videos`, `Audio`, `Documents`, `Code`, `Archives`, or `Other` to focus instantly on specific file types.
- **Set-aware sorting**: Use the sort dropdown to order groups by:
  - *Largest sets first* (default — maximizes recoverable storage quickly)
  - *Smallest sets first*
  - *Most copies first*
  - *File name (A-Z)*
  - *Newest modified*
  - *Oldest modified*
- Clicking grid column headers sorts sets coherently, preserving duplicate pairs together.

## Live inspector and side-by-side comparison

- **Embedded inspector (`Ctrl+P`)**: Selecting any row displays an instant preview in the collapsible right-side pane:
  - Images: aspect-ratio preserved preview with pixel dimensions.
  - Text & Code: first 60–80 lines with line counts and monospaced readability.
  - Binary/Other: offset hex and ASCII dump.
  - Metadata: full path with one-click copy, SHA-256 hash copy, and Explorer reveal.
- **Side-by-side comparison**: Click **Compare** (or double-click) to open the comparative review dialog. Select another copy from the dropdown to compare two duplicates side-by-side with one-click "Keep Left" or "Keep Right" selection.

## Context menu and shortcuts

Right-click any duplicate row for instant actions:
- **Open in default app** (`Enter`)
- **Show in File Explorer**
- **Compare with other copies…**
- **Copy full path** (`Ctrl+C`)
- **Copy SHA-256 hash**
- **Toggle mark for recycle** (`Space`)
- **Keep ONLY this copy in set** (marks all other copies in that set)
- **Keep ALL copies in set** (clears marks for that set)
- **Exclude this file / folder / extension**
- **Recycle selected** (`Delete`)

Other keyboard shortcuts:
- `Ctrl+F`: Focus search filter
- `Ctrl+P`: Toggle inspector panel
- `F5`: Rescan current folder

## Intentional copies and exclusions

- **Exclude file**: Skips only selected file paths.
- **Exclude folder**: Skips containing folders recursively.
- **Pattern & wildcard rules**:
  - Extension patterns: `*.tmp`, `*.bak`, `*.log`, `*.cache`, `Thumbs.db`, `.DS_Store`
  - Folder name wildcards: `node_modules`, `.git`, `.vs`, `bin`, `obj`, `__pycache__` (skips any folder with that name anywhere in the tree).
- **Excluded sources dialog**:
  - Add folders, files, or custom patterns.
  - **Add presets**: Adds common developer, cache, and system exclusion presets with one click.
  - Checkboxes allow temporarily disabling rules without deleting them.
  - Import and export exclusion rules as JSON.

## Recycle, transfer, and export

- **Recycle**: Sends selected files to the Windows Recycle Bin (`Delete`).
- **Recycle override**: If every listed copy of a set is selected, the app requires typing `RECYCLE` to confirm before proceeding.
- **Move / Copy**: Transfer extra copies to an archive directory with automatic collision renaming (`stem (2).ext`).
- **Export**: Save scan results as either CSV or JSON with full group IDs, paths, and SHA-256 hashes.
