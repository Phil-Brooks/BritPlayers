using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ContactModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public ContactModel(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string Category { get; set; } = string.Empty;
    [BindProperty] public string Title { get; set; } = string.Empty;
    [BindProperty] public string Message { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
        {
            return Content("<div class='alert alert-danger'>Please fill in all required fields.</div>");
        }

        // Configure these in appsettings.json or User Secrets
        var githubToken = _config["GitHub:Token"]; // Fine-grained Personal Access Token with "Issues: Write" permission
        var repoOwner = "YOUR_GITHUB_USERNAME";
        var repoName = "YOUR_REPO";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BritPlayersApp", "1.0"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine($"**Category:** {Category}");
        bodyBuilder.AppendLine($"**Submitted By:** {(string.IsNullOrWhiteSpace(Name) ? "Anonymous" : Name)}");
        bodyBuilder.AppendLine("\n---\n");
        bodyBuilder.AppendLine(Message);

        var payload = new
        {
            title = $"[{Category}] {Title}",
            body = bodyBuilder.ToString(),
            labels = new[] { "feedback", Category.ToLowerInvariant() }
        };

        var json = JsonSerializer.Serialize(payload);
        var response = await client.PostAsync(
            $"https://api.github.com/repos/{repoOwner}/{repoName}/issues",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        if (response.IsSuccessStatusCode)
        {
            // Returns HTMX fragment confirming submission
            return Content(@"
                <div class='alert alert-success p-4'>
                    <h5 class='alert-heading'><i class='bi bi-check-circle me-2'></i>Message Received!</h5>
                    <p class='mb-0'>Thank you for your feedback. An issue has been created in the repository for review.</p>
                </div>");
        }

        return Content(@"
            <div class='alert alert-danger'>
                Failed to deliver message to GitHub. Please try again or open an issue directly.
            </div>");
    }
}