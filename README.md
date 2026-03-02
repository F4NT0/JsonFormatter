# JSON Formatter — Fanto Edition

A desktop application for formatting, validating, and working with JSON data, built with **.NET 10** and **Avalonia UI**.

---

## Screenshots

### Main Screen
> _Add screenshot here: `docs/images/main-screen.png`_

![Main Screen](docs/images/main-screen.png)

### Valid and Formatted JSON
> _Add screenshot here: `docs/images/valid-formatted.png`_

![Valid and Formatted JSON](docs/images/valid-formatted.png)

### Invalid JSON — Error Display
> _Add screenshot here: `docs/images/invalid-json.png`_

![Invalid JSON Error](docs/images/invalid-json.png)

---

## Features

- **Paste or type** JSON directly into the editor
- **Import** a `.json` file — automatically validates and checks if it's already formatted
- **Beautify** — reformats JSON with proper 2-space indentation in place
- **Validation** — real-time feedback showing exactly where the JSON structure breaks (line + position)
- **One-Line Version** — compact single-line preview of valid JSON in the right panel
- **Copy** — copies the current editor content to clipboard
- **Copy One-Line** — copies the compact version to clipboard
- **Clear** — resets the editor and all status indicators
- **Export** — saves the current editor content to a `.json` file
- **Status indicators** — `JSON COMPLIANT` and `JSON FORMATTED` update live as you type or import

---

## Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| [.NET](https://dotnet.microsoft.com/) | 10.0 | Runtime and build system |
| [Avalonia UI](https://avaloniaui.net/) | 11.3.12 | Cross-platform desktop UI framework |
| [Avalonia.AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) | 11.3.0 | Advanced text editor component |
| [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia) | 11.3.12 | Fluent design theme |
| System.Text.Json | Built-in | JSON parsing and serialization |

---

## Project Structure

```
JsonFormatter/
├── App.axaml                          # Application entry, dark theme, global resources
├── App.axaml.cs                       # Application startup
├── MainWindow.axaml                   # Main window UI layout (AXAML)
├── MainWindow.axaml.cs                # Main window code-behind (event handlers, UI sync)
├── Program.cs                         # Entry point, Avalonia bootstrapping
├── app.manifest                       # Windows application manifest
├── JsonFormatter.csproj               # Project file (.NET 10, Avalonia packages)
│
├── ViewModels/
│   └── MainWindowViewModel.cs         # MVVM ViewModel — JSON state, validation, file ops
│
├── Converters/
│   ├── BoolToColorConverter.cs        # IValueConverter: bool → color string
│   └── BoolToCheckConverter.cs        # IValueConverter: bool → ✓ or ✗ symbol
│
└── Highlighting/
    └── JsonHighlightingColorizer.cs   # DocumentColorizingTransformer for JSON syntax colors
```

---

## Architecture

The application follows the **MVVM (Model-View-ViewModel)** pattern:

```
┌─────────────────────────────────────────────────────────┐
│                    MainWindow.axaml                     │
│              (View — pure AXAML layout)                 │
│  TextBox Editor │ Status Dots │ One-Line Panel │ Toolbar │
└────────────────────────┬────────────────────────────────┘
                         │ code-behind wires events
┌────────────────────────▼────────────────────────────────┐
│                  MainWindow.axaml.cs                    │
│  - Hooks Editor.TextChanged                             │
│  - Calls SyncStatus() to update named controls          │
│  - Handles button clicks (Beautify, Import, Export...)  │
└────────────────────────┬────────────────────────────────┘
                         │ reads/writes properties
┌────────────────────────▼────────────────────────────────┐
│              MainWindowViewModel.cs                     │
│  - JsonText       → triggers ValidateJson() on set      │
│  - IsJsonValid    → true if JSON parses successfully    │
│  - IsJsonFormatted → true if matches beautified form    │
│  - ValidationMessage → friendly error with line/pos     │
│  - OneLineJson    → minified single-line version        │
│  - BeautifyJson() → reformats _jsonText in place        │
│  - ImportJson()   → reads file, sets JsonText           │
│  - ExportJson()   → writes JsonText to file             │
└─────────────────────────────────────────────────────────┘
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run

```powershell
dotnet run --project "JsonFormatter.csproj"
```

Or from the project folder:

```powershell
dotnet run
```

### Build

```powershell
dotnet build
```

### Publish (self-contained Windows executable)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

---

## How to Use

1. **Paste JSON** directly into the editor area, or click **↓ Import** to load a `.json` file
2. Check the status panel on the right:
   - **JSON COMPLIANT** → your JSON is structurally valid
   - **JSON FORMATTED** → your JSON already follows the 2-space indentation standard
3. If invalid, an **error bar** appears at the bottom of the editor showing the exact line and position of the problem
4. Click **✦ Beautify** to auto-format the JSON with proper indentation
5. Click **⎘ Copy** to copy the result, or **↑ Export** to save it as a file
6. Click **✕ Clear** to reset everything and start fresh

---

## Key Technical Notes

> For developers working on this project:

- **`AvaloniaUseCompiledBindingsByDefault` must be `false`** in the `.csproj` — setting it to `true` breaks the AXAML source generator and prevents `InitializeComponent` and `x:Name` fields from being emitted.
- **`Text="{ }"` in AXAML** is parsed as an invalid binding expression and silently kills the source generator. Always use `Text="{}{ }"` to escape literal curly braces.
- **`x:DataType` on the Window** must not be set — it conflicts with named control field generation.
- All UI state (status dots, error bar, notification) is managed manually in code-behind via `SyncStatus()` and `SyncNotification()` — no AXAML data bindings for these controls.
- The `TextBox` editor uses `AcceptsReturn="True"` and `AcceptsTab="True"` for multi-line JSON editing.

---

## Author

**F4NT0** — [fantolaboratorio@hotmail.com](mailto:fantolaboratorio@hotmail.com)

---

## License

This project is for personal/internal use. No license defined yet.
