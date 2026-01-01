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
                try
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
                            .Title("[bold]Statistics Options[/]")
                            .PageSize(10)
                            .AddChoices(new[]
                            {
                                "🔄 Refresh Statistics",
                                "🎵 View Detailed Genre Breakdown",
                                "🏆 View All Top Albums",
                                "📤 Export Statistics (Simulated)",
                                "🔙 Back to Main Menu"
                            }));

                    switch (choice)
                    {
                        case "🔄 Refresh Statistics":
                            // Refresh by looping again
                            break;
                        case "🎵 View Detailed Genre Breakdown":
                            await ShowDetailedGenreBreakdown();
                            break;
                        case "🏆 View All Top Albums":
                            await ShowAllTopAlbums();
                            break;
                        case "📤 Export Statistics (Simulated)":
                            await ExportStatistics(stats);
                            break;
                        case "🔙 Back to Main Menu":
                            inStatisticsMenu = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    await _logService.LogErrorAsync("StatisticsMenu", "ShowStatistics", "Statistics", 
                        $"Error in statistics menu: {ex.Message}", ex);
                    WaitForUser();
                }
            }
        }

        private void DisplayHeader()
        {
            AnsiConsole.Render(
                new Rule("[bold cyan]📊 Music Library Statistics[/]")
                    .LeftAligned()
            );
            AnsiConsole.WriteLine();
        }

        private void DisplayOverallStats(MusicLibraryStatistics stats)
        {
            var panel = new Panel($"""
                [bold]👤 Artists:[/] {stats.TotalArtists}
                [bold]💿 Albums:[/] {stats.TotalAlbums}
                [bold]🎵 Tracks:[/] {stats.TotalSongs}
                [bold]⭐ Average Rating:[/] {stats.AverageAlbumRating}/5
                [bold]📈 Songs per Album:[/] {stats.SongsPerAlbum}
                """)
                .Header(new PanelHeader("[bold cyan]📈 Library Overview[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private void DisplayGenreStats(MusicLibraryStatistics stats)
        {
            if (!stats.AlbumsPerGenre.Any())
            {
                AnsiConsole.MarkupLine("[yellow]📭 No genre data available. Add some albums first![/]");
                return;
            }

            var totalAlbums = stats.TotalAlbums;
            var mostCommonGenre = stats.MostCommonGenre;
            
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green)
                .Title(new TableTitle($"[bold green]🎵 Albums by Genre (Total: {totalAlbums})[/]"));
            
            table.AddColumn(new TableColumn("[bold]Genre[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Count[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Percentage[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Chart[/]"));

            foreach (var (genre, count) in stats.AlbumsPerGenre.OrderByDescending(g => g.Value))
            {
                var percentage = totalAlbums > 0 ? Math.Round((double)count / totalAlbums * 100, 1) : 0;
                var isFavorite = mostCommonGenre.HasValue && genre == mostCommonGenre.Value;
                var genreText = isFavorite ? $"[bold gold]{genre} 👑[/]" : $"[cyan]{genre}[/]";
                
                table.AddRow(
                    genreText,
                    $"[white]{count}[/]",
                    $"[yellow]{percentage}%[/]",
                    GetProgressBar(percentage, 20)
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private void DisplayTopRatedAlbums(MusicLibraryStatistics stats)
        {
            if (!stats.TopRatedAlbums.Any())
            {
                AnsiConsole.MarkupLine("[yellow]⭐ No rated albums available. Rate some albums first![/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Yellow)
                .Title(new TableTitle("[bold gold]🏆 Top Rated Albums[/]"));
            
            table.AddColumn(new TableColumn("[bold]#[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Album[/]"));
            table.AddColumn(new TableColumn("[bold]Artist[/]"));
            table.AddColumn(new TableColumn("[bold]Rating[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Genre[/]"));

            int rank = 1;
            foreach (var album in stats.TopRatedAlbums)
            {
                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
                var ratingStars = GetRatingStars(album.Rating);
                
                var rankText = rank == 1 ? "[gold]🥇[/]" : 
                              rank == 2 ? "[silver]🥈[/]" : 
                              rank == 3 ? "[#CD7F32]🥉[/]" : $"[grey]{rank}[/]";
                
                table.AddRow(
                    rankText,
                    $"[bold white]{album.Title}[/]",
                    $"[cyan]{artist?.Name ?? "Unknown"}[/]",
                    ratingStars,
                    $"[green]{album.Genre}[/]"
                );
                rank++;
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

            var averageSeconds = stats.TotalSongs > 0 ? totalSeconds / stats.TotalSongs : 0;
            var avgMinutes = averageSeconds / 60;
            var avgSeconds = averageSeconds % 60;

            var panel = new Panel($"""
                [bold]⏱️ Total Play Time:[/] {hours}h {minutes}m {seconds}s
                [bold]📊 Average Track Length:[/] {avgMinutes}:{avgSeconds:00}
                [bold]🎯 Total Tracks:[/] {stats.TotalSongs}
                """)
                .Header(new PanelHeader("[bold yellow]⏱️ Play Time Analysis[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private void DisplayRecommendation(AlbumRecommendation recommendation)
        {
            if (!recommendation.HasRecommendation || recommendation.RecommendedAlbum == null)
            {
                var panel = new Panel("""
                    [yellow]📭 Not enough data to generate recommendations!
                    
                    Add more albums and rate them to get personalized recommendations.
                    [/]")
                    .Header(new PanelHeader("[bold purple]💡 Your Music Recommendations[/]"))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Purple);
                
                AnsiConsole.Write(panel);
                return;
            }

            var album = recommendation.RecommendedAlbum;
            var ratingStars = GetRatingStars(album.Rating);
            var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
            
            var panelContent = $"""
                [bold gold]🎵 {album.Title}[/]
                [bold cyan]👤 {artist?.Name ?? "Unknown Artist"}[/]
                
                [bold]⭐ Rating:[/] {ratingStars}
                [bold]📅 Year:[/] {album.ReleaseYear}
                [bold]🎶 Genre:[/] {album.Genre}
                
                [italic cyan]{recommendation.Reason}[/]
                
                [grey]This recommendation is based on your listening habits[/]
                """;

            var recommendationPanel = new Panel(panelContent)
                .Header(new PanelHeader("[bold purple]💡 We Think You'll Love This![/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Purple);

            AnsiConsole.Write(recommendationPanel);
        }

        private async Task ShowDetailedGenreBreakdown()
        {
            try
            {
                Console.Clear();
                DisplayHeader();

                var stats = _musicLibrary.GetStatistics();
                
                if (!stats.AlbumsPerGenre.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]📭 No genre data available.[/]");
                    WaitForUser();
                    return;
                }

                var totalAlbums = stats.TotalAlbums;
                var mostCommonGenre = stats.MostCommonGenre;

                AnsiConsole.MarkupLine("[bold cyan]📊 Detailed Genre Analysis[/]");
                AnsiConsole.WriteLine();

                // Create a breakdown grid
                var grid = new Grid()
                    .AddColumn(new GridColumn().PadRight(2))
                    .AddColumn(new GridColumn().PadRight(2));

                var leftPanel = new Panel(GetGenreBreakdownText(stats))
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]📈 Genre Distribution[/]");

                var rightPanel = new Panel(GetGenreInsightsText(stats))
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]💡 Insights[/]");

                grid.AddRow(leftPanel, rightPanel);
                AnsiConsole.Write(grid);

                // Show bar chart
                AnsiConsole.WriteLine();
                var chart = new BarChart()
                    .Width(60)
                    .Label("[blue]Album Count by Genre[/]")
                    .CenterLabel();

                foreach (var (genre, count) in stats.AlbumsPerGenre.OrderByDescending(g => g.Value))
                {
                    chart.AddItem($"{genre} ({count})", count, GetGenreColor(genre));
                }

                AnsiConsole.Write(new Panel(chart)
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]📊 Visual Distribution[/]"));

                WaitForUser();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("StatisticsMenu", "ShowDetailedGenreBreakdown", "Statistics", 
                    $"Error showing genre breakdown: {ex.Message}", ex);
                WaitForUser();
            }
        }

        private async Task ShowAllTopAlbums()
        {
            try
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
                    AnsiConsole.MarkupLine("[yellow]⭐ No rated albums available.[/]");
                    WaitForUser();
                    return;
                }

                // Group by rating
                var ratingGroups = allAlbums.GroupBy(a => a.Rating)
                    .OrderByDescending(g => g.Key)
                    .ToList();

                // Display rating summary
                var summaryPanel = new Panel($"""
                    [bold]Total Rated Albums:[/] {allAlbums.Count}
                    [bold]Highest Rating:[/] {GetRatingStars(ratingGroups.First().Key)}
                    [bold]Average Rating:[/] {Math.Round(allAlbums.Average(a => a.Rating), 1)}/5
                    """)
                    .Border(BoxBorder.Rounded)
                    .Header("[bold gold]⭐ Rating Summary[/]");

                AnsiConsole.Write(summaryPanel);
                AnsiConsole.WriteLine();

                // Display all albums in a table
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title(new TableTitle($"[bold]All Rated Albums ({allAlbums.Count})[/]"));
                
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
                var distributionTable = new Table()
                    .Border(TableBorder.Simple)
                    .Title(new TableTitle("[bold]📊 Rating Distribution[/]"));
                
                distributionTable.AddColumn(new TableColumn("[bold]Rating[/]").Centered());
                distributionTable.AddColumn(new TableColumn("[bold]Count[/]").Centered());
                distributionTable.AddColumn(new TableColumn("[bold]Percentage[/]").Centered());

                foreach (var group in ratingGroups)
                {
                    var percentage = Math.Round((double)group.Count() / allAlbums.Count * 100, 1);
                    distributionTable.AddRow(
                        GetRatingStars(group.Key),
                        $"[white]{group.Count()}[/]",
                        $"[yellow]{percentage}%[/]"
                    );
                }

                AnsiConsole.Write(distributionTable);

                WaitForUser();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                await _logService.LogErrorAsync("StatisticsMenu", "ShowAllTopAlbums", "Statistics", 
                    $"Error showing all top albums: {ex.Message}", ex);
                WaitForUser();
            }
        }

        private async Task ExportStatistics(MusicLibraryStatistics stats)
        {
            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .SpinnerStyle(Style.Parse("green"))
                    .StartAsync("Exporting statistics...", async ctx =>
                    {
                        ctx.Status = "Generating report...";
                        await Task.Delay(500);

                        ctx.Status = "Formatting data...";
                        await Task.Delay(500);

                        ctx.Status = "Creating file...";
                        await Task.Delay(500);

                        // Simulate file creation
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var fileName = $"music_stats_{timestamp}.txt";
                        
                        // In a real implementation, you would write to a file
                        var reportContent = GenerateTextReport(stats);
                        
                        AnsiConsole.MarkupLine($"[green]✅ Statistics report generated: {fileName}[/]");
                        AnsiConsole.MarkupLine("[grey](Simulated export - in production this would save to disk)[/]");
                        
                        // Show a preview
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[bold cyan]📄 Report Preview:[/]");
                        AnsiConsole.WriteLine(new string('─', 50));
                        Console.WriteLine(reportContent.Substring(0, Math.Min(200, reportContent.Length)) + "...");
                        AnsiConsole.WriteLine(new string('─', 50));
                        
                        await _logService.LogInfoAsync("StatisticsMenu", "ExportStatistics", "Statistics", 
                            $"Generated statistics report: {fileName}");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Export failed: {ex.Message}[/]");
                await _logService.LogErrorAsync("StatisticsMenu", "ExportStatistics", "Statistics", 
                    $"Failed to export statistics: {ex.Message}", ex);
            }
            
            WaitForUser();
        }

        #region Helper Methods

        private string GetGenreBreakdownText(MusicLibraryStatistics stats)
        {
            var text = "";
            var totalAlbums = stats.TotalAlbums;
            
            foreach (var (genre, count) in stats.AlbumsPerGenre.OrderByDescending(g => g.Value))
            {
                var percentage = totalAlbums > 0 ? Math.Round((double)count / totalAlbums * 100, 1) : 0;
                text += $"[{GetGenreColor(genre)}]{genre}:[/] {count} albums ({percentage}%)\n";
            }
            
            return text;
        }

        private string GetGenreInsightsText(MusicLibraryStatistics stats)
        {
            if (!stats.MostCommonGenre.HasValue || stats.TotalAlbums == 0)
                return "Not enough data for insights.";

            var mostCommon = stats.MostCommonGenre.Value;
            var mostCommonCount = stats.AlbumsPerGenre[mostCommon];
            var percentage = Math.Round((double)mostCommonCount / stats.TotalAlbums * 100, 1);
            
            var text = $"""
                [bold]Favorite Genre:[/] [{GetGenreColor(mostCommon)}]{mostCommon}[/]
                [grey]({percentage}% of your collection)[/]
                
                [bold]Genre Diversity:[/] {stats.AlbumsPerGenre.Count} genres
                
                [bold]Collection Balance:[/]
                """;

            if (percentage > 50)
                text += "[yellow]Heavily focused on one genre[/]";
            else if (stats.AlbumsPerGenre.Count >= 5)
                text += "[green]Well-balanced collection[/]";
            else
                text += "[cyan]Moderate genre variety[/]";

            return text;
        }

        private string GetRatingStars(int rating)
        {
            if (rating <= 0) return "[grey]No rating[/]";
            
            var stars = new string('★', rating);
            var emptyStars = new string('☆', 5 - rating);
            return $"[gold]{stars}[/][grey]{emptyStars}[/]";
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

        private string GenerateTextReport(MusicLibraryStatistics stats)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var report = $"""
                MUSIC LIBRARY STATISTICS REPORT
                Generated: {timestamp}
                ===========================================
                
                OVERVIEW
                ---------
                Total Artists: {stats.TotalArtists}
                Total Albums: {stats.TotalAlbums}
                Total Tracks: {stats.TotalSongs}
                Average Rating: {stats.AverageAlbumRating}/5
                Average Tracks per Album: {stats.SongsPerAlbum}
                
                GENRE DISTRIBUTION
                ------------------
                """;

            foreach (var (genre, count) in stats.AlbumsPerGenre.OrderByDescending(g => g.Value))
            {
                var percentage = Math.Round((double)count / stats.TotalAlbums * 100, 1);
                report += $"{genre}: {count} albums ({percentage}%)\n";
            }

            report += $"""
                
                TOP RATED ALBUMS
                -----------------
                """;

            int rank = 1;
            foreach (var album in stats.TopRatedAlbums)
            {
                var artist = _musicLibrary.GetAllArtists().FirstOrDefault(a => a.Id == album.ArtistId);
                report += $"{rank}. {album.Title} by {artist?.Name ?? "Unknown"} - {album.Rating}/5\n";
                rank++;
            }

            var totalSeconds = stats.TotalPlayTime;
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            
            report += $"""
                
                PLAY TIME ANALYSIS
                ------------------
                Total Play Time: {hours}h {minutes}m
                Average Track Length: {GetAverageSongLength()} minutes
                
                RECOMMENDATION
                ---------------
                """;

            var recommendation = _musicLibrary.GetAlbumRecommendation();
            if (recommendation.HasRecommendation && recommendation.RecommendedAlbum != null)
            {
                report += $"Based on your listening habits, we recommend:\n";
                report += $"{recommendation.RecommendedAlbum.Title} by {recommendation.ArtistName}\n";
                report += $"Reason: {recommendation.Reason}\n";
            }
            else
            {
                report += "Not enough data for recommendations.\n";
            }

            return report;
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