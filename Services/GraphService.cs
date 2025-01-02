using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.DirectoryObjects;
using Microsoft.Graph.Models;

namespace CustomUtility.Services;

public class GraphService : IGraphService
{
    private readonly GraphServiceClient _client;
    private readonly ILogger<GraphService> _logger;

    public GraphService(string clientId, string clientSecret, string tenantId, ILogger<GraphService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _client = new GraphServiceClient(credential);
        _logger.LogInformation("GraphService initialized with ClientId: {ClientId}, TenantId: {TenantId}", clientId, tenantId);
    }

    public async Task<(User? User, User? Manager, List<Group>? Groups)> GetUserBySamAccountNameAsync(string samAccountName, bool includeGroups = false, string? groupNameFragment = null)
    {
        _logger.LogInformation("Searching user by samAccountName: {SamAccountName}", samAccountName);

        string filter = "(OnPremisesSamAccountName eq '" + samAccountName + "')";

        var userInfo = await _client.Users.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Filter = filter;
            requestConfiguration.QueryParameters.Count = true;
            requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
        });

        if (userInfo.Value.Count > 0)
        {
            _logger.LogInformation("User found: {DisplayName}, {UPN}", userInfo.Value[0].DisplayName, userInfo.Value[0].UserPrincipalName);
            var result = await GetUserByEmailAsync(userInfo.Value[0].Mail ?? userInfo.Value[0].UserPrincipalName, includeGroups, groupNameFragment);
            return result;
        }
        else
        {
            _logger.LogWarning("No user found with samAccountName: {SamAccountName}", samAccountName);
            return (null, null, null);
        }
    }

    public async Task<(User? User, User? Manager, List<Group>? Groups)> GetUserByEmailAsync(string email, bool includeGroups = false, string? groupNameFragment = null)
    {
        _logger.LogInformation("Searching user by email: {Email}", email);
        var filterQuery = $"userPrincipalName eq '{email}'";

        try
        {
            // Retrieve user
            var result = await _client.Users
                .GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = filterQuery;
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                    "id", "displayName", "mail", "userPrincipalName", "jobTitle", "officeLocation",
                    "mobilePhone", "businessPhones", "onPremisesSamAccountName", "department"
                    };
                });

            var user = result?.Value?.FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning("No user found with email: {Email}", email);
                return (null, null, null);
            }

            _logger.LogInformation("User found: {DisplayName}, {UPN}", user.DisplayName, user.UserPrincipalName);

            // Retrieve manager
            var manager = await GetUserManagerAsync(user);

            // Retrieve groups if includeGroups is true
            List<Group>? groups = null;
            if (includeGroups)
            {
                groups = await GetUserGroupsAsync(user.Id, groupNameFragment);
            }

            return (user, manager, groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving user by email");
            throw;
        }
    }

    public async Task<(User? User, User? Manager, List<Group>? Groups)> GetUserByDisplayNameAsync(string displayName, bool includeGroups = false, string? groupNameFragment = null)
    {
        _logger.LogInformation("Searching user by displayName: {DisplayName}", displayName);
        var filterQuery = $"displayName eq '{displayName}'";

        try
        {
            // Retrieve user
            var result = await _client.Users
                .GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = filterQuery;
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                    "id", "displayName", "mail", "userPrincipalName", "jobTitle", "officeLocation",
                    "mobilePhone", "businessPhones", "onPremisesSamAccountName", "department"
                    };
                });

            var user = result?.Value?.FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning("No user found with displayName: {DisplayName}", displayName);
                return (null, null, null);
            }

            _logger.LogInformation("User found: {DisplayName}, {UPN}", user.DisplayName, user.UserPrincipalName);

            // Retrieve manager
            var manager = await GetUserManagerAsync(user);

            // Retrieve groups if includeGroups is true
            List<Group>? groups = null;
            if (includeGroups)
            {
                groups = await GetUserGroupsAsync(user.Id, groupNameFragment);
            }

            return (user, manager, groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving user by displayName");
            throw;
        }
    }

    public async Task<User?> GetUserManagerAsync(User user)
    {
        _logger.LogInformation("Fetching manager for user: {UserId}", user.Id);
        try
        {
            var managerObject = await _client.Users[user.Id].Manager.GetAsync();
            if (managerObject is User managerUser)
            {
                _logger.LogInformation("Manager found: {ManagerDisplayName}", managerUser.DisplayName);
                return managerUser;
            }
            else
            {
                _logger.LogWarning("Manager relationship exists but is not a User object.");
                return null;
            }
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError)
        {
            _logger.LogWarning("No manager relationship found for user: {UserId}. It might not be set.");
            return null; // Return null when manager relationship is not set
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving manager for user {UserId}", user.Id);
            return null; // Return null if there's any other unexpected error
        }
    }

    public async Task<List<Group>> SearchGroupsByNameAsync(string nameFragment)
    {
        _logger.LogInformation("Searching groups by name fragment: {NameFragment}", nameFragment);
        var groups = new List<Group>();
        var filterQuery = $"startswith(displayName,'{nameFragment}')";

        try
        {
            var result = await _client.Groups
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = filterQuery;
                    requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "description" };
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });

            // Use PageIterator to iterate over paginated results
            var pageIterator = PageIterator<Group, GroupCollectionResponse>
                .CreatePageIterator(
                    _client,
                    result,
                    (group) =>
                    {
                        groups.Add(group);
                        return true; // Continue iterating
                    }
                );

            await pageIterator.IterateAsync();

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching groups by name fragment");
            throw;
        }
    }



    public async Task<List<(User user, User? manager)>> GetGroupMembersAsync(string groupId)
    {
        _logger.LogInformation("Fetching members of group {GroupId}", groupId);
        var membersList = new List<(User user, User? manager)>();

        try
        {
            var memberPage = await _client.Groups[groupId].Members.GetAsync();

            while (memberPage != null && memberPage.Value.Count > 0)
            {
                foreach (var member in memberPage.Value)
                {
                    if (member is User memberUser)
                    {
                        // Re-fetch user with selected attributes
                        var fullUser = await _client.Users[memberUser.Id]
                            .GetAsync(rc =>
                            {
                                rc.QueryParameters.Select = new[]
                                {
                                    "id","displayName","mail","userPrincipalName","jobTitle","department","onPremisesSamAccountName"
                                };
                            });

                        User? manager = null;
                        if (fullUser != null)
                        {
                            manager = await GetUserManagerAsync(fullUser);
                            membersList.Add((fullUser, manager));
                        }
                    }
                }

                if (memberPage.OdataNextLink != null)
                {
                    // Manually handle paging if needed
                    var nextPageRequest = new DirectoryObjectsRequestBuilder(memberPage.OdataNextLink, _client.RequestAdapter);
                    memberPage = await nextPageRequest.GetAsync();
                }
                else
                {
                    memberPage = null;
                }
            }

            _logger.LogInformation("Found {Count} user members in group {GroupId}", membersList.Count, groupId);
            return membersList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching group members for group {GroupId}", groupId);
            throw;
        }
    }

    private async Task<List<Group>> GetUserGroupsAsync(string userId, string? nameFragment = null)
    {
        _logger.LogInformation("Fetching groups for user: {UserId}", userId);
        try
        {
            var memberOfPage = await _client.Users[userId].MemberOf.GetAsync();

            var groups = new List<Group>();
            while (memberOfPage != null && memberOfPage.Value.Count > 0)
            {
                foreach (var memberOf in memberOfPage.Value)
                {
                    if (memberOf is Group group)
                    {
                        if (string.IsNullOrEmpty(nameFragment) || group.DisplayName.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
                        {
                            groups.Add(group);
                        }
                    }
                }

                if (memberOfPage.OdataNextLink != null)
                {
                    // Manually handle paging if needed
                    var nextPageRequest = new DirectoryObjectsRequestBuilder(memberOfPage.OdataNextLink, _client.RequestAdapter);
                    memberOfPage = await nextPageRequest.GetAsync();
                }
                else
                {
                    memberOfPage = null;
                }
            }

            _logger.LogInformation("Found {Count} groups for user {UserId}", groups.Count, userId);
            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching groups for user {UserId}", userId);
            throw;
        }
    }
}