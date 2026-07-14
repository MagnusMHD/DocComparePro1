# DocComparePro

<div align="center">

# 📄 DocComparePro

### Modern Document Comparison Tool for Text, PDF and Images

Compare documents with high precision using **Word-to-Word** and **Sentence-to-Sentence** analysis.

Designed with a modern **Glassmorphism UI**, Neon Effects, OCR support and detailed difference highlighting.

***

<https://img.shields.io/badge/.NET-8.0-blue>
<https://img.shields.io/badge/WPF-Windows-512BD4>
<https://img.shields.io/badge/License-MIT-green>
<https://img.shields.io/badge/Status-In%20Development-orange>

</div>

***

# ✨ Features

## Supported File Types

✅ TXT

✅ PDF

✅ PNG

✅ JPG

✅ JPEG

***

## Comparison Modes

### Word-to-Word

Compares every single word exactly.

Detects:

* Changed words
* Missing words
* Added words
* Number differences
* Typing errors

***

### Sentence-to-Sentence

Compares complete sentences.

Detects:

* Changed statements
* Missing sentences
* Reordered content
* Added information

***

## OCR Support

Images can be processed automatically using OCR.

```text
Image
   ↓
OCR
   ↓
Extract Text
   ↓
Comparison
```

Supported:

* PNG
* JPG
* JPEG

***

## PDF Support

Extracts text from PDF documents automatically.

```text
PDF
 ↓
Text Extraction
 ↓
Document Analysis
 ↓
Comparison
```

***

## Highlighting System

### Document A

Differences are highlighted with:

```text
Neon Green
```

Color:

```csharp
#00FFAA
```

***

### Document B

Differences are highlighted with:

```text
Neon Orange
```

Color:

```csharp
#FF8800
```

***

## Statistics Dashboard

Displays:

* Similarity Percentage
* Number of Differences
* Checked Words
* Checked Sentences
* Processing Time

Example:

```text
Similarity:      94.82 %
Differences:     17
Words Checked:   1,425
```

***

# 🖥 User Interface

## Glassmorphism Design

Modern transparent UI elements.

Features:

* Rounded Corners
* Neon Effects
* Dark Theme
* Professional Desktop Layout

***

## Dashboard Layout

```text
┌───────────────────────────────────────┐
│ DocComparePro                    _ □ X│
├───────────────────────────────────────┤
│                                       │
│ Document A      Document B            │
│ Upload Area     Upload Area           │
│                                       │
│ Comparison Settings                   │
│                                       │
│ Start Comparison                      │
│                                       │
│ Similarity Statistics                 │
│                                       │
│ Results A          Results B          │
│                                       │
└───────────────────────────────────────┘
```

***

# ⚙ Comparison Options

Supported settings:

```text
✔ Case Sensitive

✔ Compare Numbers

✔ Compare Punctuation

✔ Ignore Whitespace

✔ OCR for Images

✔ Automatic PDF Reading
```

***

# 🏗 Project Structure

```text
DocComparePro
│
├── App.xaml
│
├── Resources
│   ├── Colors.xaml
│   ├── Styles.xaml
│   └── Animations.xaml
│
├── Models
│   ├── CompareResult.cs
│   ├── DifferenceItem.cs
│   └── DocumentContent.cs
│
├── Services
│   ├── FileLoaderService.cs
│   ├── PdfReaderService.cs
│   ├── OcrService.cs
│   ├── WordComparer.cs
│   └── SentenceComparer.cs
│
├── ViewModels
│   ├── MainViewModel.cs
│   └── RelayCommand.cs
│
├── Views
│   ├── MainWindow.xaml
│   ├── SettingsWindow.xaml
│   ├── AboutWindow.xaml
│   └── ExportWindow.xaml
│
└── Assets
```

***

# 🛠 Technologies

## Frontend

* WPF
* XAML
* MVVM

## Backend

* C#
* .NET 8

## Libraries

### PDF

```text
PdfPig
```

### OCR

```text
Tesseract OCR
```

### MVVM

```text
CommunityToolkit.Mvvm
```

***

# 📦 Required NuGet Packages

```powershell
Install-Package PdfPig

Install-Package Tesseract

Install-Package CommunityToolkit.Mvvm

Install-Package Microsoft.Xaml.Behaviors.Wpf
```

***

# 🚀 Getting Started

## Clone Repository

```bash
git clone https://github.com/yourusername/DocComparePro.git
```

***

## Open Project

```text
Visual Studio 2022
```

Recommended:

```text
.NET 8 SDK
```

***

## Build

```bash
Build Solution
```

or

```bash
dotnet build
```

***

## Run

```bash
dotnet run
```

***

# 📋 Planned Features

## Version 1.1

* Drag & Drop Animation
* Dark/Light Theme
* Faster Comparison Engine

***

## Version 1.2

* DOCX Support
* Excel Support
* Batch Comparison

***

## Version 1.3

* Side-by-Side Synchronised Scrolling
* Difference Navigation Panel
* Search Function

***

## Version 2.0

* AI-Assisted Difference Detection
* Semantic Comparison
* Cloud Reports
* Multi-Language OCR

***

# 🎯 Main Goal

DocComparePro aims to provide a professional desktop solution for comparing:

```text
PDF ↔ PDF

PDF ↔ Image

PDF ↔ TXT

TXT ↔ TXT

Image ↔ Image

Image ↔ PDF
```

with precise detection of differences including:

* Words
* Sentences
* Numbers
* Dates
* Symbols
* Formatting Variations

***

# 👨‍💻 Author

**Mahdi Mohebi**

Apprentice Software Developer

Germany

***

# 📄 License

```text
MIT License
```

Feel free to use, modify and contribute to this project.

***

<div align="center">

### ⭐ If you like this project, consider giving it a star ⭐

</div>
