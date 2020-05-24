# ResGateClientNet

Implementation of [ResGate](https://resgate.io) Client in .NET

Very liberal license, MIT

No credit (in binary form) is required

## Basics

Subscriber model is based on idea of tokens. You will get `IDisposable` token
each time you subscribe to any resource. As long as this token is not disposed
you will get updates to that resource.

Implementation support reconnecting on failure, and server polling (for example if you have more
then one end-point). Registered resources survives reconnection (they will be resubscribed)

Requires Newtonsoft.JSON and WebSocket.Client

Requires ResGate version at least 1.2.0

## Examples

EXAMPLE 1.
Run [this server app](https://github.com/resgateio/resgate/tree/master/examples/book-collection) - Book Collection

EXAMPLE 2.
Run [this server app](https://github.com/resgateio/resgate/tree/master/examples/password-authentication) - Password Authentication

## Roadmap

Last updated: May, 22nd, 2020

### ToDo

 - [x] ~~Support for reconnection on failure~~
 - [x] ~~Get methods~~
 - [x] ~~Subscribe and unsubscribe methods~~
 - [x] ~~Call methods~~
 - [x] ~~Auth methods~~
 - [x] ~~Error handling~~
 - [x] ~~Re-auth on reconnect (or anything else, actually)~~
 - [ ] First release on nugets

## API

```csharp
// Important: all callbacks are fired from secondary thread.
// Locking and syncing is up to you!

// The action can be used to supply different Uris on each attempt.
// The reconnection is automatic
var settings = new Resgate.Settings(() => new Uri("ws://localhost:8080"));

// Failed means something terrible has happen, and no reconnection can fix that.
// Currently supported are:
//  - UnsupportedVersion and VersionNegotiationFailed
//      They means the server is up, accessible, but has invalid version.

settings.Failed += (o, ev) =>
{
    Console.WriteLine("Failed to connect due to " + ev.Reason.ToString());
}

// If there is a problem with subscribe request, it will be reported here
// (as subscription must be re-applied every new reconnection after
// connection has been lost)
settings.Error += (obj, evnt) =>
{
	Console.WriteLine("Failed to subscribe " + evnt.Rid + " because of " +
					  evnt.Error.Message);
}

// Define class that will hold our data. You can have subobjects, and they will
// be populated via indirect resource IDs (RIDs). Oh, and cyclic references
// are supported too!
class Book
{
    public string title;
    public string author;
}

// Create actual client
using (var client = new Resgate.Client(settings))
{
    // Get some basic data (non-subscribed version)
    // Important: this will block waiting for reconnection if currently disconnected
    List<Book> data = await client.GetCollection<Book>("library.books");
    Book book = await client.GetModel<Book>("library.book.1");
    
    
    // Now for the wunderwaffe - collection (or model) that will automatically update
    // if server updates the resources. Definition for Initial, Addded, Changed, Removed below
    TokenCollection token1 = await client.SubscribeCollection<Book>
                           ("library.books", Initial, Added, Changed, Removed)
    
    // as long as token remains alive, the four methods can be called
    // (Initial is guaranteed, others may follow)
    
    // Do some stuff...
    token1.Dispose(); // Stop listening for collections
    
    
    
    // Now for the models themselves. For reference I will show how to pass custom args:
    TokenModel token2 = await client.SubscribeModel<Book>(
                  "library.book.1", InitialModel, book => { ChangedModel(1, book); });
    
    // Do some stuff...
    token2.Dispose(); // Stop listening for model changes
    
    
    // Let us call a server command (for example adding new resource).
    // Note that Resgate allows call to return subscribed resource. Therefore strong care must be taken
    // (with Back-End guy) to get the correct call type.
    
    // First, simple call that will ignore the result
    await client.Call("library.books", "new", new Book { title = "Earthsea", author = "Ursula LeGuin" });
    
    // Next call, that can actually subscribe result as model (for example you expect it will return created object)
    TokenModel token3 = await client.CallForModel<Book>("library.books", "add",
         new Book { title = "Earthsea", author = "Ursula LeGuin" } InitialModel, book => { ChangedModel(1, book); });
        
    // And analogous for collections:
    TokenCollection token4 = await client.CallForCollection<Book>("library.books", "get_some",
                           new[] { "param", "foobar" }, Initial, Added, Changed, Removed);
                            
    // And some last ones for custom payloads:
    JToken payload = await client.CallForRawPayload("library.books", "method", new[] { "Sample" } );
    
    // The same, but payload as serialized JSON string:
    string payloadStr = await client.CallForStringPayload("library.books", "method", new[] { "Sample" } );
    
    // The same, but payload as deserialized object:
    Book payloadBook = await client.CallForPayload<Book>("library.books", "method", new[] { "Sample" } );
	
	// Auth methods contain the same variants as call methods, but beware - in case of reconnection
	// they must be called again (protocol is stateless between connections!)
	
	// Therefore you can use this trick:

	TokenReconnected tokenAuth = null;
	bool isAuthenticated = false;
	
	for (;;)
	{
		Console.WriteLine("Enter password:");
		string pwd = Console.ReadLine();
		tokenAuth?.Dispose();
		tokenAuth = await client.AuthAction(async () =>
		{
			isAuthenticated = false;
			try
			{
				await client.Auth("passwd", "login", new Auth {password = pwd});
				isAuthenticated = true;
			}
			catch (ErrorException error)
			{
				Console.WriteLine("Server returned error code: " + error.Message);
			}
		});
		if (isAuthenticated) break;
	}
}

List<Book> data;

void Initial(List<Book> books)
{
    data = books;
    Console.WriteLine("Initial state:");
    foreach (var book in books)
    {
        Console.WriteLine(book.title + " by " + book.author);
    }
}

void Added(int index, Book book)
{
    Console.WriteLine("Added: " + book.title + " by " + book.author);
    data.Insert(index, book);
}

void Changed(int index, Book book)
{
    var prev = data[index];
    Console.WriteLine("Changed: " + prev.title + " by " + prev.author + " into " + book.title + " by " +
                      book.author);
    data[index] = book;
}

void Removed(int index)
{
    var book = data[index];
    Console.WriteLine("Removed: " + book.title + " by " + book.author);
    data.RemoveAt(index);
}

void InitialModel(Book book)
{
    Console.WriteLine("Initial state of this book is: " + book.title + " by " + book.author);
}

void ChangedModel(int item, Book book)
{
    Console.WriteLine("Book " + item + " changed to: " + book.title + " by " + book.author);
}
```
