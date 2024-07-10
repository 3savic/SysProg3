using SysProg3;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

class Program
{
    static void Main(string[] args)
    {
        var baseUrl = "http://localhost";
        var port = 5000;
        var cacheCapacity = 10;
        var server = new WebServer(baseUrl, port, cacheCapacity);
        server.Launch();

        Console.WriteLine("Press Enter to stop the server...");
        Console.ReadLine();
    }
}