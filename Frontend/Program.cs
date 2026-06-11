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

// Registrer en named HttpClient "GatewayApi" med base address og default headers. Denne kan så injiceres i PrivacyGatewayClient.
builder.Services.AddHttpClient("GatewayApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Registrer PrivacyGatewayClient som scoped, så den kan injiceres i Razor-komponenter. Den vil bruge den named HttpClient "GatewayApi" jeg lige har konfigureret.
builder.Services.AddScoped<PrivacyGatewayClient>();

// Build skal komme EFTER services er registreret
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// For at sikre at alle requests går over HTTPS, og for at
app.UseHttpsRedirection();

// Antiforgery middleware beskytter mod CSRF-angreb ved at validere tokens i state-changing requests (POST, PUT, DELETE).
app.UseAntiforgery();

// Static assets (CSS, JS, images) serveres fra wwwroot mappen. MapStaticAssets() er en helper der konfigurerer dette.
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();