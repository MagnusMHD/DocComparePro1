# DocComparePro

DocComparePro ist eine WPF-Desktopanwendung zum lokalen Vergleich von Dokumenten. Die Anwendung läuft auf .NET 8, verwendet MVVM und trennt Benutzeroberfläche, Dateiverarbeitung, Vergleichslogik, Export und Logging klar voneinander.

## Unterstützte Formate

- TXT
- PDF
- DOCX
- PNG
- JPG / JPEG

Bilddateien werden optional mit Tesseract OCR verarbeitet.

## Funktionen

- Dateiauswahl und Drag-and-drop für beide Dokumente
- wortweiser oder satzweiser Vergleich
- Erkennung von gleichen, hinzugefügten, entfernten und geänderten Inhalten
- Tippfehler und ähnliche Ersetzungen über Levenshtein-Distanz erkennen
- anteilige Ähnlichkeit für geänderte Wörter und Sätze
- Unterschiede direkt in beiden Dokumentansichten farbig markieren
- synchrones horizontales und vertikales Scrollen
- Auswahl eines Unterschieds in Dokumentansicht und Ergebnistabelle synchronisieren
- laufenden Vergleich abbrechen
- echten Fortschritt des Vergleichs anzeigen
- optionale Berücksichtigung von Groß-/Kleinschreibung
- optionale Berücksichtigung von Zahlen und Satzzeichen
- Normalisierung von Leerzeichen
- Ähnlichkeitswert in Prozent
- Anzeige von Unterschiedsanzahl, geprüften Einheiten und Verarbeitungszeit
- strukturierte Unterschiedsliste
- HTML- und CSV-Export
- asynchrone Dateiverarbeitung
- verständliche Fehlermeldungen
- technisches Fehlerprotokoll unter `%LocalAppData%/DocComparePro/Logs/application.log`
- automatisierte xUnit-Tests
- GitHub-Actions-Build auf Windows

## Farbliche Markierung

- **Rot:** Inhalt wurde aus Dokument A entfernt
- **Grün:** Inhalt wurde in Dokument B hinzugefügt
- **Gelb:** ähnlicher Inhalt wurde geändert
- **Blau:** aktuell ausgewählter Unterschied
- **Transparent:** Inhalt ist identisch

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

- `Views`: Darstellung, Bindings, Drag-and-drop und synchrones Scrollen
- `ViewModels`: Zustand, Commands, Fortschritt, Abbruch und Ablaufsteuerung
- `DocumentReader`: TXT-, PDF-, DOCX- und OCR-Verarbeitung
- `ComparisonEngine`: Tokenisierung, LCS-Diff, Levenshtein-Bewertung und Statistiken
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

Der Text wird abhängig vom Modus in Wörter oder Sätze zerlegt. Eine Longest-Common-Subsequence-Matrix erzeugt einen deterministischen Diff. Direkt benachbarte entfernte und hinzugefügte Einheiten werden über ihre normalisierte Levenshtein-Ähnlichkeit bewertet. Ausreichend ähnliche Paare werden als Änderung dargestellt; vollständig unterschiedliche Einheiten bleiben getrennt hinzugefügt und entfernt.

Die Vergleichsengine unterstützt `CancellationToken` und meldet ihren Fortschritt über `IProgress<int>`. Dadurch bleibt die Oberfläche bei großen Dokumenten bedienbar und der Benutzer kann den Vorgang kontrolliert abbrechen.

## Qualität

- klare MVVM-Trennung
- Abhängigkeiten über Interfaces
- XML-Dokumentationskommentare für öffentliche C#-APIs
- gezielte `//`-Kommentare für nicht offensichtliche Entscheidungen
- Code-behind ausschließlich für UI-spezifische Aufgaben
- Nullable Reference Types
- Warnungen als Buildfehler
- automatisierte Tests und Windows-CI

## Geplante nächste Ausbaustufen

- Vergleich kompletter Ordner
- Excel- und CSV-Zellvergleich
- lokaler Vergleichsverlauf
- PDF-Berichtsexport
- mehrsprachige Oberfläche

## Autor

**Mahdi Mohebi**  
Apprentice Software Developer, Germany

## Lizenz

MIT
