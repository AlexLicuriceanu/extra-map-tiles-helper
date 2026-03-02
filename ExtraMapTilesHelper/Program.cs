using System;
using System.IO;
using ExtraMapTilesHelper.Backend;
using Photino.NET;

namespace ExtraMapTilesHelper;

class Program
{
    // THIS IS THE MAGIC LINE. It allows WebView2 to render.
    [STAThread]
    static void Main(string[] args)
    {
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var indexPath = Path.Combine(wwwroot, "index.html");

        // 1. Defensive check: Let's make sure the csproj actually copied your files
        if (!File.Exists(indexPath))
        {
            // If you see this in your console, your csproj isn't copying the wwwroot folder properly.
            Console.WriteLine($"CRITICAL ERROR: Could not find UI file at {indexPath}");
            Console.ReadLine(); // Pause so you can read the error
            return;
        }

        var window = new PhotinoWindow()
            .SetTitle("Extra Map Tiles Helper")
            .SetSize(1280, 800)
            .Center()
            .SetLogVerbosity(0);

        var router = new MessageRouter(window);

        window.RegisterWebMessageReceivedHandler(router.HandleMessage);

        // 2. Wrap the path in a Uri. This prevents WebView2 from getting confused by raw Windows paths (C:\...)
        window.Load(new Uri(indexPath));

        window.WaitForClose();
    }
}