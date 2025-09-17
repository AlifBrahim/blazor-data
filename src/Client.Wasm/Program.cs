using Client.Wasm;
using Client.Wasm.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new ApiClient(sp.GetRequiredService<IJSRuntime>(), apiBase));
builder.Services.AddScoped<IndexedDbOutbox>();
builder.Services.AddScoped<DeviceInfoService>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<SyncService>();

await builder.Build().RunAsync();
