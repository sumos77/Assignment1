using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace Assignment1
{
    public class Program
    {
        // We make the connection static because we need it in almost every method.
        private static SqlConnection connection;

        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Connect to the database.
            connection = new SqlConnection(@"Data Source=(local)\SQLExpress;Initial Catalog=Assignment1;Integrated Security=SSPI;");
            connection.Open();

            bool done = false;
            // We declare the choice here because we want to remember the previous one on each iteration and pass it as the "defaultOption" parameter to ShowMenu, for improved usability.
            int choice = 0;
            while (!done)
            {
                choice = ShowMenu("What do you want to do?", new[]
                {
                    "List movies A-Z",
                    "List movies by release date",
                    "Find movies by year",
                    "Add movie",
                    "Delete movie",
                    "Exit"
                }, choice);
                Console.Clear();

                if (choice == 0)
                {
                    ListMoviesAlphabetically();
                }
                else if (choice == 1)
                {
                    ListMoviesByReleaseDate();
                }
                else if (choice == 2)
                {
                    FindMoviesByYear();
                }
                else if (choice == 3)
                {
                    AddMovie();
                }
                else if (choice == 4)
                {
                    DeleteMovie();
                }
                else
                {
                    done = true;
                    Console.WriteLine("Goodbye!");
                }

                Console.WriteLine();
            }
        }

        private static void ListMoviesAlphabetically()
        {
            WriteHeading("Movies A-Z");

            string sql = @"
                SELECT Title, YEAR(ReleaseDate) AS ReleaseYear
                FROM Movie 
                ORDER BY Title, ReleaseDate DESC";
            using SqlCommand command = new SqlCommand(sql, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string title = Convert.ToString(reader["Title"]);
                string releaseYear = "(" + Convert.ToString(reader["ReleaseYear"]) + ")";
                Console.WriteLine("- " + title + " " + releaseYear);
            }
        }

        private static void ListMoviesByReleaseDate()
        {
            WriteHeading("Movies by release date");

            string sql = @"
                SELECT Title, YEAR(ReleaseDate) AS ReleaseYear
                FROM Movie
                ORDER BY ReleaseDate DESC, Title";
            using SqlCommand command = new SqlCommand(sql, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string title = Convert.ToString(reader["Title"]);
                string releaseYear = "(" + Convert.ToString(reader["ReleaseYear"]) + ")";
                Console.WriteLine("- " + title + " " + releaseYear);
            }
        }

        private static void FindMoviesByYear()
        {
            WriteHeading("Find movies by year");

            Console.Write("Year: ");

            try
            {
                int year = int.Parse(Console.ReadLine());
                Console.WriteLine();
                WriteHeading("Movies from " + year);

                string sql = @"
                    SELECT Title, YEAR(ReleaseDate) AS ReleaseYear
                    FROM Movie WHERE YEAR(ReleaseDate) = @Year
                    ORDER BY Title, ReleaseDate DESC";
                using SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Year", year);
                command.ExecuteNonQuery();

                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string title = Convert.ToString(reader["Title"]);
                    string releaseYear = "(" + Convert.ToString(reader["ReleaseYear"]) + ")";
                    Console.WriteLine("- " + title + " " + releaseYear);
                }
            }
            catch
            {
                Console.Clear();
                Console.WriteLine("Year is not of a valid format!");
            }
        }

        private static void AddMovie()
        {
            WriteHeading("Add movie");

            Console.Write("Title: ");
            string title = Console.ReadLine();

            Console.WriteLine("Release date:");
            Console.Write("Year: ");
            
            try
            {
                int year = int.Parse(Console.ReadLine());
                Console.Write("Month (1-12): ");
                int month = int.Parse(Console.ReadLine());
                Console.Write("Day (1-31): ");
                int day = int.Parse(Console.ReadLine());

                DateTime isValidDate = new DateTime(year, month, day);
                string releaseDate = isValidDate.ToString("yyyy-MM-dd");

                string sql = "INSERT INTO Movie (Title,ReleaseDate) Values (@Title, @ReleaseDate)";
                using SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@ReleaseDate", releaseDate);
                command.ExecuteNonQuery();

                Console.Clear();
                Console.WriteLine("Movie " + title + " (" + year + ") " + "added!");
            }
            catch
            {
                Console.Clear();
                Console.WriteLine("Date is not of a valid format!");
            }
        }

        private static void DeleteMovie()
        {
            List<string> options = new List<string>();
            List<int> movieIds = new List<int>();

            string sql = @"
                SELECT ID, Title, YEAR(ReleaseDate) AS ReleaseYear
                FROM Movie
                ORDER BY Title";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                using SqlDataReader reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["ID"]);
                    movieIds.Add(id);

                    string title = Convert.ToString(reader["Title"]);
                    string releaseYear = Convert.ToString(reader["ReleaseYear"]);
                    options.Add(title + " (" + releaseYear + ")");
                }
            }

            if (options.Count == 0)
            {
                Console.WriteLine("There are no movies to delete.");
                return;
            }

            int selectedIndex = ShowMenu("Delete movie", options.ToArray());
            int selectedMovieId = movieIds[selectedIndex];

            sql = "DELETE FROM Movie WHERE ID = @MovieId";
            using SqlCommand deleteCommand = new SqlCommand(sql, connection);
            deleteCommand.Parameters.AddWithValue("@MovieId", selectedMovieId);
            deleteCommand.ExecuteNonQuery();

            Console.Clear();
            Console.WriteLine("Movie " + options[selectedIndex] + " deleted!");
        }

        // The third parameter is the default option, which allows us to, for example, "remember" where the user was when they return to the main menu.
        // If we don't provide this parameter, it defaults to zero (i.e. highlight the first option).
        public static int ShowMenu(string prompt, string[] options, int defaultOption = 0)
        {
            if (options == null || options.Length == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty array of options.");
            }

            WriteHeading(prompt);

            int selected = defaultOption;

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                // If this is not the first iteration, move the cursor to the first line of the menu.
                if (key != null)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.CursorTop - options.Length;
                }

                // Print all the options, highlighting the selected one.
                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    if (i == selected)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine("- " + option);
                    Console.ResetColor();
                }

                // Read another key and adjust the selected value before looping to repeat all of this.
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Length - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }
            }

            // Reset the cursor and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }

        // Write a string with a line below it.
        public static void WriteHeading(string s)
        {
            Console.WriteLine(s);
            // Draw a line by repeating a hyphen as many times as the length of the prompt.
            Console.WriteLine(new string('-', s.Length));
        }
    }
}