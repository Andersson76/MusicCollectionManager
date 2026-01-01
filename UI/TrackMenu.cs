using System;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.UI
{
    public class TrackMenu
    {
        private readonly MusicLibraryService _musicLibrary;
        private readonly LogService _logService;

        public TrackMenu(MusicLibraryService musicLibrary, LogService logService)
        {
            _musicLibrary = musicLibrary ?? throw new ArgumentNullException(nameof(musicLibrary));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task ShowTrackMenu()
        {
            var inTrackMenu = true;
            
            while (inTrackMenu)
            {
                Console.Clear();
                DisplayHeader();

                var songs = _musicLibrary.GetAllSongs();
                
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[bold yellow]Track Management ({songs.Count} tracks)[/]")
                        .PageSize(10)
                        .HighlightStyle(Style.Parse("cyan bold on black"))
                        .AddChoices(new[]
                        {
                            "➕ Add New Track",
                            "✏️ Edit Track",
                            "🗑️ Delete Track",
                            "👁️ View Tracks",
                            "🔍 Search Tracks",
                            "🔙 Back to Main Menu"
                        }));

                switch (choice)
                {
                    case "➕ Add New Track":
                        await AddNewTrack();
                        break;
                    case "✏️ Edit Track":
                        await EditTrack();
                        break;
                    case "🗑️ Delete Track":
                        await DeleteTrack();
                        break;
                    case "👁️ View Tracks":
                        await ViewTracks();
                        break;
                    case "🔍 Search Tracks":
                        await SearchTracks();
                        break;
                    case "🔙 Back to Main Menu":
                        inTrackMenu = false;
                        break;
                }
            }
        }

        private void DisplayHeader()
        {
            AnsiConsole.Render(
                new Rule("[bold yellow]🎵 Track Management[/]")
                    .LeftAligned()
            );
        }

        private async Task AddNewTrack()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold green]➕ Add New Track[/]")
                    .LeftAligned()
            );

            try
            {
                // Show available albums
                var albums = _musicLibrary.GetAllAlbums();
                if (!albums.Any())
                {
                    AnsiConsole.MarkupLine("[red]❌ No albums available. Please add an album first![/]");
                    WaitForUser();
                    return;
                }

                var albumId = AnsiConsole.Prompt(
                    new SelectionPrompt<int>()
                        .Title("[yellow]Select Album:[/]")
                        .PageSize(10)
                        .AddChoices(albums.Select(a => a.Id)));

                var title = AnsiConsole.Ask<string>("[yellow]Track Title:[/]");
                
                if (string.IsNullOrWhiteSpace(title))
                {
                    AnsiConsole.MarkupLine("[red]❌ Track title is required![/]");
                    WaitForUser();
                    return;
                }

                var duration = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Duration in seconds:[/]")
                        .ValidationErrorMessage("[red]Invalid duration![/]")
                        .Validate(d => d > 0));

                var track = new Song
                {
                    Title = title.Trim(),
                    AlbumId = albumId,
                    DurationSeconds = duration
                };

                await AnsiConsole.Status()
                    .StartAsync("Adding track...", async ctx =>
                    {
                        ctx.Spinner = Spinner.Known.Star;
                        ctx.SpinnerStyle = Style.Parse("green");
                        
                        var result = await _musicLibrary.AddSongAsync(track);
                        var minutes = result.DurationSeconds / 60;
                        var seconds = result.DurationSeconds % 60;
                        
                        AnsiConsole.MarkupLine($"[green]✅ Track '{result.Title}' added successfully with ID: {result.Id}[/]");
                        AnsiConsole.MarkupLine($"[grey]   Duration: {minutes}:{seconds:00}[/]");
                        
                        await _logService.LogCrudAsync("TrackMenu", "Create", "Track", result.Id, 
                            $"Added track: {result.Title}");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("TrackMenu", "AddTrack", "Track", 
                    $"Failed to add track: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task EditTrack()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold yellow]✏️ Edit Track[/]")
                    .LeftAligned()
            );

            try
            {
                var trackId = AnsiConsole.Ask<int>("[yellow]Enter Track ID to edit:[/]");

                var track = _musicLibrary.GetAllSongs().FirstOrDefault(t => t.Id == trackId);
                if (track == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Track with ID {trackId} not found![/]");
                    WaitForUser();
                    return;
                }

                var currentAlbum = _musicLibrary.GetAllAlbums().FirstOrDefault(a => a.Id == track.AlbumId);
                AnsiConsole.MarkupLine($"[bold cyan]Editing Track: {track.Title} (ID: {track.Id})[/]");
                AnsiConsole.MarkupLine($"[grey]Current Album: {currentAlbum?.Title ?? "Unknown"}[/]");
                AnsiConsole.MarkupLine("[grey]Leave field blank to keep current value.[/]");
                AnsiConsole.WriteLine();

                var title = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Title:[/]")
                        .DefaultValue(track.Title)
                        .AllowEmpty());

                var duration = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Duration in seconds:[/]")
                        .DefaultValue(track.DurationSeconds.ToString())
                        .AllowEmpty()
                        .Validate(input => 
                        {
                            if (string.IsNullOrEmpty(input)) return true;
                            if (int.TryParse(input, out int d) && d > 0) return true;
                            return false;
                        }));

                var changeAlbum = AnsiConsole.Confirm("[yellow]Change album?[/]", false);
                int newAlbumId = track.AlbumId;
                
                if (changeAlbum)
                {
                    var albums = _musicLibrary.GetAllAlbums();
                    newAlbumId = AnsiConsole.Prompt(
                        new SelectionPrompt<int>()
                            .Title("[yellow]Select new Album:[/]")
                            .PageSize(10)
                            .AddChoices(albums.Select(a => a.Id)));
                }

                if (!string.IsNullOrWhiteSpace(title)) track.Title = title.Trim();
                if (!string.IsNullOrWhiteSpace(duration) && int.TryParse(duration, out int durationValue))
                    track.DurationSeconds = durationValue;
                track.AlbumId = newAlbumId;

                var success = await _musicLibrary.UpdateSongAsync(track);
                if (success)
                {
                    var minutes = track.DurationSeconds / 60;
                    var seconds = track.DurationSeconds % 60;
                    
                    AnsiConsole.MarkupLine($"[green]✅ Track '{track.Title}' updated successfully![/]");
                    AnsiConsole.MarkupLine($"[grey]   Duration: {minutes}:{seconds:00}[/]");
                    
                    await _logService.LogCrudAsync("TrackMenu", "Update", "Track", track.Id, 
                        $"Updated track: {track.Title}");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Failed to update track![/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("TrackMenu", "EditTrack", "Track", 
                    $"Failed to edit track: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task DeleteTrack()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold red]🗑️ Delete Track[/]")
                    .LeftAligned()
            );

            try
            {
                var trackId = AnsiConsole.Ask<int>("[yellow]Enter Track ID to delete:[/]");

                var track = _musicLibrary.GetAllSongs().FirstOrDefault(t => t.Id == trackId);
                if (track == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Track with ID {trackId} not found![/]");
                    WaitForUser();
                    return;
                }

                AnsiConsole.MarkupLine($"[bold red]⚠️ WARNING: This will delete '{track.Title}'![/]");
                
                if (AnsiConsole.Confirm("[yellow]Are you sure you want to delete this track?[/]", false))
                {
                    var success = await _musicLibrary.DeleteSongAsync(trackId);
                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✅ Track '{track.Title}' deleted successfully![/]");
                        await _logService.LogCrudAsync("TrackMenu", "Delete", "Track", trackId, 
                            $"Deleted track: {track.Title}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Failed to delete track![/]");
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
                await _logService.LogErrorAsync("TrackMenu", "DeleteTrack", "Track", 
                    $"Failed to delete track: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task ViewTracks()
        {
            DisplayHeader();

            try
            {
                var songs = _musicLibrary.GetAllSongs();
                
                if (!songs.Any())
                {
                    AnsiConsole.MarkupLine("[grey]No tracks found.[/]");
                    WaitForUser();
                    return;
                }

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title(new TableTitle($"[bold]All Tracks ({songs.Count})[/]"));
                
                table.AddColumn(new TableColumn("[bold]ID[/]").Centered());
                table.AddColumn(new TableColumn("[bold]Title[/]"));
                table.AddColumn(new TableColumn("[bold]Album[/]"));
                table.AddColumn(new TableColumn("[bold]Artist[/]"));
                table.AddColumn(new TableColumn("[bold]Duration[/]").Centered());

                foreach (var track in songs.Take(15))
                {
                    var album = _musicLibrary.GetAllAlbums().FirstOrDefault(a => a.Id == track.AlbumId);
                    var artist = album != null ? 
                        _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId) : 
                        null;
                    
                    var minutes = track.DurationSeconds / 60;
                    var seconds = track.DurationSeconds % 60;
                    
                    table.AddRow(
                        $"[green]{track.Id}[/]",
                        $"[white]{track.Title}[/]",
                        $"[cyan]{album?.Title ?? "Unknown"}[/]",
                        $"[yellow]{artist?.Name ?? "Unknown"}[/]",
                        $"[grey]{minutes}:{seconds:00}[/]"
                    );
                }

                if (songs.Count > 15)
                {
                    table.AddRow(
                        "[grey]...[/]",
                        $"[grey]and {songs.Count - 15} more[/]",
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
                await _logService.LogErrorAsync("TrackMenu", "ViewTracks", "Track", 
                    $"Failed to view tracks: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        private async Task SearchTracks()
        {
            DisplayHeader();
            
            AnsiConsole.Render(
                new Rule("[bold cyan]🔍 Search Tracks[/]")
                    .LeftAligned()
            );

            try
            {
                var term = AnsiConsole.Ask<string>("[yellow]Enter search term (track title):[/]");
                
                var tracks = _musicLibrary.GetAllSongs()
                    .Where(t => t.Title.Contains(term ?? "", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (tracks.Any())
                {
                    var table = new Table()
                        .Border(TableBorder.Simple)
                        .Title(new TableTitle($"[bold]Found {tracks.Count} Tracks[/]"));
                    
                    table.AddColumn(new TableColumn("[bold]Title[/]"));
                    table.AddColumn(new TableColumn("[bold]Album[/]"));
                    table.AddColumn(new TableColumn("[bold]Duration[/]").Centered());

                    foreach (var track in tracks)
                    {
                        var album = _musicLibrary.GetAllAlbums().FirstOrDefault(a => a.Id == track.AlbumId);
                        var minutes = track.DurationSeconds / 60;
                        var seconds = track.DurationSeconds % 60;
                        
                        table.AddRow(
                            $"[white]{track.Title}[/]",
                            $"[cyan]{album?.Title ?? "Unknown"}[/]",
                            $"[grey]{minutes}:{seconds:00}[/]"
                        );
                    }

                    AnsiConsole.Render(table);
                }
                else
                {
                    AnsiConsole.MarkupLine("[grey]No tracks found matching your search.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("TrackMenu", "SearchTracks", "Track", 
                    $"Failed to search tracks: {ex.Message}", ex);
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