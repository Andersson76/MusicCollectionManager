using Spectre.Console;
using MusicCollectionManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicCollectionManager.Services
{
    /// <summary>
    /// Service för att rendera tabeller med färgkodning och formatering
    /// </summary>
    public class TableRenderer
    {
        private readonly Style _headerStyle;
        private readonly Style _highlightStyle;

        public TableRenderer()
        {
            _headerStyle = new Style(Color.Cyan1, null, Decoration.Bold);
            _highlightStyle = new Style(Color.Gold3_1, null, Decoration.None);
        }

        /// <summary>
        /// Renderar en tabell med artister
        /// </summary>
        public void RenderArtistTable(IEnumerable<Artist> artists)
        {
            AnsiConsole.Clear();
            
            var table = new Table
            {
                Title = new TableTitle("🎵 Artister", new Style(Color.Yellow, null, Decoration.Bold)),
                Border = TableBorder.Rounded,
                Expand = true
            };

            // Lägg till kolumner
            table.AddColumn("[cyan]ID[/]");
            table.AddColumn("[cyan]Namn[/]");
            table.AddColumn("[cyan]Land[/]");
            table.AddColumn("[cyan]Genre[/]");

            // Hantera tom lista
            if (artists == null || !artists.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inga artister att visa.[/]");
                return;
            }

            // Lägg till rader
            foreach (var artist in artists)
            {
                // Färgkodning baserat på om artisten är giltig
                var isValid = artist.IsValid();
                var nameColor = isValid ? "white" : "red";

                table.AddRow(
                    $"[grey]{artist.Id}[/]",
                    $"[{nameColor}]{artist.Name.EscapeMarkup()}[/]",
                    $"[grey]{artist.Country?.EscapeMarkup() ?? "Okänt"}[/]",
                    $"[silver]{artist.Genre}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[grey]Visar {artists.Count()} artister[/]");
        }

        /// <summary>
        /// Renderar en tabell med album
        /// </summary>
        public void RenderAlbumTable(IEnumerable<Album> albums, IEnumerable<Artist> artists)
        {
            AnsiConsole.Clear();
            
            var table = new Table
            {
                Title = new TableTitle("💿 Album", new Style(Color.Yellow, null, Decoration.Bold)),
                Border = TableBorder.Rounded,
                Expand = true
            };

            // Lägg till kolumner (inklusive artistnamn istället för bara ID)
            table.AddColumn("[cyan]ID[/]");
            table.AddColumn("[cyan]Titel[/]");
            table.AddColumn("[cyan]Artist[/]");  // Visar artistnamn
            table.AddColumn("[cyan]Utgivningsår[/]");
            table.AddColumn("[cyan]Genre[/]");
            table.AddColumn("[cyan]Betyg[/]");

            // Hantera tom lista
            if (albums == null || !albums.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inga album att visa.[/]");
                return;
            }

            // Skapa lookup för att hitta artistnamn från ArtistId
            var artistLookup = artists?.ToDictionary(a => a.Id, a => a.Name) 
                             ?? new Dictionary<int, string>();

            // Lägg till rader
            foreach (var album in albums)
            {
                // Hämta artistnamn baserat på ArtistId
                var artistName = artistLookup.ContainsKey(album.ArtistId) 
                    ? artistLookup[album.ArtistId] 
                    : $"Artist ID: {album.ArtistId}";

                // Färgkodning baserat på betyg
                var ratingDisplay = GetRatingDisplay(album.Rating);
                var ratingColor = GetRatingColor(album.Rating);
                
                // Formatera år med färgkodning baserat på ålder
                var yearColor = GetYearColor(album.ReleaseYear);

                // Färgkodning baserat på om albumet är giltigt
                var isValid = album.IsValid();
                var titleColor = isValid ? "white" : "red";

                table.AddRow(
                    $"[grey]{album.Id}[/]",
                    $"[{titleColor}]{album.Title.EscapeMarkup()}[/]",
                    $"[gold3_1]{artistName.EscapeMarkup()}[/]",
                    $"[{yearColor}]{album.ReleaseYear}[/]",
                    $"[silver]{album.Genre}[/]",
                    $"[{ratingColor}]{ratingDisplay}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[grey]Visar {albums.Count()} album[/]");
        }

        /// <summary>
        /// Renderar en tabell med låtar (songs)
        /// </summary>
        public void RenderSongTable(IEnumerable<Song> songs, IEnumerable<Album> albums, IEnumerable<Artist> artists)
        {
            AnsiConsole.Clear();
            
            var table = new Table
            {
                Title = new TableTitle("🎶 Låtar", new Style(Color.Yellow, null, Decoration.Bold)),
                Border = TableBorder.Rounded,
                Expand = true
            };

            // Lägg till kolumner
            table.AddColumn("[cyan]ID[/]");
            table.AddColumn("[cyan]Titel[/]");
            table.AddColumn("[cyan]Album[/]");
            table.AddColumn("[cyan]Artist[/]");
            table.AddColumn("[cyan]Längd[/]");

            // Hantera tom lista
            if (songs == null || !songs.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inga låtar att visa.[/]");
                return;
            }

            // Skapa lookups för att hitta album och artistinformation
            var albumLookup = albums?.ToDictionary(a => a.Id, a => a) 
                            ?? new Dictionary<int, Album>();
            var artistLookup = artists?.ToDictionary(a => a.Id, a => a.Name) 
                             ?? new Dictionary<int, string>();

            // Lägg till rader
            foreach (var song in songs)
            {
                // Formatera duration (mm:ss)
                var formattedDuration = FormatDuration(song.DurationSeconds);
                
                // Hämta album och artistinformation
                Album? album = null;
                string artistName = "Okänd";
                
                if (albumLookup.ContainsKey(song.AlbumId))
                {
                    album = albumLookup[song.AlbumId];
                    if (artistLookup.ContainsKey(album.ArtistId))
                    {
                        artistName = artistLookup[album.ArtistId];
                    }
                }

                var albumTitle = album?.Title ?? $"Album ID: {song.AlbumId}";

                table.AddRow(
                    $"[grey]{song.Id}[/]",
                    $"[white]{song.Title.EscapeMarkup()}[/]",
                    $"[grey]{albumTitle.EscapeMarkup()}[/]",
                    $"[gold3_1]{artistName.EscapeMarkup()}[/]",
                    $"[cyan]{formattedDuration}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[grey]Visar {songs.Count()} låtar[/]");
        }

        /// <summary>
        /// Formaterar duration till mm:ss format
        /// </summary>
        private string FormatDuration(int seconds)
        {
            var minutes = seconds / 60;
            var remainingSeconds = seconds % 60;
            return $"{minutes}:{remainingSeconds:00}";
        }

        /// <summary>
        /// Returnerar en färg baserat på betyg (1-5)
        /// </summary>
        private string GetRatingColor(int rating)
        {
            if (rating == 0) return "grey"; // Ej satt

            return rating switch
            {
                5 => "green",      // Utmärkt
                4 => "lime",       // Bra
                3 => "yellow",     // Okej
                2 => "orange1",    // Dålig
                1 => "red",        // Mycket dålig
                _ => "grey"        // Okänt
            };
        }

        /// <summary>
        /// Returnerar visningsvärde för betyg
        /// </summary>
        private string GetRatingDisplay(int rating)
        {
            if (rating == 0) return "Ej satt";
            if (rating < 1 || rating > 5) return "N/A";
            
            var stars = new string('★', rating);
            var emptyStars = new string('☆', 5 - rating);
            return $"{stars}{emptyStars} ({rating}/5)";
        }

        /// <summary>
        /// Returnerar en färg baserat på albumets ålder
        /// </summary>
        private string GetYearColor(int year)
        {
            var currentYear = DateTime.Now.Year;
            var age = currentYear - year;

            if (year == 0) return "grey"; // Ej satt

            return age switch
            {
                < 1 => "green",    // Nytt
                < 3 => "lime",     // Ganska nytt
                < 10 => "yellow",  // Mellangammalt
                < 20 => "orange1", // Gammalt
                _ => "red"         // Väldigt gammalt
            };
        }
    }
}
