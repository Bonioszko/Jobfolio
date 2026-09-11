using App.Application;
using App.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPersistenceInfrastructure(builder.Configuration);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
await scope.ServiceProvider
    .GetRequiredService<IApplicationDatabaseInitializer>()
    .InitializeAsync(CancellationToken.None);
