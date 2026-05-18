using Fiap.SecureSystem.WebApp.Authentication;
using Fiap.SecureSystem.WebApp.Clients.ApiGateway;
using Fiap.SecureSystem.WebApp.Clients.Identity;
using Fiap.SecureSystem.WebApp.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services
    .AddOptions<ApiGatewayOptions>()
    .Bind(builder.Configuration.GetSection(ApiGatewayOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<IdentityServiceOptions>()
    .Bind(builder.Configuration.GetSection(IdentityServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiGatewayAccessTokenHandler>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.SlidingExpiration = false;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

builder.Services
    .AddHttpClient<IApiGatewayClient, ApiGatewayClient>((serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<ApiGatewayOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    })
    .AddHttpMessageHandler<ApiGatewayAccessTokenHandler>();

builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<IdentityServiceOptions>>()
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
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
