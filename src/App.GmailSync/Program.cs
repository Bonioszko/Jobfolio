using App.GmailSync;
using App.Application;
using App.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLocalInfrastructure(builder.Configuration);
builder.Services.AddHostedService<GmailSyncWorker>();

var host = builder.Build();
await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<IApplicationDatabaseInitializer>()
        .InitializeAsync(CancellationToken.None);
}
host.Run();
