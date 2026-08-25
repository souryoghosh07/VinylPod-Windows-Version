# VinylPod (Windows Edition)

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)

An interactive, ambient media widget for the Windows desktop. 

**Note on Inspiration:** This project is a native Windows port/homage inspired by the beautiful [Original VinylPod for macOS](https://apps.apple.com/us/app/vinylpod-music-widget/id6443989711). I loved the UX of the macOS version and wanted to bring that exact experience to Windows users, so I engineered this version from scratch using C# and native Windows OS APIs.

## Systems Architecture
Unlike standard web-wrapped desktop apps, VinylPod (Windows) communicates directly with the host operating system to manage media state and UI rendering.

* **Native Win32 Interop:** Utilises `DllImport` (P/Invoke) to interface directly with `user32.dll`. This allows the application to manipulate its Z-order via `SetWindowPos`, snapping the UI seamlessly to the bottom-most desktop layer as an interactive live wallpaper.
* **System Media Hooks:** Implements the `GlobalSystemMediaTransportControlsSessionManager` (Windows SDK) to passively listen to and control system-wide media playback (Spotify, browser media) without requiring API keys.
* **Dynamic GPU Rendering:** Features a highly efficient asynchronous pixel-sampling algorithm using `FormatConvertedBitmap`. It parses album artwork in real-time, extracting dominant RGB values to dynamically tint the XAML glassmorphism UI without locking the main thread.

## Installation
1. Go to the [Releases](../../releases) tab.
2. Download the self-contained `VinylPod.exe`.
3. Run the executable (No external .NET runtime installation required).

## Global Hotkeys
Registered via native Windows message loops (`HwndHook`), allowing control regardless of system focus:
* `Ctrl + 1`: Small Widget
* `Ctrl + 2`: Medium Widget
* `Ctrl + 3`: Large Widget
* `Ctrl + 4`: Full Screen - Wallpaper
* `Ctrl + 5`: Full Screen - Bring to Front
