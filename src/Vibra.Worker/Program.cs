using Vibra.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<IngestWorker>();

builder.Build().Run();
