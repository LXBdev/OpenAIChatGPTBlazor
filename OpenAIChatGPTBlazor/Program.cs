using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAIChatGPTBlazor.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureAppConfiguration(options =>
{
    var settings = options.Build();
    string? endpoint = settings["AppConfig:Endpoint"];
    if (!string.IsNullOrEmpty(endpoint))
    {
        options.AddAzureAppConfiguration(o =>
        {
            o.Connect(new Uri(endpoint), new DefaultAzureCredential());
        });
    }
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddScoped<OpenAIClient>(sp =>
{
    var configuration = sp.GetRequiredService<IOptionsSnapshot<OpenAIOptions>>().Value;
    var apiKey = configuration.ApiKey;
    var deploymentId = configuration.DeploymentId;
    var resourceName = configuration.ResourceName;

    if (!string.IsNullOrEmpty(apiKey))
    {

        var client = new OpenAIClient(
            new Uri($"https://{resourceName}.openai.azure.com/"),
            new AzureKeyCredential(apiKey));
        return client;
    }
    else
    {
        var client = new OpenAIClient(
            new Uri($"https://{resourceName}.openai.azure.com/"),
            new DefaultAzureCredential());
        return client;
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public class OpenAIOptions
{
    public string? ApiKey { get; set; }
    public string? DeploymentId { get; set; }
    public string? ResourceName { get; set; }
}