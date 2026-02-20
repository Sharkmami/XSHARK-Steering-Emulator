# 🦈 XShark - Steering Emulator

XShark is a lightweight Windows controller emulation tool that converts mouse movement into steering input using ViGEm.

Designed for precision, smooth response and low latency.

---

## ✨ Features

- 🎮 Virtual Xbox Controller (ViGEm)
- 🖱 Mouse-based steering input
- ⚙ Adjustable settings:
  - Deadzone
  - Smoothing
  - Response Curve
  - Auto-Center strength
  - Damping
  - Delay
- 🔁 Auto-reconnect system
- 💾 Automatic config saving
- 🖥 Clean console UI (60 FPS rendering)
- 📦 Self-contained single-file build

---

## 📦 Installation

1. Download the latest release from the **Releases** section.
2. Install the ViGEmBus driver:
   https://github.com/nefarius/ViGEmBus/releases
3. Run `XShark.exe`

---

## 🔧 Requirements

- Windows 10/11 (64-bit)
- .NET 8 Runtime (if not using self-contained build)
- ViGEmBus Driver installed

---

## 🚀 Build From Source

Use this command:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output will be located in:

```bash
bin/Release/net8.0-windows/win-x64/publish/
```

---

## 📁 Project Structure

```
XShark_C.cs   → Core logic & main loop
XShark_U.cs   → Console UI
XShark_E.cs   → Emulator layer (ViGEm)
XShark_I.cs   → Input handling
XShark_M.cs   → Math processing
XShark_cfg.cs → Configuration system
```

---

## 📌 How Releases Work

The GitHub repository contains the source code only.

Compiled executable files (.exe) are provided inside the **Releases** section.

To create a new release:

1. Go to **Releases**
2. Click **Create a new release**
3. Tag version (example: v1.0.0)
4. Upload the compiled `XShark.exe`
5. Publish

---

## ⚠ Disclaimer

This tool is intended for personal use and testing purposes.

The author is not responsible for misuse.

---

## 📜 License

This project is licensed under the MIT License.

---

## 🦈 Author

Sharkmami
