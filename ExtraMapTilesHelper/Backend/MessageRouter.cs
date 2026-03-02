using System.Text.Json;
using Photino.NET;

namespace ExtraMapTilesHelper.Backend;

public sealed class MessageRouter(PhotinoWindow window)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void HandleMessage(object? sender, string rawMessage)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawMessage);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "ping":
                    Send("pong", new { message = "Photino backend is alive!" });
                    break;

                default:
                    Send("error", new { message = $"Unknown message type: {type}" });
                    break;
            }
        }
        catch (Exception ex)
        {
            Send("error", new { message = ex.Message });
        }
    }

    public void Send(string type, object payload)
    {
        var message = JsonSerializer.Serialize(new { type, payload }, JsonOptions);
        window.SendWebMessage(message);
    }
}