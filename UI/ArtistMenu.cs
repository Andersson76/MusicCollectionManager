using System;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.UI
{
    public class ArtistMenu
    {
        private readonly MusicLibraryService _musicLibrary;
        private readonly LogService _logService;

        public ArtistMenu(MusicLibraryService musicLibrary, LogService logService)
        {
            _musicLibrary = musicLibrary ?? throw new ArgumentNullException(nameof(musicLibrary));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task ShowArtistMenu()
        {
            var inArtistMenu = true;
            
            while (inArtistMenu)
            {
                Console.Clear();
                DisplayHeader();

                var artists = _musicLibrary.GetAllArtists();
                
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Title(new TableTitle($"[bold]Artists ({artists.Count})[/]"));
                
                table.AddColumn(new TableColumn("[bold]ID[/]").Centered());
                table.AddColumn(new TableColumn("[bold]Name[/]"));
                table.AddColumn(new TableColumn("[bold]Genre[/]"));
                table.AddColumn(new TableColumn("[bold]Country[/]"));

                foreach (var artist in artists.Take(10))
                {
                    table.AddRow(
                        $"[green]{artist.Id}[/]",
                        $"[white]{artist.Name}[/]",
                        $"[cyan]{artist.Genre}[/]",
                        $"[grey]{artist.Country}[/]"
                    );
                }

                if (artists.Count > 10)
                {
                    table.AddRow("[grey]...[/]", $"[grey]and {artists.Count - 10} more[/]", "[grey]...[/]", "[grey]...[/]");
                }

                AnsiConsole.Render(table);
                AnsiConsole.WriteLine();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold yellow]Artist Management[/]")
                        .PageSize(10)
                        .HighlightStyle(Style.Parse("cyan bold on black"))
                        .AddChoices(new[]
                        {
                            "➕ Add New Artist",
                            "✏️ Edit Artist",
                            "🗑️ Delete Artist",
                            "👁️ View Artist Details",
                            "🔍 Search Artists",
                            "🔙 Back to Main Menu"
                        }));

                switch (choice)
                {
                    case "➕ Add New Artist":
                        await AddNewArtist();
                        break;
                    case "✏️ Edit Artist":
                        await EditArtist();
                        break;
                    case "🗑️ Delete Artist":
                        await DeleteArtist();
                        break;
                    case "👁️ View Artist Details":
                        await ViewArtistDetails();
                        break;
                    case "🔍 Search Artists":
                        await SearchArtists();
                        break;
                    case "🔙 Back to Main Menu":
                        inArtistMenu = false;
                        break;
                }
            }
        }

        private void DisplayHeader()
        {
            AnsiConsole.Render(
                new Rule("[bold yellow]🎤 Artist Management[/]")
                    .LeftAligned()
            );
        }

        private async Task AddNewArtist()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold green]➕ Add New Artist[/]")
                    .LeftAligned()
            );

            try
            {
                var name = AnsiConsole.Ask<string>("[yellow]Artist Name:[/]");
                
                if (string.IsNullOrWhiteSpace(name))
                {
                    AnsiConsole.MarkupLine("[red]❌ Artist name is required![/]");
                    WaitForUser();
                    return;
                }

                var country = AnsiConsole.Ask<string>("[yellow]Country:[/]");
                
                var genre = AnsiConsole.Prompt(
                    new SelectionPrompt<Genre>()
                        .Title("[yellow]Select Genre:[/]")
                        .PageSize(10)
                        .AddChoices(Enum.GetValues(typeof(Genre)).Cast<Genre>().Where(g => g != Genre.Unknown)));

                var artist = new Artist 
                { 
                    Name = name.Trim(), 
                    Country = country?.Trim() ?? string.Empty,
                    Genre = genre
                };

                await AnsiConsole.Status()
                    .StartAsync("Adding artist...", async ctx =>
                    {
                        ctx.Spinner = Spinner.Known.Star;
                        ctx.SpinnerStyle = Style.Parse("green");
                        
                        var result = await _musicLibrary.AddArtistAsync(artist);
                        
                        AnsiConsole.MarkupLine($"[green]✅ Artist '{result.Name}' added successfully with ID: {result.Id}[/]");
                        await _logService.LogCrudAsync("ArtistMenu", "Create", "Artist", result.Id, 
                            $"Added artist: {result.Name}");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("ArtistMenu", "AddArtist", "Artist", 
                    $"Failed to add artist: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task EditArtist()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold yellow]✏️ Edit Artist[/]")
                    .LeftAligned()
            );

            try
            {
                var artistId = AnsiConsole.Ask<int>("[yellow]Enter Artist ID to edit:[/]");

                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == artistId);
                if (artist == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Artist with ID {artistId} not found![/]");
                    WaitForUser();
                    return;
                }

                AnsiConsole.MarkupLine($"[bold cyan]Editing Artist: {artist.Name} (ID: {artist.Id})[/]");
                AnsiConsole.MarkupLine("[grey]Leave field blank to keep current value.[/]");
                AnsiConsole.WriteLine();

                var name = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Name:[/]")
                        .DefaultValue(artist.Name)
                        .AllowEmpty());

                var country = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Country:[/]")
                        .DefaultValue(artist.Country)
                        .AllowEmpty());

                var changeGenre = AnsiConsole.Confirm("[yellow]Change genre?[/]", false);
                Genre newGenre = artist.Genre;
                
                if (changeGenre)
                {
                    newGenre = AnsiConsole.Prompt(
                        new SelectionPrompt<Genre>()
                            .Title("[yellow]Select new Genre:[/]")
                            .PageSize(10)
                            .AddChoices(Enum.GetValues(typeof(Genre)).Cast<Genre>().Where(g => g != Genre.Unknown)));
                }

                if (!string.IsNullOrWhiteSpace(name)) artist.Name = name.Trim();
                if (!string.IsNullOrWhiteSpace(country)) artist.Country = country.Trim();
                artist.Genre = newGenre;

                var success = await _musicLibrary.UpdateArtistAsync(artist);
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✅ Artist '{artist.Name}' updated successfully![/]");
                    await _logService.LogCrudAsync("ArtistMenu", "Update", "Artist", artist.Id, 
                        $"Updated artist: {artist.Name}");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Failed to update artist![/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("ArtistMenu", "EditArtist", "Artist", 
                    $"Failed to edit artist: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task DeleteArtist()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold red]🗑️ Delete Artist[/]")
                    .LeftAligned()
            );

            try
            {
                var artistId = AnsiConsole.Ask<int>("[yellow]Enter Artist ID to delete:[/]");

                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == artistId);
                if (artist == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Artist with ID {artistId} not found![/]");
                    WaitForUser();
                    return;
                }

                AnsiConsole.MarkupLine($"[bold red]⚠️ WARNING: This will delete '{artist.Name}' and ALL associated albums and songs![/]");
                
                if (AnsiConsole.Confirm("[yellow]Are you sure you want to delete this artist?[/]", false))
                {
                    var success = await _musicLibrary.DeleteArtistAsync(artistId);
                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✅ Artist '{artist.Name}' deleted successfully![/]");
                        await _logService.LogCrudAsync("ArtistMenu", "Delete", "Artist", artistId, 
                            $"Deleted artist: {artist.Name}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Failed to delete artist![/]");
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
                await _logService.LogErrorAsync("ArtistMenu", "DeleteArtist", "Artist", 
                    $"Failed to delete artist: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task ViewArtistDetails()
        {
            DisplayHeader();

            try
            {
                var artistId = AnsiConsole.Ask<int>("[yellow]Enter Artist ID to view:[/]");

                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == artistId);
                if (artist == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Artist with ID {artistId} not found![/]");
                    WaitForUser();
                    return;
                }

                var panel = new Panel($"""
                    [bold]ID:[/] {artist.Id}
                    [bold]Name:[/] {artist.Name}
                    [bold]Genre:[/] {artist.Genre}
                    [bold]Country:[/] {artist.Country}
                    """)
                    .Header(new PanelHeader("[bold cyan]Artist Details[/]"))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Cyan);

                AnsiConsole.Render(panel);

                var albums = _musicLibrary.GetAlbumsByArtist(artistId);
                
                if (albums.Any())
                {
                    var albumsTable = new Table()
                        .Border(TableBorder.Simple)
                        .Title(new TableTitle($"[bold]Albums by {artist.Name} ({albums.Count})[/]"));
                    
                    albumsTable.AddColumn(new TableColumn("[bold]Title[/]"));
                    albumsTable.AddColumn(new TableColumn("[bold]Year[/]").Centered());
                    albumsTable.AddColumn(new TableColumn("[bold]Rating[/]").Centered());

                    foreach (var albumWithArtist in albums)
                    {
                        var ratingText = albumWithArtist.Album.Rating > 0 ? 
                            $"[yellow]{albumWithArtist.Album.Rating}/5[/]" : 
                            "[grey]No rating[/]";
                        
                        albumsTable.AddRow(
                            albumWithArtist.Album.Title,
                            albumWithArtist.Album.ReleaseYear.ToString(),
                            ratingText
                        );
                    }

                    AnsiConsole.Render(albumsTable);
                }
                else
                {
                    AnsiConsole.MarkupLine("[grey]No albums found for this artist.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("ArtistMenu", "ViewArtist", "Artist", 
                    $"Failed to view artist: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task SearchArtists()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold cyan]🔍 Search Artists[/]")
                    .LeftAligned()
            );

            try
            {
                var term = AnsiConsole.Ask<string>("[yellow]Enter search term (name or country):[/]");
                
                var results = _musicLibrary.SearchArtists(term ?? "");
                
                if (results.Any())
                {
                    var table = new Table()
                        .Border(TableBorder.Rounded)
                        .Title(new TableTitle($"[bold]Found {results.Count} Artists[/]"));
                    
                    table.AddColumn(new TableColumn("[bold]ID[/]").Centered());
                    table.AddColumn(new TableColumn("[bold]Name[/]"));
                    table.AddColumn(new TableColumn("[bold]Genre[/]"));
                    table.AddColumn(new TableColumn("[bold]Country[/]"));

                    foreach (var artist in results)
                    {
                        table.AddRow(
                            $"[green]{artist.Id}[/]",
                            $"[white]{artist.Name}[/]",
                            $"[cyan]{artist.Genre}[/]",
                            $"[grey]{artist.Country}[/]"
                        );
                    }

                    AnsiConsole.Render(table);
                }
                else
                {
                    AnsiConsole.MarkupLine("[grey]No artists found matching your search.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("ArtistMenu", "SearchArtists", "Artist", 
                    $"Failed to search artists: {ex.Message}", ex);
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