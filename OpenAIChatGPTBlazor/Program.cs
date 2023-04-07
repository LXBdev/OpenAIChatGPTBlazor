using OpenAI_API;
using OpenAIChatGPTBlazor.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddScoped<OpenAIAPI>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["OpenAI:ApiKey"];
    var deploymentId = configuration["OpenAI:DeploymentId"];
    var resourceName = configuration["OpenAI:ResourceName"];
    var client = OpenAIAPI.ForAzure(resourceName, deploymentId, apiKey);
    client.ApiVersion = "2023-03-15-preview";
    return client;
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
