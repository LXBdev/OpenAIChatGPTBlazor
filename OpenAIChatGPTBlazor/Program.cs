using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using OpenAIChatGPTBlazor.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureAppConfiguration(options =>
{
    var settings = options.Build();
    var opt = settings.GetSection("AppConfig").Get<AppConfigOptions>();
    if (opt != null && !string.IsNullOrEmpty(opt.Endpoint))
    {
        options.AddAzureAppConfiguration(o =>
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
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddFeatureManagement();
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddScoped<OpenAIClient>(sp =>
{
    var configuration = sp.GetRequiredService<IOptionsSnapshot<OpenAIOptions>>().Value;
    var apiKey = configuration.ApiKey;
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

public class AppConfigOptions
{
    public string? Endpoint { get; set; }
    public string? Label { get; set; }

}

public class OpenAIOptions
{
    public string? ApiKey { get; set; }
    public string? ResourceName { get; set; }
    public string? SelectableModels { get; set; }
}