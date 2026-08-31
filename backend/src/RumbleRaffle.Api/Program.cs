var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/hello", () => "hello, world");

app.Run();

// Exposed so WebApplicationFactory<Program> can be used from integration tests.
public partial class Program { }
