using FileTransfer.Factory;
using WorkerSample;

var builder = Host.CreateApplicationBuilder(args);

// Add FileTransfer library with all providers
builder.Services.AddFileTransfer()
    .AddAllProviders();

// Add the background worker
builder.Services.AddHostedService<FileTransferWorker>();

var host = builder.Build();
host.Run();
