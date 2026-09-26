# Polsimer F74LED – SimHub Plugin

A native C# plugin and telemetry-driven LED profile for the Polsimer F74LED steering wheel in SimHub. The plugin connects to the wheel over USB HID and exposes its 12 LEDs to SimHub's LED effects system.

![LED effects preview](docs/images/led_effects.gif)

## Features

- Native HID driver with connection monitoring
- SimHub brightness and Day/Night preset integration
- Persistent plugin settings
- LED effects for RPM, shift alerts, ABS, traction control, pit limiter, flags, damage, startup, and idle
- Customizable effect layers and trigger thresholds through SimHub's profile editor

![SimHub interface](docs/images/settings_preview.png)

## Included LED profile

`Polsimer_F74LED.ledsprofile` is an optional example profile. Import it through **Telemetry LEDs** in SimHub, or create and use your own profile with the plugin.

## Installation

1. Download and extract the latest `Polsimer-F74LED-SimHub` ZIP from [GitHub Releases](../../releases).
2. Close SimHub, run `setup.exe`, and choose **Install / update**.
3. In SimHub, open **Add/remove features** and enable **Polsimer F74LED**.
4. Open the **Polsimer F74LED** tab. To use the included effects, import `Polsimer_F74LED.ledsprofile` from **Telemetry LEDs**.

To install manually, copy `Polsimer.SimHub.Plugin.dll` from the package into the SimHub installation folder, then enable the plugin in **Add/remove features**. Import the profile separately if desired.

To uninstall, run `setup.exe` and choose **Uninstall**, or remove the plugin DLL from the SimHub folder.

## Build

The plugin targets .NET Framework 4.8 and references assemblies supplied by a local SimHub installation. The paths are currently set in `src/Polsimer.SimHub.Plugin.csproj`; update them if SimHub is installed elsewhere. The installer targets .NET 8 for Windows.

Run `build.ps1` in PowerShell to build the plugin and installer and create a ZIP under `release/`. The release ZIP is intended to be uploaded to GitHub Releases, not committed to the repository.

## Troubleshooting

- **Plugin tab is missing:** Enable **Polsimer F74LED** under **Add/remove features** in SimHub.
- **LEDs do not update:** Check the wheel's USB connection and the plugin's connection status.
- **Settings do not persist:** Close SimHub normally so it can save its settings.
