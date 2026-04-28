using Frontend.Client;
using Frontend.Components;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


// Flyt dette OP før builder.Build()
var apiBaseUrl = builder.Configuration["ApiBaseUrl:BaseUrl"]
    ?? throw new InvalidOperationException("Missing ApiBaseUrl:BaseUrl in appsettings.json");

builder.Services.AddHttpClient("GatewayApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddScoped<PrivacyGatewayClient>();

// Build skal komme EFTER services er registreret
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();