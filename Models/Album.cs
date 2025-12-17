namespace MusicCollectionManager.Models
{
    /// <summary>
    /// Representerar ett musikalbum i systemet.
    /// - Inkapsling (privata fält, publika properties)
    /// - Association till Artist via ArtistId
    /// </summary>
    public class Album
    {
        // Inkapsling:
        // Fälten är privata för att skydda objektets interna tillstånd.
        // All åtkomst sker via publika properties.
        private int _id;
        private string _title = string.Empty;
        private int _artistId;
        private int _releaseYear;
        private Genre _genre;
        private int _rating;

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        public string Title
        {
            get => _title;
            set => _title = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Association till Artist.
        /// Album "har en" Artist, men äger den inte.
        /// Relationen hanteras genom ArtistId istället för ett Artist-objekt.
        /// </summary>
        public int ArtistId
        {
            get => _artistId;
            set => _artistId = value;
        }

        public int ReleaseYear
        {
            get => _releaseYear;
            set => _releaseYear = value;
        }

        public Genre Genre
        {
            get => _genre;
            set => _genre = value;
        }

        /// <summary>
        /// Betyg på albumet (1–5).
        /// Set är privat för att säkerställa att all logik går via UpdateRating().
        /// </summary>
        public int Rating
        {
            get => _rating;
            private set => _rating = value;
        }

        /// <summary>
        /// Uppdaterar betyget på albumet.
        /// Endast värden mellan 1 och 5 accepteras.
        /// </summary>
        public void UpdateRating(int rating)
        {
            if (rating < 1 || rating > 5)
                return;

            Rating = rating;
        }

        /// <summary>
        /// Validerar att albumet innehåller korrekt och rimlig data.
        /// Används innan album sparas eller visas.
        /// </summary>
        public bool IsValid()
        {
            if (Id <= 0) return false;
            if (string.IsNullOrWhiteSpace(Title)) return false;
            if (ArtistId <= 0) return false;

            // Rimlig kontroll av utgivningsår
            if (ReleaseYear < 1900 || ReleaseYear > System.DateTime.Now.Year) return false;

            // Rating får vara 0 (ej satt) eller 1–5
            if (Rating != 0 && (Rating < 1 || Rating > 5)) return false;

            return true;
        }
    }
}