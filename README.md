<div align="center">

# Polsimer F74LED – SimHub Plugin

![LED Effects Preview](docs/images/led_effects.gif)

<p>
  <img src="https://img.shields.io/badge/SimHub-Plugin-orange">
  <img src="https://img.shields.io/badge/platform-Windows-0078D4?logo=windows&logoColor=white">
  <a href="LICENSE"><img src="https://img.shields.io/github/license/kpulka247/polsimer-simhub-plugin"></a>

</p>

</div>

## Overview

A native C# plugin and telemetry-driven LED profile for the **Polsimer F74LED** steering wheel in **SimHub**. Direct USB HID communication handled internally by SimHub's output manager without external middleware or background utilities.

## Features

* 🔌 **Native HID driver** — Operates directly inside SimHub process memory with 60 Hz hardware connection polling.
* ☀️ **Hardware brightness** — Native integration with SimHub's global brightness slider and Day/Night mode presets.
* 💾 **Persistent settings** — Retains brightness levels, active profiles, and hardware states across restarts.
* ⚡ **Frame state sync** — Automatically resets timeline offsets on USB re-plug and game state changes to prevent frame desynchronization.

![SimHub Interface](docs/images/settings_preview.png)

## Included LED Profile

* **Smart RPM** — Telemetry-matched rev progression supporting GT3 (symmetrical inward) and single-seater/formula (left-to-right) curves.
* **Split-Side Assists** — Dedicated yellow ABS indicator (left 6 LEDs) and deep-blue Traction Control indicator (right 6 LEDs).
* **Shift Alert** — High-visibility cyan strobe triggering on optimal shift point and redline.
* **Speed-Aware Pit Limiter** — 3-stage visual feedback adapting to stationary pit box, rolling speed, and pit limiter threshold.
* **Marshall Flags Suite** — Real-time track status indicators for Yellow, Blue, Green, White, Red, Black, Slippery/Debris, Safety Car, and a 10-second Checkered flag.
* **Damage Alert** — Symmetrical orange pulse responding to aero, suspension, or engine damage telemetry.
* **Engine Startup & Idle Presets** — Ignition sweep sequence and 5 selectable idle wave color schemes (Deep Blue, Crimson Red, Amber & Gold, Polsimer Tribute, Prism Spectrum).

> ⚙️ **Profile customization:** Every effect is built as an independent layer. Inside the **Polsimer F74LED** tab, click **Edit profile** to toggle individual effects on/off, reorder layer priority, adjust colors, or fine-tune trigger thresholds.

> 💡 **Custom profiles:** Using the included `Polsimer_F74LED.ledsprofile` is completely optional. The plugin acts as the native hardware bridge, unlocking SimHub's full LED module for the wheel — you are free to build custom LED profiles from scratch or import any third-party profiles.

## Installation

> ⚠️ **Important:** Newly installed plugins are hidden in SimHub by default. Enable the tab in the sidebar after installation (see step 3).

### ⚡ Auto (Recommended)

1. Download and extract the latest `Polsimer-F74LED-SimHub-vX.X.X.zip` from [Releases](../../releases)
2. Run `setup.exe` (accept the UAC prompt) and click **Install / update**
3. Open **SimHub**, click **Add/remove features** at the bottom of the left sidebar, and check **Polsimer F74LED**
4. Select the new **Polsimer F74LED** tab in the left navigation menu
5. Under **Telemetry Leds**, click **Import profile**, choose `Polsimer_F74LED.ledsprofile`, and load it (or build/select your own custom profile)

### 🛠️ Manual

1. Copy `Polsimer.SimHub.Plugin.dll` into your SimHub installation folder (e.g., `C:\Program Files (x86)\SimHub\` or `D:\SimHub\`)
2. Start SimHub, go to **Add/remove features** (or **Settings → Plugins**), and enable **Polsimer F74LED**
3. Open the **Polsimer F74LED** tab, import `Polsimer_F74LED.ledsprofile`, or create a profile from scratch

## Uninstallation

To remove the plugin:
* Run `setup.exe` and click **Uninstall**, or delete `Polsimer.SimHub.Plugin.dll` directly from your SimHub root directory.

## Troubleshooting

* **Tab missing in SimHub:** Click **Add/remove features** at the very bottom of the left sidebar in SimHub and ensure **Polsimer F74LED** is enabled.
* **LEDs not updating:** Verify the USB connection and check the **Connection status** in the Polsimer F74LED tab.
* **Config persistence:** Close SimHub normally using the window close button or system tray icon rather than terminating the task.