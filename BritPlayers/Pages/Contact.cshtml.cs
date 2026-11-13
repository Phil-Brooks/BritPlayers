using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ContactModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;

    public ContactModel(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config;
    }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string Message { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostSubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Message))
        {
            return Content("<div class='alert alert-danger'>Subject and message are required.</div>", "text/html");
        }

        var token = _config["GitHub:Token"];
        var owner = _config["GitHub:Owner"] ?? "Phil-Brooks";
        var repo = _config["GitHub:Repo"] ?? "BritPlayers";

        var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BritPlayersApp", "1.0"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            title = $"[Contact] {Subject}",
            body = $"**From:** {Name}\n\n**Message:**\n{Message}",
            labels = new[] { "feedback" }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://api.github.com/repos/{owner}/{repo}/issues", content);

        if (response.IsSuccessStatusCode)
        {
            return Content(@"
                <div class='alert alert-success' role='alert'>
                    <h5 class='alert-heading'>Message Sent!</h5>
                    <p class='mb-0'>Thank you for reaching out. Your feedback has been logged to our GitHub tracker.</p>
                </div>", "text/html");
        }

        return Content("<div class='alert alert-danger'>Failed to submit issue. Please check your GitHub token permissions.</div>", "text/html");
    }
}