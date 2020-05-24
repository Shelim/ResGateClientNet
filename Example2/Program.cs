using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Resgate;
using Resgate.Resgate;
using Resgate.Utility;

namespace Example2
{
    class Program
    {
        class Auth
        {
            public string password;
        }
        class Model
        {
            public int seconds;
        }


        static async Task Main(string[] args)
        {
            var settings = new Resgate.Settings(() => new Uri("ws://localhost:8080"));
            settings.Failed += (o, ev) =>
            {
                Console.WriteLine("Failed to connect due to " + ev.Reason.ToString());
            };
            settings.Error += (sender, eventArgs) =>
            {
                Console.WriteLine("Failed to subscribe " + eventArgs.Rid + " because of " +
                                  eventArgs.Error.Message);
            };

            TokenReconnected tokenAuth = null;
            bool isAuthenticated = false;

            using (var client = new Resgate.Client(settings))
            {
                for (;;)
                {

                    for (;;)
                    {
                        Console.WriteLine("Enter password ('secret'):");
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


                    Console.WriteLine("Login successful. Press any key to logout");

                    using (var token =
                        await client.SubscribeModel<Model>("ticker.model", Initial, Changed))
                    {
                        Console.ReadKey(true);
                        tokenAuth?.Dispose();
                        tokenAuth = null;
                        isAuthenticated = false;
                        await client.Auth("passwd", "logout", null);
                        Console.WriteLine(
                            "Logout successful. You should stop receiving signals. Press any key to re-login");
                        Console.ReadKey(true);
                    }
                }
            }
        }


        private static void Initial(Model obj)
        {
            Console.WriteLine("Server returned: " + obj.seconds + "s since start");
        }
        private static void Changed(Model obj)
        {
            Console.WriteLine("Server returned: " + obj.seconds + "s since start");
        }
    }
}
