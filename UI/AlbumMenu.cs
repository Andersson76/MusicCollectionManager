using System;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.UI
{
    public class AlbumMenu
    {
        private readonly MusicLibraryService _musicLibrary;
        private readonly LogService _logService;

        public AlbumMenu(MusicLibraryService musicLibrary, LogService logService)
        {
            _musicLibrary = musicLibrary ?? throw new ArgumentNullException(nameof(musicLibrary));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task ShowAlbumMenu()
        {
            var inAlbumMenu = true;
            
            while (inAlbumMenu)
            {
                Console.Clear();
                DisplayHeader();

                var albums = _musicLibrary.GetAllAlbums();
                
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[bold yellow]Album Management ({albums.Count} albums)[/]")
                        .PageSize(10)
                        .HighlightStyle(Style.Parse("cyan bold on black"))
                        .AddChoices(new[]
                        {
                            "➕ Add New Album",
                            "✏️ Edit Album",
                            "🗑️ Delete Album",
                            "👁️ View Albums",
                            "🔍 Search Albums",
                            "🔙 Back to Main Menu"
                        }));

                switch (choice)
                {
                    case "➕ Add New Album":
                        await AddNewAlbum();
                        break;
                    case "✏️ Edit Album":
                        await EditAlbum();
                        break;
                    case "🗑️ Delete Album":
                        await DeleteAlbum();
                        break;
                    case "👁️ View Albums":
                        await ViewAlbums();
                        break;
                    case "🔍 Search Albums":
                        await SearchAlbums();
                        break;
                    case "🔙 Back to Main Menu":
                        inAlbumMenu = false;
                        break;
                }
            }
        }

        private void DisplayHeader()
        {
            AnsiConsole.Render(
                new Rule("[bold yellow]💿 Album Management[/]")
                    .LeftAligned()
            );
        }

        private async Task AddNewAlbum()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold green]➕ Add New Album[/]")
                    .LeftAligned()
            );

            try
            {
                // Show available artists
                var artists = _musicLibrary.GetAllArtists();
                if (!artists.Any())
                {
                    AnsiConsole.MarkupLine("[red]❌ No artists available. Please add an artist first![/]");
                    WaitForUser();
                    return;
                }

                var artistId = AnsiConsole.Prompt(
                    new SelectionPrompt<int>()
                        .Title("[yellow]Select Artist:[/]")
                        .PageSize(10)
                        .AddChoices(artists.Select(a => a.Id)));

                var title = AnsiConsole.Ask<string>("[yellow]Album Title:[/]");
                
                if (string.IsNullOrWhiteSpace(title))
                {
                    AnsiConsole.MarkupLine("[red]❌ Album title is required![/]");
                    WaitForUser();
                    return;
                }

                var releaseYear = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Release Year:[/]")
                        .DefaultValue(DateTime.Now.Year)
                        .ValidationErrorMessage("[red]Invalid year![/]")
                        .Validate(year => year >= 1900 && year <= DateTime.Now.Year + 1));

                var genre = AnsiConsole.Prompt(
                    new SelectionPrompt<Genre>()
                        .Title("[yellow]Select Genre:[/]")
                        .PageSize(10)
                        .AddChoices(Enum.GetValues(typeof(Genre)).Cast<Genre>().Where(g => g != Genre.Unknown)));

                var rating = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Rating (1-5, optional, press Enter to skip):[/]")
                        .AllowEmpty()
                        .Validate(input => 
                        {
                            if (string.IsNullOrEmpty(input)) return true;
                            if (int.TryParse(input, out int r) && r >= 1 && r <= 5) return true;
                            return false;
                        }));

                var album = new Album
                {
                    Title = title.Trim(),
                    ArtistId = artistId,
                    ReleaseYear = releaseYear,
                    Genre = genre,
                };

                if (!string.IsNullOrWhiteSpace(rating) && int.TryParse(rating, out int ratingValue))
                {
                    album.UpdateRating(ratingValue);
                }

                await AnsiConsole.Status()
                    .StartAsync("Adding album...", async ctx =>
                    {
                        ctx.Spinner = Spinner.Known.Star;
                        ctx.SpinnerStyle = Style.Parse("green");
                        
                        var result = await _musicLibrary.AddAlbumAsync(album);
                        
                        AnsiConsole.MarkupLine($"[green]✅ Album '{result.Title}' added successfully with ID: {result.Id}[/]");
                        await _logService.LogCrudAsync("AlbumMenu", "Create", "Album", result.Id, 
                            $"Added album: {result.Title}");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("AlbumMenu", "AddAlbum", "Album", 
                    $"Failed to add album: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task EditAlbum()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold yellow]✏️ Edit Album[/]")
                    .LeftAligned()
            );

            try
            {
                var albumId = AnsiConsole.Ask<int>("[yellow]Enter Album ID to edit:[/]");

                var album = _musicLibrary.GetAllAlbums().FirstOrDefault(a => a.Id == albumId);
                if (album == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Album with ID {albumId} not found![/]");
                    WaitForUser();
                    return;
                }

                var currentArtist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
                AnsiConsole.MarkupLine($"[bold cyan]Editing Album: {album.Title} (ID: {album.Id})[/]");
                AnsiConsole.MarkupLine($"[grey]Current Artist: {currentArtist?.Name ?? "Unknown"}[/]");
                AnsiConsole.MarkupLine("[grey]Leave field blank to keep current value.[/]");
                AnsiConsole.WriteLine();

                var title = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Title:[/]")
                        .DefaultValue(album.Title)
                        .AllowEmpty());

                var releaseYear = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Release Year:[/]")
                        .DefaultValue(album.ReleaseYear.ToString())
                        .AllowEmpty()
                        .Validate(input => 
                        {
                            if (string.IsNullOrEmpty(input)) return true;
                            if (int.TryParse(input, out int year) && year >= 1900 && year <= DateTime.Now.Year + 1) return true;
                            return false;
                        }));

                var changeArtist = AnsiConsole.Confirm("[yellow]Change artist?[/]", false);
                int newArtistId = album.ArtistId;
                
                if (changeArtist)
                {
                    var artists = _musicLibrary.GetAllArtists();
                    newArtistId = AnsiConsole.Prompt(
                        new SelectionPrompt<int>()
                            .Title("[yellow]Select new Artist:[/]")
                            .PageSize(10)
                            .AddChoices(artists.Select(a => a.Id)));
                }

                var changeGenre = AnsiConsole.Confirm("[yellow]Change genre?[/]", false);
                Genre newGenre = album.Genre;
                
                if (changeGenre)
                {
                    newGenre = AnsiConsole.Prompt(
                        new SelectionPrompt<Genre>()
                            .Title("[yellow]Select new Genre:[/]")
                            .PageSize(10)
                            .AddChoices(Enum.GetValues(typeof(Genre)).Cast<Genre>().Where(g => g != Genre.Unknown)));
                }

                var rating = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Rating (1-5, empty to keep current):[/]")
                        .AllowEmpty()
                        .Validate(input => 
                        {
                            if (string.IsNullOrEmpty(input)) return true;
                            if (int.TryParse(input, out int r) && r >= 1 && r <= 5) return true;
                            return false;
                        }));

                if (!string.IsNullOrWhiteSpace(title)) album.Title = title.Trim();
                if (!string.IsNullOrWhiteSpace(releaseYear) && int.TryParse(releaseYear, out int year))
                    album.ReleaseYear = year;
                album.ArtistId = newArtistId;
                album.Genre = newGenre;

                if (!string.IsNullOrWhiteSpace(rating) && int.TryParse(rating, out int ratingValue))
                {
                    album.UpdateRating(ratingValue);
                }

                var success = await _musicLibrary.UpdateAlbumAsync(album);
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✅ Album '{album.Title}' updated successfully![/]");
                    await _logService.LogCrudAsync("AlbumMenu", "Update", "Album", album.Id, 
                        $"Updated album: {album.Title}");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Failed to update album![/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("AlbumMenu", "EditAlbum", "Album", 
                    $"Failed to edit album: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task DeleteAlbum()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold red]🗑️ Delete Album[/]")
                    .LeftAligned()
            );

            try
            {
                var albumId = AnsiConsole.Ask<int>("[yellow]Enter Album ID to delete:[/]");

                var album = _musicLibrary.GetAllAlbums().FirstOrDefault(a => a.Id == albumId);
                if (album == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Album with ID {albumId} not found![/]");
                    WaitForUser();
                    return;
                }

                AnsiConsole.MarkupLine($"[bold red]⚠️ WARNING: This will delete '{album.Title}' and ALL associated songs![/]");
                
                if (AnsiConsole.Confirm("[yellow]Are you sure you want to delete this album?[/]", false))
                {
                    var success = await _musicLibrary.DeleteAlbumAsync(albumId);
                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✅ Album '{album.Title}' deleted successfully![/]");
                        await _logService.LogCrudAsync("AlbumMenu", "Delete", "Album", albumId, 
                            $"Deleted album: {album.Title}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Failed to delete album![/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Deletion cancelled.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("AlbumMenu", "DeleteAlbum", "Album", 
                    $"Failed to delete album: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task ViewAlbums()
        {
            DisplayHeader();

            try
            {
                var albums = _musicLibrary.GetAllAlbums();
                var albumsWithArtists = _musicLibrary.GetAlbumsWithArtists();

                if (!albumsWithArtists.Any())
                {
                    AnsiConsole.MarkupLine("[grey]No albums found.[/]");
                    WaitForUser();
                    return;
                }

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Title(new TableTitle($"[bold]All Albums ({albums.Count})[/]"));
                
                table.AddColumn(new TableColumn("[bold]ID[/]").Centered());
                table.AddColumn(new TableColumn("[bold]Title[/]"));
                table.AddColumn(new TableColumn("[bold]Artist[/]"));
                table.AddColumn(new TableColumn("[bold]Year[/]").Centered());
                table.AddColumn(new TableColumn("[bold]Genre[/]"));
                table.AddColumn(new TableColumn("[bold]Rating[/]").Centered());

                foreach (var albumWithArtist in albumsWithArtists.Take(15))
                {
                    var ratingText = albumWithArtist.Album.Rating > 0 ? 
                        $"[yellow]{albumWithArtist.Album.Rating}/5[/]" : 
                        "[grey]-[/]";
                    
                    table.AddRow(
                        $"[green]{albumWithArtist.Album.Id}[/]",
                        $"[white]{albumWithArtist.Album.Title}[/]",
                        $"[cyan]{albumWithArtist.Artist.Name}[/]",
                        $"[grey]{albumWithArtist.Album.ReleaseYear}[/]",
                        $"[blue]{albumWithArtist.Album.Genre}[/]",
                        ratingText
                    );
                }

                if (albumsWithArtists.Count > 15)
                {
                    table.AddRow(
                        "[grey]...[/]",
                        $"[grey]and {albumsWithArtists.Count - 15} more[/]",
                        "[grey]...[/]",
                        "[grey]...[/]",
                        "[grey]...[/]",
                        "[grey]...[/]"
                    );
                }

                AnsiConsole.Render(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("AlbumMenu", "ViewAlbums", "Album", 
                    $"Failed to view albums: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task SearchAlbums()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold cyan]🔍 Search Albums[/]")
                    .LeftAligned()
            );

            try
            {
                var term = AnsiConsole.Ask<string>("[yellow]Enter search term (title):[/]");
                
                var results = _musicLibrary.SearchAlbums(term ?? "");
                
                if (results.Any())
                {
                    var table = new Table()
                        .Border(TableBorder.Simple)
                        .Title(new TableTitle($"[bold]Found {results.Count} Albums[/]"));
                    
                    table.AddColumn(new TableColumn("[bold]Title[/]"));
                    table.AddColumn(new TableColumn("[bold]Year[/]").Centered());
                    table.AddColumn(new TableColumn("[bold]Genre[/]"));
                    table.AddColumn(new TableColumn("[bold]Rating[/]").Centered());

                    foreach (var album in results)
                    {
                        var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
                        var ratingText = album.Rating > 0 ? $"[yellow]{album.Rating}/5[/]" : "[grey]-[/]";
                        
                        table.AddRow(
                            $"[white]{album.Title}[/]",
                            $"[grey]{album.ReleaseYear}[/]",
                            $"[cyan]{album.Genre}[/]",
                            ratingText
                        );
                    }

                    AnsiConsole.Render(table);
                }
                else
                {
                    AnsiConsole.MarkupLine("[grey]No albums found matching your search.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("AlbumMenu", "SearchAlbums", "Album", 
                    $"Failed to search albums: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private void WaitForUser(string message = "Press any key to continue...")
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[grey]{message}[/]");
            Console.ReadKey();
        }
    }
}