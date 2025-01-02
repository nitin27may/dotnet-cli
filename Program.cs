using CustomUtility;
using CustomUtility.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Build configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Extract Azure AD settings
        var clientId = config["AzureAd:ClientId"];
        var clientSecret = config["AzureAd:ClientSecret"];
        var tenantId = config["AzureAd:TenantId"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(tenantId))
        {
            Console.WriteLine("Azure AD credentials are not properly set in appsettings.json or environment variables.");
            return 1;
        }

        // Setup DI and Logging
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(config.GetSection("Logging"));
            loggingBuilder.AddConsole();
        });

        services.AddTransient<IGraphService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<GraphService>>();
            return new GraphService(clientId, clientSecret, tenantId, logger);
        });

        services.AddTransient<CommandHandlers>();

        var serviceProvider = services.BuildServiceProvider();
        var handlers = serviceProvider.GetRequiredService<CommandHandlers>();

        // Build commands
        var rootCommand = new RootCommand("A CLI tool to fetch user and group details from Microsoft Graph.");

        // get-user command
        var getUserCommand = new Command("get-user", "Fetch user details from Microsoft Graph");
        var networkIdOption = new Option<string?>("--networkid", "The network ID (onPremisesSamAccountName) of the user.");
        var emailOption = new Option<string?>("--email", "The email (UPN) of the user.");
        var nameOption = new Option<string?>("--name", "The full display name of the user.");

        var includeGroupOption = new Option<bool>("--includegroup", "Include groups the user belongs to.");
        var groupNameFragmentOption = new Option<string?>("--groupfragment", "Filter groups by a specific name fragment.");
        var exportOption = new Option<string?>("--export", "Path to export user details and groups to a file.");


        getUserCommand.AddOption(networkIdOption);
        getUserCommand.AddOption(emailOption);
        getUserCommand.AddOption(nameOption);
        getUserCommand.AddOption(includeGroupOption);
        getUserCommand.AddOption(groupNameFragmentOption);
        getUserCommand.AddOption(exportOption);

        getUserCommand.SetHandler(async (string? networkId, string? email, string? name, bool includeGroups, string? groupFragment, string? exportPath) =>
        {
            int provided = 0;
            if (!string.IsNullOrEmpty(networkId)) provided++;
            if (!string.IsNullOrEmpty(email)) provided++;
            if (!string.IsNullOrEmpty(name)) provided++;

            if (provided != 1)
            {
                Console.WriteLine("You must provide exactly one of --networkid, --email, or --name.");
                return;
            }

            if (!string.IsNullOrEmpty(networkId))
            {
                await handlers.HandleGetUserByNetworkIdAsync(networkId, includeGroups, groupFragment, exportPath);
            }
            else if (!string.IsNullOrEmpty(email))
            {
                await handlers.HandleGetUserByEmailAsync(email, includeGroups, groupFragment, exportPath);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                await handlers.HandleGetUserByNameAsync(name, includeGroups, groupFragment, exportPath);
            }
        }, networkIdOption, emailOption, nameOption, includeGroupOption, groupNameFragmentOption, exportOption);

        rootCommand.AddCommand(getUserCommand);

        // get-group command
        var getGroupCommand = new Command("get-group", "Interact with Groups in Microsoft Graph");

        // get-group search
        var searchCommand = new Command("search", "Search for groups by partial name");
        var groupParitalNameOption = new Option<string>("--name", "Partial or start of the group name to search") { IsRequired = true };
        searchCommand.AddOption(groupParitalNameOption);
        searchCommand.SetHandler(async (string partialName) =>
        {
            await handlers.HandleGroupSearchAsync(partialName);
        }, groupParitalNameOption);

        // get-group members
        var membersCommand = new Command("members", "List members of a given group");
        var groupNameOption = new Option<string>("--group", "The name of the group") { IsRequired = true };
        var csvOption = new Option<string?>("--csv", "Path to export members to a CSV file");
        membersCommand.AddOption(groupNameOption);
        membersCommand.AddOption(csvOption);
        membersCommand.SetHandler(async (string groupName, string? csvPath) =>
        {
            await handlers.HandleGroupMembersByNameAsync(groupName, csvPath);
        }, groupNameOption, csvOption);

        getGroupCommand.AddCommand(searchCommand);
        getGroupCommand.AddCommand(membersCommand);

        rootCommand.AddCommand(getGroupCommand);




        var httpRequestCommand = new Command("http-request", "Make an HTTP request and display the result.");
        var methodOption = new Option<string>("--method", "The HTTP method to use (GET, POST, PUT, DELETE).") { IsRequired = true };
        var urlOption = new Option<string>("--url", "The URL to send the request to.") { IsRequired = true };
        var headersOption = new Option<string[]>("--headers", "Optional headers in 'Key:Value' format.") { IsRequired = false };
        var bodyOption = new Option<string>("--body", "Optional JSON body for POST/PUT requests.") { IsRequired = false };

        httpRequestCommand.AddOption(methodOption);
        httpRequestCommand.AddOption(urlOption);
        httpRequestCommand.AddOption(headersOption);
        httpRequestCommand.AddOption(bodyOption);

        // Use HttpCommandHandler in the handler
        httpRequestCommand.SetHandler(async (string method, string url, string[]? headers, string? body) =>
        {
            var httpHandler = new HttpCommandHandler();
            await httpHandler.ExecuteHttpRequest(method, url, headers, body);
        }, methodOption, urlOption, headersOption, bodyOption);

        rootCommand.AddCommand(httpRequestCommand);


        // If no command is provided, show help
        rootCommand.SetHandler(() =>
        {
            rootCommand.InvokeAsync("--help").Wait();
        });

        return await rootCommand.InvokeAsync(args);
    }
}