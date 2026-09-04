using App.AiWorker;
using App.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddLocalInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
