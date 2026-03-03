using ExtraMapTilesHelper.backend;
using Photino.NET;
using System;
using System.IO;

namespace ExtraMapTilesHelper;

class Program
{
    [STAThread] // Required for Windows UI to render
    static void Main(string[] args)
    {
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var indexPath = Path.Combine(wwwroot, "index.html");

        if (!File.Exists(indexPath))
        {
            Console.WriteLine($"CRITICAL ERROR: Could not find UI file at {indexPath}");
            Console.ReadLine();
            return;
        }

        var window = new PhotinoWindow()
            .SetTitle("Map Tile Editor")
            .SetSize(1024, 768)
            .Center()
            .SetLogVerbosity(0);

        var router = new MessageRouter(window);

        // Tell Photino to send all JS messages to our router
        window.RegisterWebMessageReceivedHandler(router.HandleMessage);

        window.Load(new Uri(indexPath));
        window.WaitForClose();
    }
}