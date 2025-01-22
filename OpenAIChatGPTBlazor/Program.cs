using Azure.Identity;
using Blazored.LocalStorage;
using Microsoft.FeatureManagement;
using OpenAIChatGPTBlazor.Components;

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

// Add services to the container.
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o =>
    {
        // Increase max message size so user can sent large input as part of conversation
        o.MaximumReceiveMessageSize = 10240000;
    });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddFeatureManagement();
builder.Services.Configure<List<OpenAIOptions>>(builder.Configuration.GetSection("OpenAI"));

builder.AddAzureOpenAIClient("OpenAi");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public class AppConfigOptions
{
    public string? Endpoint { get; set; }
    public string? Label { get; set; }
}

public class OpenAIOptions
{
    public string? Hint { get; set; }
    public string? DeploymentName { get; set; }
    public string Key => $"{DeploymentName}-{Hint}";

    // Special stuff for o1
    public bool HasStreamingSupport { get; set; }

    // Special stuff for o1
    public bool HasSystemMessageSupport { get; set; } = true;

    public override string ToString() => $"{DeploymentName} ({Hint})";
}
