using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Blazored.LocalStorage;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var opt = configuration.GetSection("AppConfig").Get<AppConfigOptions>();
if (opt != null && !string.IsNullOrEmpty(opt.Endpoint))
{
    configuration.AddAzureAppConfiguration(o =>
    {
        o.Connect(new Uri(opt.Endpoint), new DefaultAzureCredential());

        o.Select("*");
        o.Select("*", opt.Label);
        o.UseFeatureFlags(of =>
        {
            of.Select("*");
            of.Select("*", opt.Label);
        });
    });
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(o =>
    {
        // Increase max message size so user can sent large input as part of conversation
        o.MaximumReceiveMessageSize = 10240000;
    });


builder.Services.AddBlazoredLocalStorage();
builder.Services.AddFeatureManagement();
builder.Services.Configure<List<OpenAIOptions>>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddScoped<IDictionary<string, OpenAIClient>>(sp =>
{
    var configurations = sp.GetRequiredService<IOptionsSnapshot<List<OpenAIOptions>>>().Value;
    var clients = new Dictionary<string, OpenAIClient>();

    foreach (var configuration in configurations)
    {
        // Skip if resourcename or deploymentname is not set
        if (string.IsNullOrEmpty(configuration.ResourceName) || string.IsNullOrEmpty(configuration.DeploymentName))
        {
            continue;
        }
        var apiKey = configuration.ApiKey;
        var resourceName = configuration.ResourceName;

        if (!string.IsNullOrEmpty(apiKey))
        {
            var client = new OpenAIClient(
                new Uri($"https://{resourceName}.openai.azure.com/"),
                new AzureKeyCredential(apiKey));
            clients[configuration.Key] = client;
        }
        else
        {
            var client = new OpenAIClient(
                new Uri($"https://{resourceName}.openai.azure.com/"),
                new DefaultAzureCredential());
            clients[configuration.Key] = client;
        }
    }
    return clients;
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

public class AppConfigOptions
{
    public string? Endpoint { get; set; }
    public string? Label { get; set; }

}

public class OpenAIOptions
{
    public string? Hint { get; set; }
    public string? ResourceName { get; set; }
    public string? DeploymentName { get; set; }
    public string? ApiKey { get; set; }
    public bool HasVision { get; set; }
    public string Key => $"{DeploymentName}-{Hint}";
    public override string ToString() => $"{DeploymentName} ({Hint})";
}