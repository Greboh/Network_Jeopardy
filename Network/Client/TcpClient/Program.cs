using System.Diagnostics;
using Tcp.Client;

Console.WriteLine("Please enter your username!");
string username = Console.ReadLine();

Console.WriteLine("Now please enter your password!!");
string password = Console.ReadLine();

Client client = new(username, password);
