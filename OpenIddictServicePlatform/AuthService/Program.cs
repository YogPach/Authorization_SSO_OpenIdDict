using AuthService.Data;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews(); // API + MVC Views
//builder.Services.AddRazorPages();
// Configure EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict(); // Required for OpenIddict EF Core entities
});

// Configure OpenIddict: server + validation
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AppDbContext>();
    })
    .AddServer(serverOptions =>
    {
        //serverOptions.AllowPasswordFlow();
        serverOptions
        .SetAuthorizationEndpointUris("/connect/authorize")
                .SetEndSessionEndpointUris("/connect/logout")
                .SetTokenEndpointUris("/connect/token")
                .SetUserInfoEndpointUris("/connect/userinfo");

        serverOptions.RegisterScopes(
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
        OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
        OpenIddict.Abstractions.OpenIddictConstants.Scopes.Roles
            );
        //serverOptions.AllowClientCredentialsFlow();
        serverOptions.AllowAuthorizationCodeFlow();

        // Register custom scope
       // serverOptions.RegisterScopes("api.read");
        serverOptions.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        serverOptions.AddDevelopmentEncryptionCertificate()
                     .AddDevelopmentSigningCertificate();

        serverOptions.UseAspNetCore()
                     .EnableAuthorizationEndpointPassthrough()
                     .EnableEndSessionEndpointPassthrough()
                     .EnableTokenEndpointPassthrough();
    })
    .AddValidation(validationOptions =>
    {
        // Validate tokens issued by the local server
        validationOptions.UseLocalServer();
        validationOptions.UseAspNetCore();
    });


builder.Services.AddAuthorization();

// Hosted service to seed client
builder.Services.AddHostedService<Worker>();
builder.Services.AddTransient<AuthorizationService>();
builder.Services.AddTransient<ClientsSeeder>();
builder.Services.AddScoped<UsersSeeder>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(c =>
    {
        c.LoginPath = "/Authenticate";
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:44334")
            .AllowAnyHeader();

        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ClientsSeeder>();
    seeder.AddClients().GetAwaiter().GetResult();
    seeder.AddScopes().GetAwaiter().GetResult();
    var userSeeder = scope.ServiceProvider.GetRequiredService<UsersSeeder>();
    await userSeeder.SeedUsers();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Middleware
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//app.MapRazorPages();
app.Run();
