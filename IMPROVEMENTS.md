# GoProImport - Improvement Backlog & Task List

This document tracks prioritized bugs, performance optimizations, feature requests, and architectural improvements for GoProImport.

---

## 1. Critical Bugs & Data Safety (High Priority)

- [x] **Decouple and verify deletions before deleting source files**
  - **Issue**: In `Program.cs`, `deleteList` is populated before copying starts (`Program.cs#L90-L99`). If a file fails to copy, errors out, or is skipped during an overwrite prompt, it remains in `deleteList` and can be permanently deleted from the camera/card.
  - **Task**: Only queue files for deletion if they were actually copied successfully, and verify integrity (at least check target file size matches source, or hash check) before prompting for or executing deletion.
- [x] **Fix `IndexOutOfRangeException` on CLI argument parsing**
  - **Issue**: `Program.cs` accesses `args[++i]` and `args[i+1]` without verifying array bounds (`Program.cs#L47-L59`).
  - **Task**: Add bounds checking for all parameter arguments (`-d`, `-o`, `-i`), or migrate to a robust argument parser like `System.CommandLine`.
- [x] **Fix year vs timestamp time offset discrepancy in `JPEGFile.cs`**
  - **Issue**: `JPEGFile.cs#L21-L24` applies `device.HourOffset` to the directory `year` string, but does not apply it to `timestamp`.
  - **Task**: Apply `device.HourOffset` to both `timestamp` and `year`.
- [x] **Add null-guards on metadata reading in `MP4File.cs`, `JPEGFile.cs`, and `WAVFile.cs`**
  - **Issue**: Calling `.GetInt32(...)` or `.GetDateTime(...)` directly on null directory objects (`qttheader`, `qtmheader`, `exifIFD0`) causes `NullReferenceException` on non-standard, corrupt, or missing metadata files.
  - **Task**: Add safe null-coalescing / fallback logic to file system timestamps when metadata directories are absent.
- [x] **Handle empty camera directories in `DJI_Osmo.cs`**
  - **Issue**: `Directory.GetFiles(path)[0]` throws an `IndexOutOfRangeException` if the camera folder exists but has no recordings.
  - **Task**: Check if `Directory.GetFiles(path)` contains items before indexing index 0.

---

## 2. Copy Performance & Transfer UX (Medium-High Priority)

- [x] **Stream-based chunked copying with live progress & transfer speed**
  - **Issue**: `File.Copy()` blocks during the copy of multi-gigabyte files (4 GB – 12 GB), making the console progress bar freeze for several minutes per file.
  - **Task**: Implement chunked `FileStream` copying with buffer pooling (e.g. 2 MB – 8 MB buffers) reporting live bytes copied, instantaneous/average speed (MB/s), and ETA.
- [x] **Safe console cursor updates**
  - **Issue**: `Console.CursorTop` and `Console.CursorLeft` manipulations in `Program.cs` crash with `IOException` if stdout is redirected or piped.
  - **Task**: Check `Console.IsOutputRedirected` before invoking cursor positioning commands.
- [x] **Parallel multi-device copy support**
  - **Issue**: Transfers from multiple drives/cards are executed sequentially.
  - **Task**: Allow concurrent copying when multiple distinct source devices are connected.

---

## 3. Device & Media Handling (Medium Priority)

- [ ] **Recursive / dynamic GoPro DCIM folder scanning**
  - **Issue**: `GoPro.cs` hardcodes `DCIM\100GOPRO`. Cameras rollover to `101GOPRO`, `102GOPRO`, etc.
  - **Task**: Scan for any folder under `DCIM` ending with `GOPRO`.
- [ ] **GoPro chaptering and burst collision prevention**
  - **Issue**: Renaming purely by timestamp (`YYMMDD_HHmmss`) can result in filename collisions for photo bursts or chaptered video clips recorded in quick succession.
  - **Task**: Preserve chapter/sequence indicators or append a collision counter (e.g., `_01`, `_02`) if a collision occurs.
- [ ] **Proxy video (`.LRV`) and thumbnail (`.THM`) handling**
  - **Issue**: Low-resolution video and thumbnail files are currently ignored.
  - **Task**: Add an option to import `.LRV` files as editing proxies (e.g., renaming with a `_proxy.mp4` suffix).
- [ ] **Video framerate metadata tagging**
  - **Task**: Extract video frame rate from `MP4File` track headers and append it to the filename (e.g., `4K60`, `4K120`, `4K24`).

---

## 4. Configuration & Usability (Medium Priority)

- [ ] **Implement persistent configuration (`Config.cs`)**
  - **Issue**: Default destination path (`D:\GoPro`), known camera models, and offsets are hardcoded.
  - **Task**: Implement JSON configuration persistence via `GoProImport.cfg` or `appsettings.json`.
- [ ] **Dry-run mode (`--dry-run` / `-n`)**
  - **Task**: Add a flag to preview files found, destination paths, and potential overwrites without writing or deleting anything.
- [ ] **Modern CLI options parsing**
  - **Task**: Adopt `System.CommandLine` for structured arguments, flags, help output, and validation.

---

## 5. Architectural & Code Quality Cleanups (Low Priority)

- [ ] **Remove interactive prompts from property getters (`DJI_Osmo.cs`)**
  - **Issue**: `DeviceName` property getter invokes `Console.ReadLine()` if an unrecognized camera is detected.
  - **Task**: Separate device identification / user prompt into an initialization step.
- [ ] **Remove static destination path from `FileItem.cs`**
  - **Issue**: `FileItem.DstPath` is a mutable static property, making concurrency and testing difficult.
  - **Task**: Pass destination context via constructor or method parameters.
- [ ] **Clean up string interpolation formatting and typos in `Program.cs`**
  - Fix `${DstPath}` string interpolation artifact (lines 52 & 56) and typo `"Deleteing files..."` (line 195).
- [ ] **Clean up `DeviceBase.IsDevice` polymorphism anti-pattern**
  - Refactor device detection into a registry or static abstract members on an interface.
