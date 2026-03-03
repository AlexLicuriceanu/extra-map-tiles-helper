using Photino.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExtraMapTilesHelper.backend
{
    public class MessageRouter
    {
        private readonly PhotinoWindow _window;

        // Configured to match JavaScript's camelCase naming
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public MessageRouter(PhotinoWindow window)
        {
            _window = window;
        }

        public void HandleMessage(object? sender, string rawMessage)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawMessage);
                var type = doc.RootElement.GetProperty("type").GetString();

                // The Traffic Cop
                switch (type)
                {
                    case "ping":
                        Send("pong", new { message = "Backend is alive and ready!" });
                        break;

                    case "open_file_dialog":
                        // 1. Open native Windows file picker
                        var selectedFiles = _window.ShowOpenFile(
                            title: "Select a YTD file",
                            multiSelect: false,
                            filters: new[] { ("YTD Map Files", new[] { "ytd" }) }
                        );

                        if (selectedFiles != null && selectedFiles.Length > 0)
                        {
                            string filePath = selectedFiles[0];

                            // 2. Run extraction on a background thread so UI doesn't freeze
                            Task.Run(() =>
                            {
                                try
                                {
                                    var service = new CodeWalkerService();
                                    var result = service.ExtractYtd(filePath);

                                    // 3. Send results back to UI
                                    Send("ytd_loaded", result);
                                }
                                catch (Exception ex)
                                {
                                    Send("error", new { message = $"Extraction failed: {ex.Message}" });
                                }
                            });
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown message type: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Router Error: {ex.Message}");
            }
        }

        // Helper to format and send JSON back to the UI
        public void Send(string type, object payload)
        {
            var message = JsonSerializer.Serialize(new { type, payload }, JsonOptions);
            _window.SendWebMessage(message);
        }
    }
}
