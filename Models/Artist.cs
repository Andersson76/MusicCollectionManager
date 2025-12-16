using System;

namespace MusicCollectionManager.Models
{
    /// <summary>
    /// Representerar en musikartist i systemet.
    /// Detta är en domänmodell som innehåller endast data och enkel logik kopplad till objektets giltighet.
    /// </summary>
    public class Artist
    {
        // Privata fält (inkapsling)
        // Dessa kan inte ändras direkt utifrån klassen,
        // vilket skyddar objektets interna tillstånd.
        private int _id;
        private string _name = string.Empty;
        private string _country = string.Empty;
        private Genre _genre;

        /// <summary>
        /// Unikt ID för artisten
        /// </summary>
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// Artistens namn
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Ursprungsland
        /// </summary>
        public string Country
        {
            get => _country;
            set => _country = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Musikgenre
        /// </summary>
        public Genre Genre
        {
            get => _genre;
            set => _genre = value;
        }

        /// <summary>
        /// Kontrollerar om artistobjektet innehåller giltig data.
        /// Används för att säkerställa datakvalitet innan lagring.
        /// </summary>
        /// <returns>true om objektet är giltigt</returns>
        public bool IsValid()
        {
            return
                Id > 0 &&
                !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(Country);
        }
    }

    /// <summary>
    /// Enum som beskriver tillåtna musikgenrer.
    /// En enum begränsar värden och minskar risken för fel.
    /// </summary>
    public enum Genre
    {
        Unknown,
        Rock,
        Pop,
        HipHop,
        Jazz,
        Electronic,
        Classical,
        Metal
    }
}