using Spectre.Console;
using System;
using System.Threading;

namespace MusicCollectionManager.UI
{
    /// <summary>
    /// Huvudmeny (Spectre.Console)
    /// - Visar banner/intro
    /// - Navigerar till olika vyer (Artists/Albums/Tracks/Search/Statistics)
    /// - Visar spinner vid "data load"
    /// - Loopar tills användaren väljer Exit
    /// </summary>
    public static class MainMenu
    {
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
        /// (In med MusicLibraryService?).
        /// </summary>
        public static void Run()
        {
            ShowIntro();

            // Simulerad initial data load, ersätta?
            ShowLoadingSpinner("Loading data...");

            bool running = true;
            while (running)
            {
                AnsiConsole.Clear();
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
                        NavigateTo("Artists");
                        // TODO: ArtistsUI.Show();
                        break;

                    case "Albums":
                        NavigateTo("Albums");
                        // TODO: AlbumsUI.Show();
                        break;

                    case "Tracks":
                        NavigateTo("Tracks");
                        // TODO: TracksUI.Show();
                        break;

                    case "Search":
                        NavigateTo("Search");
                        // TODO: SearchUI.Show();
                        break;

                    case "Statistics":
                        NavigateTo("Statistics");
                        // TODO: StatisticsUI.Show();
                        break;

                    case "Exit":
                        running = ConfirmExit();
                        break;
                }
            }

            ShowGoodbye();
        }

        private static void ShowIntro()
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
            ShowLoadingSpinner("Booting Spectre.Console...");
        }

        private static void ShowHeader()
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

        private static void NavigateTo(string sectionName)
        {
            // Spinner varje gång man går till en sektion
            ShowLoadingSpinner($"Opening {sectionName}...");

            // Tillfällig placeholder vy
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold aqua]{sectionName}[/]")
            {
                Justification = Justify.Left
            });

            AnsiConsole.MarkupLine("[grey]Här kopplar ni in er riktiga UI-sida senare.[/]");
            AnsiConsole.MarkupLine("Tryck valfri tangent för att gå tillbaka till menyn...");
            Console.ReadKey(true);
        }

        private static void ShowLoadingSpinner(string message)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .Start(message, ctx =>
                {
                    // Simulerad väntan – byt till riktig load (IO/JSON/DB)
                    Thread.Sleep(700);
                });
        }

        private static bool ConfirmExit()
        {
            var confirm = AnsiConsole.Confirm("Vill du avsluta programmet?", false);
            return !confirm; // om true = avsluta -> running = false
        }

        private static void ShowGoodbye()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold green]Tack för att du använde Music Collection Manager![/]");
            Thread.Sleep(350);
        }
    }
}
