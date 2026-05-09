using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChatApplication.Client;
using ChatApplication.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatApiService>();
builder.Services.AddScoped<HubService>();
builder.Services.AddSingleton<AppState>();

await builder.Build().RunAsync();
