using AuthService.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services
{
    public class ClientsSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientsSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task AddScopes()
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            var apiScope = await manager.FindByNameAsync("api1");

            if (apiScope != null)
            {
                await manager.DeleteAsync(apiScope);
            }

            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                DisplayName = "Api scope",
                Name = "api1",
                Resources =
                {
                    "resource_server_1"
                }
            });
        }

        //public async Task AddClients()
        //{
        //    await using var scope = _serviceProvider.CreateAsyncScope();

        //    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //    await context.Database.EnsureCreatedAsync();

        //    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        //    var client = await manager.FindByClientIdAsync("web-client-test2");
        //    if (client != null)
        //    {
        //        await manager.DeleteAsync(client);
        //    }

        //    await manager.CreateAsync(new OpenIddictApplicationDescriptor
        //    {
        //        ClientId = "web-client-test2",
        //        ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
        //        ConsentType = ConsentTypes.Explicit,
        //        DisplayName = "Swagger client application",
        //        //RedirectUris =
        //        //{
        //        //    new Uri("https://localhost:44397/swagger/oauth2-redirect.html"),
        //        //    new Uri("https://localhost:44334/swagger/oauth2-redirect.html"),

        //        //},
        //        // Update RedirectUris in ClientsSeeder.cs to point to API endpoints
        //        RedirectUris =
        //        {
        //            new Uri("https://localhost:44397/api/auth/callback"), // Your API callback endpoint
        //            new Uri("https://localhost:44334/api/auth/callback"),
        //        },
        //        PostLogoutRedirectUris =
        //        {
        //            new Uri("https://localhost:44397/resources"),
        //            new Uri("https://localhost:7043/resources"),
        //            new Uri("http://localhost:5144/resources")
        //        },
        //        Permissions =
        //        {
        //            Permissions.Endpoints.Authorization,
        //            //Permissions.Endpoints.Logout,
        //            Permissions.Endpoints.Token,
        //            Permissions.GrantTypes.AuthorizationCode,
        //            Permissions.ResponseTypes.Code,
        //            Permissions.Scopes.Email,
        //            Permissions.Scopes.Profile,
        //            Permissions.Scopes.Roles,
        //           $"{Permissions.Prefixes.Scope}api1"
        //        },
        //        //Requirements =
        //        //{
        //        //    Requirements.Features.ProofKeyForCodeExchange
        //        //}
        //    });

        //    //var reactClient = await manager.FindByClientIdAsync("react-client");
        //    //if (reactClient != null)
        //    //{
        //    //    await manager.DeleteAsync(reactClient);
        //    //}

        //    //await manager.CreateAsync(new OpenIddictApplicationDescriptor
        //    //{
        //    //    ClientId = "react-client",
        //    //    ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
        //    //    ConsentType = ConsentTypes.Explicit,
        //    //    DisplayName = "React client application",
        //    //    RedirectUris =
        //    //    {
        //    //        new Uri("http://localhost:3000/oauth/callback")
        //    //    },
        //    //    PostLogoutRedirectUris =
        //    //    {
        //    //        new Uri("http://localhost:5127/")
        //    //    },
        //    //    Permissions =
        //    //    {
        //    //        Permissions.Endpoints.Authorization,
        //    //        Permissions.Endpoints.EndSession,
        //    //        Permissions.Endpoints.Token,
        //    //        Permissions.GrantTypes.AuthorizationCode,
        //    //        Permissions.ResponseTypes.Code,
        //    //        Permissions.Scopes.Email,
        //    //        Permissions.Scopes.Profile,
        //    //        Permissions.Scopes.Roles,
        //    //       $"{Permissions.Prefixes.Scope}api1"
        //    //    },
        //    //    //Requirements =
        //    //    //{
        //    //    //    Requirements.Features.ProofKeyForCodeExchange
        //    //    //}
        //    //});
        //}


        public async Task AddClients()
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            // ✅ Fetch clients from DB
            var clients = await context.Clients.ToListAsync();

            foreach (var dbClient in clients)
            {
                // If exists → delete & re-create
                var existing = await manager.FindByClientIdAsync(dbClient.ClientId);
                if (existing != null)
                {
                    await manager.DeleteAsync(existing);
                }

                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = dbClient.ClientId,
                    ClientSecret = dbClient.ClientSecret,
                    DisplayName = dbClient.DisplayName,
                    ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                    Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}api1"
            }
                };

                // ✅ Handle multiple RedirectUris (comma separated in DB)
                if (!string.IsNullOrEmpty(dbClient.RedirectUris))
                {
                    foreach (var uri in dbClient.RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        descriptor.RedirectUris.Add(new Uri(uri.Trim()));
                    }
                }

                await manager.CreateAsync(descriptor);
            }
        }

    }
}
