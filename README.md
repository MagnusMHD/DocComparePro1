# DocComparePro

DocComparePro ist eine WPF-Desktopanwendung zum Vergleich von TXT-, PDF- und Bilddateien. Die Anwendung verwendet eine saubere MVVM-Struktur, trennt Benutzeroberfläche, Dateiverarbeitung und Vergleichslogik und läuft auf .NET 8.

## Funktionen

- TXT-Dateien direkt einlesen
- PDF-Texte mit PdfPig extrahieren
- PNG-, JPG- und JPEG-Dateien mit Tesseract OCR verarbeiten
- Wortweiser oder satzweiser Vergleich
- Groß-/Kleinschreibung optional beachten
- Zahlen und Satzzeichen optional vergleichen
- Leerzeichen normalisieren
- Ähnlichkeit in Prozent berechnen
- Unterschiede mit Position, Typ und Inhalt anzeigen
- Verarbeitungszeit und Anzahl geprüfter Einheiten anzeigen
- Asynchrone Dateiverarbeitung ohne blockierte Oberfläche

## Architektur

```text
DocComparePro/
├── App.xaml
├── App.xaml.cs
├── Core/
│   ├── Models.cs
│   ├── DocumentReader.cs
│   └── ComparisonEngine.cs
├── ViewModels/
│   └── MainViewModel.cs
└── Views/
    ├── MainWindow.xaml
    └── MainWindow.xaml.cs
```

### Verantwortlichkeiten

- `Views`: Darstellung und Bindings
- `ViewModels`: UI-Zustand, Commands und Ablaufsteuerung
- `Core/DocumentReader`: TXT-, PDF- und OCR-Verarbeitung
- `Core/ComparisonEngine`: Tokenisierung, LCS-Diff und Statistik
- `Core/Models`: unveränderliche Domain-Modelle

## Technologien

- C#
- .NET 8
- WPF
- MVVM
- CommunityToolkit.Mvvm
- PdfPig
- Tesseract OCR

## Projekt starten

Voraussetzungen:

- Windows 10 oder Windows 11
- Visual Studio 2022 mit Workload **.NET-Desktopentwicklung**
- .NET 8 SDK

```bash
git clone https://github.com/MagnusMHD/DocComparePro1.git
cd DocComparePro1/DocComparePro
dotnet restore
dotnet build
dotnet run
```

## OCR einrichten

Für Bildvergleiche wird im Ausgabeordner ein Verzeichnis `tessdata` benötigt. Darin müssen mindestens diese Sprachdateien liegen:

```text
tessdata/
├── deu.traineddata
└── eng.traineddata
```

Ohne diese Dateien funktionieren TXT- und PDF-Vergleiche weiterhin; Bild-OCR zeigt eine verständliche Fehlermeldung.

## Vergleichsverfahren

Die Anwendung zerlegt Dokumente abhängig vom ausgewählten Modus in Wörter oder Sätze. Anschließend wird über eine Longest-Common-Subsequence-Matrix ein deterministischer Diff erzeugt. Dadurch werden gleiche, entfernte und hinzugefügte Einheiten sauber unterschieden.

## Clean-Code-Grundsätze

- kleine Klassen mit klarer Verantwortung
- Abhängigkeiten über Interfaces
- keine Geschäftslogik im Code-behind
- unveränderliche Records für Ergebnisse
- asynchrone Dateioperationen
- Kommentare nur für nicht offensichtliche technische Entscheidungen
- verständliche Namen statt unnötiger Abkürzungen

## Aktueller Stand

Der Kernumfang ist implementiert: Dateiauswahl, TXT/PDF/OCR-Einlesen, Wort- und Satzvergleich, Optionen, Statistiken und Ergebnisanzeige. Geplante Erweiterungen sind DOCX-Unterstützung, Export, synchrones Scrollen und semantischer KI-Vergleich.

## Autor

**Mahdi Mohebi**  
Apprentice Software Developer, Germany

## Lizenz

MIT
