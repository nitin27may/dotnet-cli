using CustomUtility.Common;
using Spectre.Console;
using System.Text;
using System.Text.Json;

namespace CustomUtility;

public class HttpCommandHandler
{
    private readonly HttpClient _httpClient;

    public HttpCommandHandler()
    {
        _httpClient = new HttpClient();
    }

    public async Task ExecuteHttpRequest(string method, string url, string[]? headers, string? body)
    {
        try
        {
            // Add headers if provided
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    var parts = header.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        _httpClient.DefaultRequestHeaders.Add(parts[0].Trim(), parts[1].Trim());
                    }
                }
            }

            // Perform HTTP request
            HttpResponseMessage response;
            switch (method.ToUpper())
            {
                case "GET":
                    response = await _httpClient.GetAsync(url);
                    break;
                case "POST":
                    response = await _httpClient.PostAsync(url, new StringContent(body ?? "", Encoding.UTF8, "application/json"));
                    break;
                case "PUT":
                    response = await _httpClient.PutAsync(url, new StringContent(body ?? "", Encoding.UTF8, "application/json"));
                    break;
                case "DELETE":
                    response = await _httpClient.DeleteAsync(url);
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]Unsupported HTTP method: {0}[/]", method);
                    return;
            }

            // Read and display the response
            var responseContent = await response.Content.ReadAsStringAsync();
            DisplayResponse(response, responseContent);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error occurred:[/] {0}", ex.Message);
        }
    }

    private void DisplayResponse(HttpResponseMessage response, string responseContent)
    {
        if (response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine("[green]Request succeeded![/]");
            var jsonObject = JsonSerializer.Deserialize<object>(responseContent);
            if (jsonObject != null)
            {
                Formatted.DisplayJsonIndented(jsonObject);
                Formatted.DisplayJsonWithDynamicHeaders(responseContent);
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Request failed with status code:[/] [yellow]{0}[/]", response.StatusCode);
            var jsonObject = JsonSerializer.Deserialize<object>(responseContent);
            if (jsonObject != null)
            {
                Formatted.DisplayJsonIndented(jsonObject);
            }
        }
    }
}
