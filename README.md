# ResGateClientNet

Implementation of [ResGate](https://resgate.io) Client in .NET

Very liberal license, MIT

No credit (in binary form) is required

(Will amend this readme later on)

## Examples

Subscriber model is based on idea of tokens. You will get `IDisposable` token
each time you subscribe to any resource. As long as this token is not disposed
you will get updates to that resource.

Implementation support reconnecting on failure, and server polling (for example if you have more
then one end-point)

Requires Newtonsoft.JSON and WebSocket.Client

Requires ResGate version at least 1.2.0

DEMO Prequestitions:
Run [this example](https://github.com/resgateio/resgate/tree/master/examples/book-collection)

```csharp
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
```
