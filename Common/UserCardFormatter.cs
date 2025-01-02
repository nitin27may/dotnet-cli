using Microsoft.Graph.Models;
using Spectre.Console;
using System.Text;

namespace CustomUtility.Common;

public static class UserCardFormatter
{
    public static string Format(User user)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------");
        sb.AppendLine("                   USER DETAILS                  ");
        sb.AppendLine("-------------------------------------------------");
        sb.AppendLine($"Name:        {user.DisplayName}");
        sb.AppendLine($"Network ID:  {user.OnPremisesSamAccountName ?? "N/A"}");
        sb.AppendLine($"Email:       {user.Mail ?? user.UserPrincipalName}");
        sb.AppendLine($"Job Title:   {user.JobTitle ?? "N/A"}");
        sb.AppendLine($"Office:      {user.OfficeLocation ?? "N/A"}");
        sb.AppendLine($"Mobile:      {user.MobilePhone ?? "N/A"}");

        if (user.BusinessPhones != null && user.BusinessPhones.Any())
        {
            sb.AppendLine("Business Phones:");
            foreach (var phone in user.BusinessPhones)
            {
                sb.AppendLine($" - {phone}");
            }
        }
        else
        {
            sb.AppendLine("Business Phones: N/A");
        }

        sb.AppendLine($"Object ID:   {user.Id}");
        sb.AppendLine("-------------------------------------------------");

        return sb.ToString();
    }

    public static void PrintUserWithGroups(User? user, User? manager, List<Group>? groups, string? exportPath)
    {
        if (user == null)
        {
            AnsiConsole.MarkupLine("[bold red]User not found.[/]");
            return;
        }

        var userDetails = $@"
        Name:          {user.DisplayName ?? "N/A"}
        Network ID:    {user.OnPremisesSamAccountName ?? "N/A"}
        Email:         {user.Mail ?? user.UserPrincipalName ?? "N/A"}
        Department:    {user.Department ?? "N/A"}
        Job Title:     {user.JobTitle ?? "N/A"}
        Manager:       {manager?.DisplayName ?? "N/A"}";

        // Build the content for the card
        var cardContent = new Markup($@"
            [bold yellow]Name:[/]          [green]{user.DisplayName ?? "N/A"}[/]
            [bold yellow]Network ID:[/]    [green]{user.OnPremisesSamAccountName ?? "N/A"}[/]
            [bold yellow]Email:[/]         [green]{user.Mail ?? user.UserPrincipalName ?? "N/A"}[/]
            [bold yellow]Department:[/]    [green]{user.Department ?? "N/A"}[/]
            [bold yellow]Job Title:[/]     [green]{user.JobTitle ?? "N/A"}[/]
            [bold yellow]Manager:[/]       [green]{manager?.DisplayName ?? "N/A"}[/]");

        // Create a panel with the card content
        var card = new Panel(cardContent)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.LightSlateGrey)
            .Header("[bold yellow]User Details[/]")
            .Expand();

        // Render the user details card
        AnsiConsole.Write(card);
        string groupDetails = string.Empty;
        // If there are groups, display them
        if (groups != null && groups.Count > 0)
        {
            var groupRows = groups.Select(group =>
           $"- {group.DisplayName ?? "N/A"}: {group.Description ?? "N/A"}").ToList();
            groupDetails = "User Groups:\n" + string.Join("\n", groupRows);
            // Render a table for groups
            var table = new Table()
                .AddColumn("[bold yellow]Group Name[/]")
                .AddColumn("[bold yellow]Group ID[/]")
                .AddColumn("[bold yellow]Group Description[/]")
                .Border(TableBorder.Rounded);

            foreach (var group in groups)
            {
                table.AddRow(
                    group.DisplayName ?? "N/A",
                    group.Id ?? "N/A",
                    group.Description ?? "N/A"
                );
            }

            // Add a panel for groups
            var groupPanel = new Panel(table)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.LightSlateGrey)
                .Header("[bold yellow]User Groups[/]")
                .Expand();

            AnsiConsole.Write(groupPanel);
        }
        else
        {
            // If no groups are found
            AnsiConsole.MarkupLine("[bold red]No groups found for this user.[/]");
        }
        if (!string.IsNullOrEmpty(exportPath))
        {
            try
            {
                var fullPath = Path.Combine(exportPath.TrimEnd(Path.DirectorySeparatorChar), $"{user.OnPremisesSamAccountName}.txt");
                var exportContent = $"User Details:\n{userDetails}\n\n{groupDetails}";
                File.WriteAllText(fullPath, exportContent);

                AnsiConsole.MarkupLine($"[bold green]Exported card to:[/] [blue]{fullPath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Failed to export card:[/] {ex.Message}");
            }
        }
    }


    public static void PrintMembersAsTable(List<(User user, User? manager)> members, string groupName)
    {
        var table = new Table()
            .Centered() // Center the table in the console
            .Border(TableBorder.Rounded) // Set a rounded border
            .BorderColor(Color.LightSlateGrey) // Set border color
            .Title("[yellow]Team Members[/]") // Add a table title with color
            .Caption("[grey]Showing team member details[/]"); // Add a caption

        // Add columns with styles
        table.AddColumn("[green]Name[/]");
        table.AddColumn(new TableColumn("[blue]NetworkId[/]").Centered()); // Center-align the Age column
        table.AddColumn("[green]Email[/]");
        table.AddColumn("[green]Dept[/]");
        table.AddColumn("[green]Job Title[/]"); // Center-align the Age column
        table.AddColumn("[purple]Manager[/]");

        foreach (var (user, manager) in members)
        {
            table.AddRow(
                string.IsNullOrWhiteSpace(user.DisplayName) ? "" : user.DisplayName,
                string.IsNullOrWhiteSpace(user.OnPremisesSamAccountName) ? "" : user.OnPremisesSamAccountName,
                string.IsNullOrWhiteSpace(user.Mail ?? user.UserPrincipalName) ? "" : (user.Mail ?? user.UserPrincipalName),
                string.IsNullOrWhiteSpace(user.Department) ? "" : user.Department,
                string.IsNullOrWhiteSpace(user.JobTitle) ? "" : user.JobTitle,
                string.IsNullOrWhiteSpace(manager?.DisplayName) ? "" : manager?.DisplayName
            );
        }

        // Render the table to the console
        AnsiConsole.Write(table);
    }

    public static void PrintGroups(List<Group>? groups)
    {
        if (groups == null || groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[bold red]No groups found.[/]");
            return;
        }

        // Render a table for groups
        var table = new Table()
            .AddColumn("[bold yellow]Group Name[/]")
            .AddColumn("[bold yellow]Group Description[/]")
            .Border(TableBorder.Rounded);

        foreach (var group in groups)
        {
            table.AddRow(
                group.DisplayName ?? "N/A",
                group.Description ?? "N/A"
            );
        }

        // Add a panel for groups
        var groupPanel = new Panel(table)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.LightSlateGrey)
            .Header("[bold yellow]User Groups[/]")
            .Expand();

        // Render the groups panel
        AnsiConsole.Write(groupPanel);
    }

}