using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    class Program
    {
        class Book
        {
            public int id;
            public string title;
            public string author;
        }


        static async Task Main(string[] args)
        {
            var settings = new Resgate.Settings(() => new Uri("ws://localhost:8080"));
            settings.Failed += (o, ev) =>
            {
                Console.WriteLine("Failed to connect due to " + ev.Reason.ToString());
            };

            var client = new Resgate.Client(settings);

            Console.WriteLine("Press any key to unsubscribe...");

            var token = await client.SubscribeCollection<Book>("library.books", Initial, Added, Changed, Removed);
            Console.ReadKey();
            token.Dispose();


            Console.WriteLine("Press any key to end...");
            Console.ReadKey();
        }

        private static List<Book> data;

        private static void Initial(List<Book> books)
        {
            data = books;
            Console.WriteLine("Initial state:");
            foreach (var book in books)
            {
                Console.WriteLine(book.title + " by " + book.author);
            }
            Console.WriteLine("________________");
        }

        private static void Added(int index, Book book)
        {
            Console.WriteLine("Added: " + book.title + " by " + book.author);
            data.Insert(index, book);
        }

        private static void Changed(int index, Book book)
        {
            var prev = data[index];
            Console.WriteLine("Changed: " + prev.title  + " by " + prev.author + " into " + book.title + " by " + book.author);
            data[index] = book;
        }

        private static void Removed(int index)
        {
            var book = data[index];
            Console.WriteLine("Removed: " + book.title + " by " + book.author);
            data.RemoveAt(index);
        }
    }
}
