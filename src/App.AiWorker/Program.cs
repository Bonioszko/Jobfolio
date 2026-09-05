using App.AiWorker;
using App.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLocalInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
