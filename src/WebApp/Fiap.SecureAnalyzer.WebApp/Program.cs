using Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway;
using Fiap.SecureAnalyzer.WebApp.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services
    .AddOptions<ApiGatewayOptions>()
    .Bind(builder.Configuration.GetSection(ApiGatewayOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IApiGatewayClient, ApiGatewayClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<ApiGatewayOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
