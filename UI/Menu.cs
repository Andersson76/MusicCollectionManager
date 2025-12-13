
namespace MusicCollectionManager.UI
{
    internal class Menu
    {
        public void Start()
        {
            Console.WriteLine("=== Music Collection Manager ===");
            Console.WriteLine("1. Placeholder");
            Console.WriteLine("Q. Avsluta");

            var input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "Q") return;

            Console.WriteLine("Inte implementerat än.");
        }
    }
}
