using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAIChatGPTBlazor.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddScoped<OpenAIClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["OpenAI:ApiKey"];
    var deploymentId = configuration["OpenAI:DeploymentId"];
    var resourceName = configuration["OpenAI:ResourceName"];

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
