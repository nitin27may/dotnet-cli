using Microsoft.Graph.Models;

namespace CustomUtility.Services;

public interface IGraphService
{
    Task<(User? User, User? Manager, List<Group>? Groups)> GetUserBySamAccountNameAsync(string samAccountName, bool includeGroups = false, string? groupNameFragment = null);
    Task<(User? User, User? Manager, List<Group>? Groups)> GetUserByEmailAsync(string email, bool includeGroups = false, string? groupNameFragment = null);
    Task<(User? User, User? Manager, List<Group>? Groups)> GetUserByDisplayNameAsync(string displayName, bool includeGroups = false, string? groupNameFragment = null);
    Task<User?> GetUserManagerAsync(User user);
    Task<List<Group>> SearchGroupsByNameAsync(string nameFragment);
    Task<List<(User user, User? manager)>> GetGroupMembersAsync(string groupId);
}
