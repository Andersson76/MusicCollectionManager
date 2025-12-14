using MusicCollectionManager.Tests;
using MusicCollectionManager.UI;

namespace MusicCollectionManager
{
    internal class Program
    {
        //static void Main(string[] args)
       // {
            //var menu = new Menu();
            //menu.Start();
        //}

         static async Task Main(string[] args)
    {
        Console.WriteLine("Music Collection Manager - JSON Service Tester");
        Console.WriteLine("===============================================\n");
        
        var tester = new JsonServiceTester();
        
        // Run normal tests
        await tester.RunTests();
        
        // Run error scenario tests
        tester.TestErrorScenarios();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    }
}
