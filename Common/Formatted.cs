using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomUtility.Common;

public static class Formatted
{
    public static void DisplayJsonIndented(object data)
    {
        var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true // Formats JSON with indentation
        });

        AnsiConsole.Write(
            new Panel(jsonString)
                .Border(BoxBorder.Rounded)
                .Header("[yellow]JSON Output[/]")
                .Expand()
        );
    }
    public static void DisplayJsonWithDynamicHeaders(string jsonString)
    {
        // Parse the JSON into a JsonObject
        var jsonNode = JsonNode.Parse(jsonString);
        if (jsonNode == null)
        {
            AnsiConsole.MarkupLine("[red]Invalid JSON.[/]");
            return;
        }

        // Flatten the JSON
        var flattenedData = new Dictionary<string, string>();
        FlattenJson(jsonNode, "", flattenedData);

        // Create the table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.LightSlateGrey)
            .Expand();

        // Add dynamic headers
        foreach (var header in flattenedData.Keys)
        {
            table.AddColumn($"[green]{header}[/]");
        }

        // Add a single row with all values
        table.AddRow(flattenedData.Values.Select(v => v ?? "null").ToArray());

        // Render the table
        AnsiConsole.Write(table);
    }
    private static void FlattenJson(JsonNode node, string prefix, IDictionary<string, string> result)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Key : $"{prefix}{property.Key}";
                FlattenJson(property.Value, key, result);
            }
        }
        else if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                var key = $"{prefix}[{i}]";
                FlattenJson(array[i], key, result);
            }
        }
        else
        {
            result[prefix] = node?.ToString() ?? "null";
        }
    }
}
