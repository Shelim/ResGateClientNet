using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Example1
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
                waitForInitial.Set();
            };

            using (var client = new Resgate.Client(settings))
            {
                using (var token =
                    await client.SubscribeCollection<Book>("library.books", Initial, Added, Changed, Removed))
                {

                    waitForInitial.WaitOne();
                    for (;;)
                    {
                        DisplayInfo();
                        var key = Console.ReadKey(true);
                        switch (key.KeyChar)
                        {
                            case 'a':
                            case 'A':
                                AddBook();
                                break;
                            case 'q':
                            case 'Q':
                                return;
                        }
                    }
                }
            }
        }

        private static List<Book> data;
        private static object guard = new object();
        private static ManualResetEvent waitForInitial = new ManualResetEvent(false);

        private static void DisplayInfo()
        {
            lock (guard)
            {
                Console.WriteLine("What do you want to do next?");
                Console.WriteLine("(A)dd book");
                Console.WriteLine("(E)dit book");
                Console.WriteLine("(D)elete book");
                Console.WriteLine("(Q)uit");
                Console.WriteLine("________________");
            }
        }

        private static void AddBook()
        {
            string title, author;
            lock (guard)
            {
                Console.WriteLine("Enter book title:");
                title = Console.ReadLine();
                Console.WriteLine("Enter book author:");
                author = Console.ReadLine();
            }
        }

        private static void Initial(List<Book> books)
        {
            lock (guard)
            {
                data = books;
                Console.WriteLine("Initial state:");
                foreach (var book in books)
                {
                    Console.WriteLine(book.title + " by " + book.author);
                }
                Console.WriteLine("________________");
                waitForInitial.Set();
            }
        }

        private static void Added(int index, Book book)
        {
            lock (guard)
            {
                Console.WriteLine("Added: " + book.title + " by " + book.author);
                data.Insert(index, book);
            }
        }

        private static void Changed(int index, Book book)
        {
            lock (guard)
            {
                var prev = data[index];
                Console.WriteLine("Changed: " + prev.title + " by " + prev.author + " into " + book.title + " by " +
                                  book.author);
                data[index] = book;
            }
        }

        private static void Removed(int index)
        {
            lock (guard)
            {
                var book = data[index];
                Console.WriteLine("Removed: " + book.title + " by " + book.author);
                data.RemoveAt(index);
            }
        }
    }
}
