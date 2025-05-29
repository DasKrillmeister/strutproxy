using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapPost("/", async context =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var parts = body.Split(' ', 2);
    var bodyPassword = parts[0];
    var bodyText = parts.Length > 1 ? parts[1] : string.Empty;

    if (bodyPassword != Environment.GetEnvironmentVariable("aiproxypassword"))
    {
        context.Response.StatusCode = 403;
        return;
    }

    var response = await SendRequestToGeminiApi(bodyText);

    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(response);


});

static async Task<string> SendRequestToGeminiApi(string bodyText)
{
    using var httpClient = new HttpClient();
    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    var requestBody = new
    {
        contents = new[]
        {
        new
        {
            parts = new[]
            {
                new { text = bodyText }
            }
        }
    }
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", content);

    if (response.IsSuccessStatusCode)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(responseBody);
        var contentNode = result.RootElement.GetProperty("candidates").EnumerateArray().First().GetProperty("content").GetProperty("parts").EnumerateArray().First().GetProperty("text");
        var text = contentNode.GetString();
        return text ?? "Eror :(";
    }
    else
    {
        return "Eror :(";
    }
}

app.Run();
