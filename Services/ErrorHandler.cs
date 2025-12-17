using System;
using System.Threading.Tasks;

namespace MusicCollectionManager.Services
{
    /// <summary>
    /// Centralized error handling for the application.
    /// Provides consistent error reporting and logging.
    /// </summary>
    public class ErrorHandler
    {
        /// <summary>
        /// Handles application-level errors and displays them to the user.
        /// </summary>
        public void HandleFatalError(Exception exception, string context = "application")
        {
            Console.Clear();
            
            // Display error header
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  FATAL ERROR                         ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine($"\nContext: {context}");
            Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Error Type: {exception.GetType().Name}");
            
            // Display error message
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n📛 Error Message:");
            Console.ResetColor();
            Console.WriteLine($"  {exception.Message}");
            
            // Display inner exception if exists
            if (exception.InnerException != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"\n🔍 Inner Exception:");
                Console.ResetColor();
                Console.WriteLine($"  {exception.InnerException.Message}");
            }
            
            // Display stack trace in development mode
            #if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n🐛 Stack Trace (DEBUG MODE):");
            Console.ResetColor();
            Console.WriteLine($"  {exception.StackTrace}");
            #endif
            
            // Display recovery instructions
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🛠️  Possible Solutions:");
            Console.ResetColor();
            Console.WriteLine("  1. Check if data files are accessible");
            Console.WriteLine("  2. Verify you have write permissions");
            Console.WriteLine("  3. Restart the application");
            Console.WriteLine("  4. Contact support if problem persists");
            
            // Display shutdown message
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n⛔ The application must close.");
            Console.ResetColor();
            
            Console.Write("\nPress any key to exit... ");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles non-fatal errors with optional recovery.
        /// </summary>
        public async Task<bool> HandleRecoverableErrorAsync(Exception exception, string action, 
            Func<Task<bool>>? recoveryAction = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  Error during {action}:");
            Console.ResetColor();
            Console.WriteLine($"  {exception.Message}");
            
            if (recoveryAction != null)
            {
                Console.Write("\nWould you like to try again? (y/n): ");
                var response = Console.ReadLine()?.ToLower();
                
                if (response == "y" || response == "yes")
                {
                    try
                    {
                        return await recoveryAction();
                    }
                    catch (Exception retryEx)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Retry failed: {retryEx.Message}");
                        Console.ResetColor();
                        return false;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Validates user input with customizable error messages.
        /// </summary>
        public bool ValidateInput(string? input, out string errorMessage, params Func<string?, bool>[] validators)
        {
            errorMessage = string.Empty;
            
            if (validators.Length == 0)
            {
                // Default validator: not null or empty
                if (string.IsNullOrWhiteSpace(input))
                {
                    errorMessage = "Input cannot be empty.";
                    return false;
                }
                return true;
            }
            
            foreach (var validator in validators)
            {
                if (!validator(input))
                {
                    errorMessage = "Input validation failed.";
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Displays a warning message to the user.
        /// </summary>
        public void ShowWarning(string message, string? details = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  Warning: {message}");
            Console.ResetColor();
            
            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine($"  Details: {details}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Displays an informational message.
        /// </summary>
        public void ShowInfo(string message, bool pause = true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\nℹ️  {message}");
            Console.ResetColor();
            
            if (pause)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}