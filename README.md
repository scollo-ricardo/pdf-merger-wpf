# PDF & Image Merger

A production-quality WPF desktop application for merging, splitting, extracting, rotating, and compressing PDF files and images. Built with .NET 8 and a modern Windows 11 Fluent UI.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows 10 version 1803 or later)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community or higher) with the **.NET desktop development** workload

## Build & Run

1. Clone or download this repository.
2. Open `PDF Merger WPF.sln` in Visual Studio 2022.
3. Restore NuGet packages: **Tools → NuGet Package Manager → Restore NuGet Packages** (or simply build — VS restores automatically).
4. Press **F5** to build and run, or **Ctrl+F5** to run without debugging.

Alternatively, from the command line:

```bash
cd "PDFMerger"
dotnet restore
dotnet run
```

## Features

| Tab | Feature |
|---|---|
| **Merge** | Drag & drop PDFs and images (PNG/JPG/BMP/TIFF) into a queue, reorder via drag-to-reorder or Move Up/Down, preview pages, merge into a single PDF. |
| **Split** | Load a PDF, drag pages into 1–10 named output columns, save each column as a separate PDF. |
| **Extract** | Load a PDF, select pages (multi-select), export each page as a PNG or JPG at a chosen DPI. |
| **Rotate** | Load a PDF, apply 90°/180° rotations per page, preview the rotation visually, save the result. |
| **Compress** | Load a PDF, choose Low/Medium/High compression, see before/after file size and % reduction. |
| **Settings** | Switch Light/Dark theme, set default output folder and merge filename, toggle auto-open folder. Settings are persisted in `%LOCALAPPDATA%\PDFMerger\settings.json`. |

## Technology

- **.NET 8 WPF** — target `net8.0-windows10.0.17763.0`
- **ModernWpfUI 0.9.6** — Windows 11 Fluent design system
- **iText7 8.0.5** — PDF merge, split, rotate, compress
- **Windows.Data.Pdf** (built-in WinRT) — high-quality page rendering for previews and image extraction

## License

Copyright © 2026 Ricardo Scollo. All rights reserved.
