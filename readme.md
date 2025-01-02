# Command-Line Tool
A .NET command-line utility for interacting with Microsoft Graph API to fetch and manage user and group details in Azure Active Directory. The idea is that using this concept, you can make any command-line utility.

## Usage Examples
Query user details by:
- Network ID (SAM Account Name)
- Email address
- Display name

List user's group memberships with optional filtering
Search for groups by name
List group members with optional CSV export
Formatted console output using Spectre.Console

## Configuration
```json
{
  "AzureAd": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret", 
    "TenantId": "your-tenant-id"
  }
}
```

## Installation

### Build and create package
```bash
dotnet pack -c Release -o ./nupkg
```

### Install globally
```bash
dotnet tool install --global --add-source ./nupkg graph
```

### Update existing installation
```bash
dotnet tool update --global --add-source ./nupkg graph
```

### Uninstall
```bash
dotnet tool uninstall --global graph
```

## Usage Examples


```bash
### Get User Details with all Groups in which the user is present
graph get-user --networkid USRNITIN --includegroup
### Get User Details with Groups which contain the character 'char'
graph get-user --networkid USRNITIN --includegroup true --groupfragment char
### Get User Details with Groups which contain the character 'char' and export the details
graph get-user --networkid USRNITIN --includegroup true --groupfragment char --export .\
### Search Group Name
graph get-group search --name MANAG
### Get all members of a group
graph get-group members --group GroupName          
### Get all members of a group and export it
graph get-group members --group GroupName --csv .\
```

```bash
### GET Request:
graph http-request --method GET --url https://jsonplaceholder.typicode.com/posts/1
### POST Request:
graph http-request --method POST --url https://jsonplaceholder.typicode.com/posts --body '{"title": "foo", "body": "bar", "userId": 1}'
### GET Request with Headers:
graph http-request --method GET --url https://api.example.com/resource --headers "Authorization:Bearer xyz" "Accept:application/json"
```


### Installation using Artifact 
```bash
dotnet nuget add source --name AzureArtifacts --username your_username --password <PAT> https://dev.azure.com/your_organization/_packaging/your_feed_name/nuget/v3/index.json

```
```bash
dotnet tool install --global --add-source https://dev.azure.com/your_organization/_packaging/your_feed_name/nuget/v3/index.json graph
```
```bash
graph --version
```

Persist Environment Variables (Optional)
1. User Environment Variables: Add them to the user’s environment:
```powershell
[System.Environment]::SetEnvironmentVariable("AzureAd__ClientId", "your-client-id", "User")
[System.Environment]::SetEnvironmentVariable("AzureAd__ClientSecret", "your-client-secret", "User")
[System.Environment]::SetEnvironmentVariable("AzureAd__TenantId", "your-tenant-id", "User")
```
2. System Environment Variables: Add them to the system environment (requires admin privileges):
```powershell
[System.Environment]::SetEnvironmentVariable("AzureAd__ClientId", "your-client-id", "Machine")
[System.Environment]::SetEnvironmentVariable("AzureAd__ClientSecret", "your-client-secret", "Machine")
[System.Environment]::SetEnvironmentVariable("AzureAd__TenantId", "your-tenant-id", "Machine")
```
## Dependencies
- .NET 9.0
- Microsoft.Graph SDK
- System.CommandLine
- Azure.Identity
- Spectre.Console

## License
MIT