# DocComparePro

DocComparePro ist eine fertige WPF-Desktopanwendung zum lokalen Vergleich von Dokumenten. Die Anwendung läuft auf .NET 8, verwendet MVVM und trennt Benutzeroberfläche, Dateiverarbeitung, Vergleichslogik, Export und Logging klar voneinander.

## Unterstützte Formate

- TXT
- PDF
- DOCX
- PNG
- JPG / JPEG

Bilddateien werden optional mit Tesseract OCR verarbeitet.

## Funktionen

- Dateiauswahl und Drag-and-drop für beide Dokumente
- Wortweiser oder satzweiser Vergleich
- Erkennung von gleichen, hinzugefügten, entfernten und geänderten Inhalten
- optionale Berücksichtigung von Groß-/Kleinschreibung
- optionale Berücksichtigung von Zahlen und Satzzeichen
- Normalisierung von Leerzeichen
- Ähnlichkeitswert in Prozent
- Anzeige von Unterschiedsanzahl, geprüften Einheiten und Verarbeitungszeit
- getrennte Vorschau beider Dokumente
- strukturierte Unterschiedsliste
- HTML- und CSV-Export
- asynchrone Dateiverarbeitung
- verständliche Fehlermeldungen
- technisches Fehlerprotokoll unter `%LocalAppData%/DocComparePro/Logs/application.log`
- automatisierte xUnit-Tests
- GitHub-Actions-Build auf Windows

## Architektur

```text
DocComparePro/
├── App.xaml
├── App.xaml.cs
├── Core/
│   ├── Models.cs
│   ├── DocumentReader.cs
│   ├── ComparisonEngine.cs
│   ├── ReportExporter.cs
│   └── FileLogger.cs
├── ViewModels/
│   └── MainViewModel.cs
└── Views/
    ├── MainWindow.xaml
    └── MainWindow.xaml.cs

DocComparePro.Tests/
└── ComparisonEngineTests.cs
```

### Verantwortlichkeiten

- `Views`: Darstellung, Bindings und ausschließlich UI-spezifische Ereignisse
- `ViewModels`: Zustand, Commands und Ablaufsteuerung
- `DocumentReader`: TXT-, PDF-, DOCX- und OCR-Verarbeitung
- `ComparisonEngine`: Tokenisierung, LCS-Diff und Statistiken
- `ReportExporter`: HTML- und CSV-Berichte
- `FileLogger`: persistente technische Fehlerprotokolle
- `Models`: unveränderliche Domain-Modelle

## Technologien

- C# 12
- .NET 8
- WPF
- MVVM mit CommunityToolkit.Mvvm
- PdfPig
- DocumentFormat.OpenXml
- Tesseract OCR
- xUnit
- GitHub Actions

## Projekt starten

Voraussetzungen:

- Windows 10 oder Windows 11
- Visual Studio 2022 mit Workload **.NET-Desktopentwicklung**
- .NET 8 SDK

```bash
git clone https://github.com/MagnusMHD/DocComparePro1.git
cd DocComparePro1
dotnet restore DocComparePro.slnx
dotnet build DocComparePro.slnx --configuration Release
dotnet run --project DocComparePro/DocComparePro.csproj
```

## OCR einrichten

Für Bildvergleiche müssen diese Dateien unter `DocComparePro/tessdata` liegen:

```text
tessdata/
├── deu.traineddata
└── eng.traineddata
```

Die Dateien werden beim Build in den Ausgabeordner kopiert. TXT-, PDF- und DOCX-Vergleiche funktionieren auch ohne OCR-Sprachdateien.

## Vergleichsverfahren

Der Text wird abhängig vom Modus in Wörter oder Sätze zerlegt. Eine Longest-Common-Subsequence-Matrix erzeugt anschließend einen deterministischen Diff. Direkt aufeinanderfolgende Entfernen-/Hinzufügen-Paare werden als Änderung zusammengefasst.

## Qualität

- klare MVVM-Trennung
- Abhängigkeiten über Interfaces
- XML-Dokumentationskommentare für öffentliche C#-APIs
- gezielte `//`-Kommentare für nicht offensichtliche Entscheidungen
- keine Geschäftslogik im Window-Code-behind
- Nullable Reference Types
- Warnungen als Buildfehler
- automatisierte Tests und Windows-CI

## Autor

**Mahdi Mohebi**  
Apprentice Software Developer, Germany

## Lizenz

MIT