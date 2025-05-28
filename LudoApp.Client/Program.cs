using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components; 
using LudoApp.Client;
using LudoApp.Client.Services;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to point to your server
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5010/") 
});

builder.Services.AddSingleton<AuthService>();

builder.Services.AddSingleton<HubConnection>(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    // IMPORTANT: Make sure this URL matches your server's SignalR hub endpoint
    // It should be the absolute URL to your server's hub endpoint.
    // Assuming your server runs on http://localhost:5010
    var hubUrl = navigationManager.ToAbsoluteUri("http://localhost:5010/gamehub");

    return new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect() // Optional: Automatically try to reconnect
        .Build();
});

builder.Services.AddSingleton<GameClientService>();

await builder.Build().RunAsync();