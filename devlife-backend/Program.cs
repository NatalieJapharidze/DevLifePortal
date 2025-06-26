using DevLife.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.LoadEnvironmentVariables();
builder.Services.ConfigureApiDocumentation();
builder.Services.ConfigureDatabases(builder.Configuration);
builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureCors();

var app = builder.Build();

await app.InitializeDatabasesAsync();

app.ConfigureDocumentation();
app.UseCors("AllowAll");
app.MapEndpoints();

app.Run();