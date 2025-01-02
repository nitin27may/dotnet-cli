using Microsoft.Graph.Models;
using Spectre.Console;
using System.Globalization;

namespace CustomUtility.Common;

public static class CsvExporter
{
    public static async Task ExportUsersAsync(string filePath, List<(User user, User? manager)> users)
    {
        using var writer = new StreamWriter(filePath);
        // Write header
        await writer.WriteLineAsync("DisplayName,NetworkID,Email,Department,JobTitle,ManagerName,ManagerEmail");

        foreach (var (user, manager) in users)
        {
            var line = string.Format(CultureInfo.InvariantCulture,
                "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"",
                user.DisplayName,
                user.OnPremisesSamAccountName ?? "",
                user.Mail ?? user.UserPrincipalName,
                user.Department ?? "",
                user.JobTitle ?? "",
                manager?.DisplayName ?? "",
                manager?.Mail ?? manager?.UserPrincipalName ?? ""
            );

            await writer.WriteLineAsync(line);
            AnsiConsole.MarkupLine($"[bold green]Exported to:[/] [blue]{filePath}[/]");
        }
    }
}