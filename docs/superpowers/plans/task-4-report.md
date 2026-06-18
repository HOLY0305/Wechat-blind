# Task 4 Report: GIF Frame Delay Extraction

## What Was Done

Added `GetGifFrameDelays` static method to `PatternManager` class following TDD.

### Changes

**`src/Config/PatternManager.cs`**
- Added `GetGifFrameDelays(string filePath)` static method
- Uses `Image.FromFile` + `GetFrameCount(FrameDimension.Time)` to read frame count
- Reads `PropertyTagFrameDelay` (0x5100) property for per-frame delays
- Converts GIF centisecond units to milliseconds (multiply by 10)
- Returns `int[] { 100 }` as default for single-frame GIFs

**`tests/WechatBlind.Tests/GifHelperTests.cs`**
- Added `GetGifFrameDelays_ThrowsForInvalidFile` — verifies `FileNotFoundException` for missing files
- Added `GetGifFrameDelays_ReadsDelaysFromMinimalGif` — constructs a 3-frame GIF at byte level with known delays (10, 20, 15 centiseconds = 100, 200, 150 ms)
- Added `CreateMinimalGifWithDelays` helper — builds a valid GIF89a binary with proper LZW sub-block format

## Test Results

| Phase | Tests | Result |
|-------|-------|--------|
| RED (before implementation) | 8 total (6 existing + 2 new) | Build failed: CS0117 `GetGifFrameDelays` not defined |
| GREEN (after implementation) | 8 total | All 8 passed in 54ms |

## Concerns

1. **LZW data sensitivity** — The initial test used incorrect LZW sub-block format (`0x04` as "clear code" was actually parsed as sub-block size). Fixed with proper GIF sub-block structure: min-code-size byte, size byte, data bytes, block terminator `0x00`. The corrected LZW data (0x84, 0x51) encodes a solid-color 2x2 frame using codes 4,0,6,0,5 packed LSB-first.

2. **`Image.FromFile` file locking** — The method uses `using var image = Image.FromFile(filePath)` which disposes promptly, but callers should be aware that Windows may briefly lock the file during the read.

3. **Single-frame default** — Returns `int[] { 100 }` (100ms) for single-frame GIFs. This is a reasonable default but callers may want to distinguish "single frame" from "multi-frame with 100ms delay". Not a blocker for current use case.

## Files Modified

- `D:\project\github_project\Wechat blind\src\Config\PatternManager.cs` (lines 231-259)
- `D:\project\github_project\Wechat blind\tests\WechatBlind.Tests\GifHelperTests.cs` (lines 20-105)
