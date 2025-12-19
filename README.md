# 🎵 Music Collection Manager

Ett objektorienterat C#-konsolprogram för att hantera en musiksamling bestående av artister och album.  
Projektet är utvecklat som ett **grupparbete** inom ramen för kursen och fokuserar på **OOP, Git-arbetsflöde, samarbete och kodkvalitet**.

---

## 📌 Projektbeskrivning

Music Collection Manager låter användaren:

- Skapa, visa, uppdatera och ta bort **artister**
- Skapa, visa, uppdatera och ta bort **album**
- Koppla album till artister (association)
- Spara och läsa data från **JSON-filer**
- Visa information i tabeller via konsol-UI
- Arbeta strukturerat enligt objektorienterade principer

Projektet är uppbyggt enligt skolans rekommenderade kodstruktur med tydlig separation mellan **Models, Services och UI**.

---

## 🎯 Syfte och mål

Syftet med projektet är att:

- Tillämpa objektorienterad programmering i C#
- Träna på samarbete i GitHub med branches och pull requests
- Arbeta enligt en strukturerad utvecklingsprocess
- Dokumentera och presentera kod på ett professionellt sätt

---

## 🧠 Koppling till lärandemål

Projektet examinerar bland annat:

- Objektorienterad programmering (inkapsling, associationer)
- Klass- och modellstruktur
- Filhantering (JSON)
- Versionshantering med Git & GitHub
- Samarbete i grupp och code reviews
- Dokumentation och teknisk presentation

---

## 🧠 Designöversikt

### 📐 Övergripande arkitektur

Projektet är uppdelat i tre huvudsakliga lager:

- **Models** – Domänmodeller (Artist, Album)
- **Services** – Affärslogik och datalagring
- **UI** – Menyer och presentation i konsolen

---

### 📊 Pseudodiagram / Klassöversikt

```text
+------------------+
|     Artist       |
+------------------+
| - _id            |
| - _name          |
| - _country       |
| - _genre         |
+------------------+
| + Id             |
| + Name           |
| + Country        |
| + Genre          |
| + IsValid()      |
+------------------+

          ▲
          │ Association (ArtistId)
          │
+------------------+
|      Album       |
+------------------+
| - _id            |
| - _title         |
| - _artistId      |
| - _releaseYear   |
| - _genre         |
| - _rating        |
+------------------+
| + UpdateRating() |
+------------------+

+-----------------------------+
|   MusicLibraryService       |
+-----------------------------+
| + AddArtist()               |
| + AddAlbum()                |
| + GetArtists()              |
| + GetAlbums()               |
| + UpdateArtist()            |
| + UpdateAlbum()             |
| + DeleteArtist()            |
| + DeleteAlbum()             |
+-----------------------------+

+-----------------------------+
|     JsonFileService         |
+-----------------------------+
| + Load<T>()                 |
| + Save<T>()                 |
+-----------------------------+

+-----------------------------+
|           Menu              |
+-----------------------------+
| + ShowMainMenu()            |
| + HandleUserChoice()        |
+-----------------------------+
│
├── Data/                   // JSON-filer
│
└── README.md
