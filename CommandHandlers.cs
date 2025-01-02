using CustomUtility.Common;
using CustomUtility.Services;
using Microsoft.Extensions.Logging;

namespace CustomUtility;

public class CommandHandlers
{
    private readonly IGraphService _graphService;
    private readonly ILogger<CommandHandlers> _logger;

    public CommandHandlers(IGraphService graphService, ILogger<CommandHandlers> logger)
    {
        _graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleGetUserByNetworkIdAsync(string networkId, bool includeGroups = false, string? groupFragment = null, string? exportPath = null)
    {
        _logger.LogInformation("HandleGetUserByNetworkIdAsync called with {NetworkId}, IncludeGroups: {IncludeGroups}, GroupFragment: {GroupFragment}", networkId, includeGroups, groupFragment);

        var user = await _graphService.GetUserBySamAccountNameAsync(networkId, includeGroups, groupFragment);
        UserCardFormatter.PrintUserWithGroups(user.User, user.Manager, user.Groups, exportPath);
    }

    public async Task HandleGetUserByEmailAsync(string email, bool includeGroups = false, string? groupFragment = null, string? exportPath = null)
    {
        _logger.LogInformation("HandleGetUserByEmailAsync called with {Email}, IncludeGroups: {IncludeGroups}, GroupFragment: {GroupFragment}", email, includeGroups, groupFragment);

        var user = await _graphService.GetUserByEmailAsync(email, includeGroups, groupFragment);
        UserCardFormatter.PrintUserWithGroups(user.User, user.Manager, user.Groups, exportPath);
    }

    public async Task HandleGetUserByNameAsync(string name, bool includeGroups = false, string? groupFragment = null, string? exportPath = null)
    {
        _logger.LogInformation("HandleGetUserByNameAsync called with {Name}, IncludeGroups: {IncludeGroups}, GroupFragment: {GroupFragment}", name, includeGroups, groupFragment);

        var user = await _graphService.GetUserByDisplayNameAsync(name, includeGroups, groupFragment);
        UserCardFormatter.PrintUserWithGroups(user.User, user.Manager, user.Groups, exportPath);
    }

    public async Task HandleGroupSearchAsync(string partialName)
    {
        _logger.LogInformation("HandleGroupSearchAsync called with {PartialName}", partialName);
        var groups = await _graphService.SearchGroupsByNameAsync(partialName);

        if (groups.Count == 0)
        {
            _logger.LogInformation("No groups found.");
            return;
        }

        UserCardFormatter.PrintGroups(groups);
    }

    public async Task HandleGroupMembersByNameAsync(string groupName, string? csvPath)
    {
        _logger.LogInformation("HandleGroupMembersAsync called with {GroupName}, CSV: {CsvPath}", groupName, csvPath ?? "None");
        var groups = await _graphService.SearchGroupsByNameAsync(groupName);

        if (groups.Count == 0)
        {
            _logger.LogInformation($"No groups found with name starting with '{groupName}'.");
            return;
        }

        var group = groups.First();
        _logger.LogInformation($"Found group: {group.DisplayName} (ID: {group.Id}). Fetching members...");

        var members = await _graphService.GetGroupMembersAsync(group.Id);
        if (members.Count == 0)
        {
            _logger.LogInformation("No members found in this group.");
            return;
        }

        _logger.LogInformation($"Members of Group {group.DisplayName}:");
        foreach (var (user, manager) in members)
        {
            _logger.LogInformation($"{user.DisplayName} ({user.OnPremisesSamAccountName}) - {user.Mail ?? user.UserPrincipalName} | Dept: {user.Department ?? "N/A"} | Title: {user.JobTitle ?? "N/A"} | Manager: {manager?.DisplayName ?? "N/A"}");
        }

        if (!string.IsNullOrEmpty(csvPath))
        {
            var fullPath = Path.Combine(csvPath.TrimEnd(Path.DirectorySeparatorChar), $"{groupName}.csv");
            _logger.LogInformation($"Exporting {members.Count} members to CSV at: {fullPath}");
            await CsvExporter.ExportUsersAsync(fullPath, members);
            _logger.LogInformation($"CSV Exported: {fullPath}");
        }
        else
        {
            UserCardFormatter.PrintMembersAsTable(members, group.DisplayName);
        }
    }

    public async Task HandleUserGroupsAsync(string email, string? groupFragment)
    {
        _logger.LogInformation("HandleUserGroupsAsync called with {Email}, GroupFragment: {GroupFragment}", email, groupFragment);

        var (user, _, groups) = await _graphService.GetUserByEmailAsync(email, includeGroups: true, groupFragment);

        if (user == null)
        {
            _logger.LogInformation("No user found with email: {Email}", email);
            return;
        }

        Console.WriteLine($"Groups for user {user.DisplayName} ({user.Mail}):");
        if (groups == null || groups.Count == 0)
        {
            Console.WriteLine("No groups found.");
            return;
        }

        UserCardFormatter.PrintGroups(groups);
    }
}
