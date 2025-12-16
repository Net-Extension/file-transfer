using FileTransfer.Factory;
using FileTransfer.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add FileTransfer library with HTTP provider
builder.Services.AddFileTransfer()
    .AddHttpProvider();

// Add health checks
builder.Services.AddHealthChecks()
    .AddFileTransferHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
