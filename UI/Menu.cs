using Spectre.Console;
using System;
using System.Threading.Tasks;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.UI
{
    /// <summary>
    /// Huvudmeny (Spectre.Console)
    /// - Visar banner/intro
    /// - Navigerar till olika vyer (Artists/Albums/Tracks/Search/Statistics)
    /// - Visar spinner vid "data load"
    /// - Loopar tills användaren väljer Exit
    /// </summary>
    public class MainMenu
    {
        private readonly MusicLibraryService _musicLibrary;
        private readonly LogService _logService;

        public MainMenu(MusicLibraryService musicLibrary, LogService logService)
        {
            _musicLibrary = musicLibrary ?? throw new ArgumentNullException(nameof(musicLibrary));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // Menyval enligt issuet
        private static readonly string[] MenuItems =
        {
            "Artists",
            "Albums",
            "Tracks",
            "Search",
            "Statistics",
            "Exit"
        };

        /// <summary>
        /// Startar huvudmeny loopen.
        /// </summary>
        public async Task Run()
        {
            ShowIntro();

            // Simulerad initial data load
            await ShowLoadingSpinnerAsync("Loading data...");

            bool running = true;
            while (running)
            {
                Console.Clear();
                ShowHeader();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Välj ett alternativ:[/]")
                        .PageSize(10)
                        .HighlightStyle(new Style(foreground: Color.Black, background: Color.Grey))
                        .AddChoices(MenuItems)
                );

                switch (choice)
                {
                    case "Artists":
                        await NavigateToArtistMenu();
                        break;

                    case "Albums":
                        await NavigateToAlbumMenu();
                        break;

                    case "Tracks":
                        await NavigateToTrackMenu();
                        break;

                    case "Search":
                        await NavigateToSearchMenu();
                        break;

                    case "Statistics":
                        await NavigateToStatistics();
                        break;

                    case "Exit":
                        running = ConfirmExit();
                        break;
                }
            }

            ShowGoodbye();
        }

        private void ShowIntro()
        {
            AnsiConsole.Clear();

            // Liten "startanimation"
            var title = new FigletText("Music")
                .Color(Color.Green);

            var subtitle = new FigletText("Collection Manager")
                .Color(Color.Aqua);

            AnsiConsole.Write(title);
            AnsiConsole.Write(subtitle);

            AnsiConsole.MarkupLine("[grey]Initializing UI...[/]");
        }

        private void ShowHeader()
        {
            var rule = new Rule("[bold green]Main Menu[/]")
            {
                Style = Style.Parse("grey"),
                Justification = Justify.Left
            };

            AnsiConsole.Write(rule);

            AnsiConsole.MarkupLine(
                "[grey]Tips:[/] Använd [bold]piltangenter[/] + [bold]Enter[/].\n");
        }

        private async Task NavigateToArtistMenu()
        {
            await ShowLoadingSpinnerAsync("Opening Artists...");
            
            var artistMenu = new ArtistMenu(_musicLibrary, _logService);
            await artistMenu.ShowArtistMenu();
        }

        private async Task NavigateToAlbumMenu()
        {
            await ShowLoadingSpinnerAsync("Opening Albums...");
            
            var albumMenu = new AlbumMenu(_musicLibrary, _logService);
            await albumMenu.ShowAlbumMenu();
        }

        private async Task NavigateToTrackMenu()
        {
            await ShowLoadingSpinnerAsync("Opening Tracks...");
            
            var trackMenu = new TrackMenu(_musicLibrary, _logService);
            await trackMenu.ShowTrackMenu();
        }

        private async Task NavigateToSearchMenu()
        {
            await ShowLoadingSpinnerAsync("Opening Search...");
            
            var searchMenu = new SearchMenu(_musicLibrary, _logService);
            await searchMenu.ShowSearchMenu();
        }

        private async Task NavigateToStatistics()
        {
            await ShowLoadingSpinnerAsync("Loading Statistics...");
            
            var statistics = new StatisticsMenu(_musicLibrary, _logService);
            await statistics.ShowStatistics();
        }

        private async Task ShowLoadingSpinnerAsync(string message)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .StartAsync(message, async ctx =>
                {
                    // Simulerad väntan
                    await Task.Delay(700);
                });
        }

        private bool ConfirmExit()
        {
            var confirm = AnsiConsole.Confirm("Vill du avsluta programmet?", false);
            return !confirm; // om true = avsluta -> running = false
        }

        private void ShowGoodbye()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold green]Tack för att du använde Music Collection Manager![/]");
            System.Threading.Thread.Sleep(350);
        }
    }
}