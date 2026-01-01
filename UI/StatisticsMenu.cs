using System;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.UI
{
    public class StatisticsMenu
    {
        private readonly MusicLibraryService _musicLibrary;
        private readonly LogService _logService;

        public StatisticsMenu(MusicLibraryService musicLibrary, LogService logService)
        {
            _musicLibrary = musicLibrary ?? throw new ArgumentNullException(nameof(musicLibrary));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task ShowStatistics()
        {
            var inStatisticsMenu = true;
            
            while (inStatisticsMenu)
            {
                Console.Clear();
                DisplayHeader();

                // Get statistics
                var stats = _musicLibrary.GetStatistics();
                var recommendation = _musicLibrary.GetAlbumRecommendation();

                // Display overall statistics
                DisplayOverallStats(stats);

                // Display albums per genre chart
                DisplayGenreStats(stats);

                // Display top rated albums
                DisplayTopRatedAlbums(stats);

                // Display playtime
                DisplayPlayTime(stats);

                // Display recommendation
                DisplayRecommendation(recommendation);

                AnsiConsole.WriteLine();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Options[/]")
                        .PageSize(10)
                        .AddChoices(new[]
                        {
                            "Refresh Statistics",
                            "View Detailed Genre Breakdown",
                            "View All Top Albums",
                            "Export Statistics",
                            "Back to Main Menu"
                        }));

                try
                {
                    switch (choice)
                    {
                        case "Refresh Statistics":
                            // Just refresh by looping again
                            break;
                        case "View Detailed Genre Breakdown":
                            await ShowDetailedGenreBreakdown();
                            break;
                        case "View All Top Albums":
                            await ShowAllTopAlbums();
                            break;
                        case "Export Statistics":
                            await ExportStatistics(stats);
                            break;
                        case "Back to Main Menu":
                            inStatisticsMenu = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    await _logService.LogErrorAsync("StatisticsMenu", "MenuNavigation", "Statistics", 
                        $"Error in statistics menu: {ex.Message}", ex);
                    WaitForUser();
                }
            }
        }

        private void DisplayHeader()
        {
            var rule = new Rule("[bold blue]📊 Music Library Statistics[/]")
            {
                Justification = Justify.Left
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        private void DisplayOverallStats(MusicLibraryStatistics stats)
        {
            // Create a panel for overall stats
            var panel = new Panel($"""
                [bold]Artists:[/] {stats.TotalArtists}
                [bold]Albums:[/] {stats.TotalAlbums}
                [bold]Songs:[/] {stats.TotalSongs}
                [bold]Average Rating:[/] {stats.AverageAlbumRating}/5
                [bold]Songs per Album:[/] {stats.SongsPerAlbum}
                """)
                .Header(new PanelHeader("[bold]📈 Overview[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private void DisplayGenreStats(MusicLibraryStatistics stats)
        {
            if (!stats.AlbumsPerGenre.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No genre data available.[/]");
                return;
            }

            var totalAlbums = stats.AlbumsPerGenre.Values.Sum();
            
            // Create a bar chart for genre distribution
            var chart = new BarChart()
                .Width(60)
                .Label("[blue]Genres[/]")
                .CenterLabel();

            foreach (var (genre, count) in stats.AlbumsPerGenre.OrderByDescending(g => g.Value))
            {
                var percentage = totalAlbums > 0 ? (double)count / totalAlbums * 100 : 0;
                chart.AddItem($"{genre} ({count})", count, GetGenreColor(genre));
            }

            var panel = new Panel(chart)
                .Header(new PanelHeader($"[bold]🎵 Albums per Genre (Total: {totalAlbums})[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private void DisplayTopRatedAlbums(MusicLibraryStatistics stats)
        {
            if (!stats.TopRatedAlbums.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No rated albums available.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title(new TableTitle("[bold]🏆 Top Rated Albums[/]"));
            
            table.AddColumn(new TableColumn("[bold]#[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Album[/]"));
            table.AddColumn(new TableColumn("[bold]Rating[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Genre[/]"));

            int rank = 1;
            foreach (var album in stats.TopRatedAlbums)
            {
                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
                var ratingStars = GetRatingStars(album.Rating);
                
                table.AddRow(
                    $"[yellow]{rank++}[/]",
                    $"[white]{album.Title}[/]\n[grey]{artist?.Name ?? "Unknown"}[/]",
                    ratingStars,
                    $"[cyan]{album.Genre}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private void DisplayPlayTime(MusicLibraryStatistics stats)
        {
            var totalSeconds = stats.TotalPlayTime;
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            var panel = new Panel($"""
                [bold]Total Play Time:[/] {hours}h {minutes}m {seconds}s
                [bold]Average Song Length:[/] {GetAverageSongLength()} minutes
                """)
                .Header(new PanelHeader("[bold]⏱️ Play Time Analysis[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private void DisplayRecommendation(AlbumRecommendation recommendation)
        {
            if (!recommendation.HasRecommendation || recommendation.RecommendedAlbum == null)
            {
                var panel = new Panel("[yellow]Not enough data to generate recommendations.\nAdd more albums and ratings![/]")
                    .Header(new PanelHeader("[bold]💡 Recommendation[/]"))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Purple);
                
                AnsiConsole.Write(panel);
                return;
            }

            var album = recommendation.RecommendedAlbum;
            var ratingStars = GetRatingStars(album.Rating);
            
            var panelContent = $"""
                [bold]🎵 {album.Title}[/]
                [bold]👤 {recommendation.ArtistName}[/]
                [bold]⭐ Rating:[/] {ratingStars}
                [bold]📅 Year:[/] {album.ReleaseYear}
                [bold]🎶 Genre:[/] {album.Genre}
                
                [italic]{recommendation.Reason}[/]
                
                [grey]Based on your music library analysis[/]
                """;

            var recommendationPanel = new Panel(panelContent)
                .Header(new PanelHeader("[bold]💡 We Think You'll Love This![/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Purple);

            AnsiConsole.Write(recommendationPanel);
        }

        private async Task ShowDetailedGenreBreakdown()
        {
            Console.Clear();
            DisplayHeader();

            var stats = _musicLibrary.GetStatistics();
            
            if (!stats.AlbumsPerGenre.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No genre data available.[/]");
                WaitForUser();
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title(new TableTitle("[bold]📊 Detailed Genre Breakdown[/]"));
            
            table.AddColumn(new TableColumn("[bold]Genre[/]"));
            table.AddColumn(new TableColumn("[bold]Album Count[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Percentage[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Progress[/]"));

            var totalAlbums = stats.AlbumsPerGenre.Values.Sum();

            foreach (var (genre, count) in stats.AlbumsPerGenre.OrderByDescending(g => g.Value))
            {
                var percentage = totalAlbums > 0 ? Math.Round((double)count / totalAlbums * 100, 1) : 0;
                var progressBar = GetProgressBar(percentage, 20);
                
                table.AddRow(
                    $"[{GetGenreColor(genre)}]{genre}[/]",
                    $"[bold]{count}[/]",
                    $"[yellow]{percentage}%[/]",
                    progressBar
                );
            }

            table.AddRow(
                "[bold]Total[/]",
                $"[bold]{totalAlbums}[/]",
                "[bold]100%[/]",
                GetProgressBar(100, 20)
            );

            AnsiConsole.Write(table);
            
            // Show some insights
            AnsiConsole.WriteLine();
            if (stats.MostCommonGenre.HasValue)
            {
                var mostCommon = stats.MostCommonGenre.Value;
                var mostCommonCount = stats.AlbumsPerGenre[mostCommon];
                var percentage = Math.Round((double)mostCommonCount / totalAlbums * 100, 1);
                
                AnsiConsole.MarkupLine($"[bold]Insights:[/]");
                AnsiConsole.MarkupLine($"[grey]• Your favorite genre is [bold]{mostCommon}[/] ({percentage}% of your library)[/]");
                AnsiConsole.MarkupLine($"[grey]• You have [bold]{stats.AlbumsPerGenre.Count}[/] different genres in your collection[/]");
            }

            WaitForUser();
        }

        private async Task ShowAllTopAlbums()
        {
            Console.Clear();
            DisplayHeader();

            var allAlbums = _musicLibrary.GetAllAlbums()
                .Where(a => a.Rating > 0)
                .OrderByDescending(a => a.Rating)
                .ThenBy(a => a.Title)
                .ToList();

            if (!allAlbums.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No rated albums available.[/]");
                WaitForUser();
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title(new TableTitle($"[bold]🏆 All Rated Albums ({allAlbums.Count})[/]"));
            
            table.AddColumn(new TableColumn("[bold]Album[/]"));
            table.AddColumn(new TableColumn("[bold]Artist[/]"));
            table.AddColumn(new TableColumn("[bold]Rating[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Genre[/]"));
            table.AddColumn(new TableColumn("[bold]Year[/]").Centered());

            foreach (var album in allAlbums)
            {
                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
                var ratingStars = GetRatingStars(album.Rating);
                
                table.AddRow(
                    $"[white]{album.Title}[/]",
                    $"[cyan]{artist?.Name ?? "Unknown"}[/]",
                    ratingStars,
                    $"[green]{album.Genre}[/]",
                    $"[grey]{album.ReleaseYear}[/]"
                );
            }

            AnsiConsole.Write(table);

            // Show rating distribution
            AnsiConsole.WriteLine();
            var ratingGroups = allAlbums.GroupBy(a => a.Rating)
                .OrderByDescending(g => g.Key)
                .ToList();

            if (ratingGroups.Any())
            {
                var ratingChart = new BarChart()
                    .Width(40)
                    .Label("[blue]Rating Distribution[/]")
                    .CenterLabel();

                foreach (var group in ratingGroups)
                {
                    ratingChart.AddItem(
                        $"{GetRatingStars(group.Key)} ({group.Count()})", 
                        group.Count(), 
                        GetRatingColor(group.Key)
                    );
                }

                AnsiConsole.Write(new Panel(ratingChart)
                    .Header(new PanelHeader("[bold]⭐ Rating Distribution[/]"))
                    .Border(BoxBorder.Rounded));
            }

            WaitForUser();
        }

        private async Task ExportStatistics(MusicLibraryStatistics stats)
        {
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Exporting statistics...", async ctx =>
                    {
                        ctx.Spinner = Spinner.Known.Star;
                        ctx.SpinnerStyle = Style.Parse("green"));

                        // Simulate export process
                        await Task.Delay(1000);
                        
                        // In a real implementation, you would:
                        // 1. Create a CSV or JSON file
                        // 2. Save it to disk
                        // 3. Log the action
                        
                        AnsiConsole.MarkupLine("[green]Statistics exported successfully![/]");
                        AnsiConsole.MarkupLine("[grey](In a real implementation, this would save to a file)[/]");
                        
                        await _logService.LogCrudAsync("StatisticsMenu", "Export", "Statistics", 0, 
                            "Exported library statistics");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Export failed: {ex.Message}[/]");
                await _logService.LogErrorAsync("StatisticsMenu", "ExportStatistics", "Statistics", 
                    $"Failed to export statistics: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        #region Helper Methods

        private string GetRatingStars(int rating)
        {
            if (rating <= 0) return "[grey]No rating[/]";
            
            var stars = new string('★', rating);
            var emptyStars = new string('☆', 5 - rating);
            return $"[gold]{stars}[/][grey]{emptyStars}[/] ({rating}/5)";
        }

        private Color GetRatingColor(int rating)
        {
            return rating switch
            {
                5 => Color.Gold,
                4 => Color.Yellow,
                3 => Color.Orange,
                2 => Color.OrangeRed,
                1 => Color.Red,
                _ => Color.Grey
            };
        }

        private Color GetGenreColor(Genre genre)
        {
            return genre switch
            {
                Genre.Rock => Color.Red,
                Genre.Pop => Color.Pink,
                Genre.Jazz => Color.Blue,
                Genre.Classical => Color.Purple,
                Genre.HipHop => Color.Orange,
                Genre.Electronic => Color.Cyan,
                Genre.Country => Color.Green,
                Genre.RnB => Color.Magenta,
                Genre.Metal => Color.DarkRed,
                Genre.Indie => Color.LightGreen,
                _ => Color.Grey
            };
        }

        private string GetProgressBar(double percentage, int width)
        {
            var filled = (int)Math.Round(percentage / 100 * width);
            var empty = width - filled;
            
            var filledBar = new string('█', filled);
            var emptyBar = new string('░', empty);
            
            return $"[green]{filledBar}[/][grey]{emptyBar}[/]";
        }

        private string GetAverageSongLength()
        {
            var songs = _musicLibrary.GetAllSongs();
            if (!songs.Any()) return "0:00";

            var averageSeconds = (int)songs.Average(s => s.DurationSeconds);
            var minutes = averageSeconds / 60;
            var seconds = averageSeconds % 60;
            
            return $"{minutes}:{seconds:00}";
        }

        private void WaitForUser(string message = "Press any key to continue...")
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[grey]{message}[/]");
            Console.ReadKey();
        }

        #endregion
    }
}