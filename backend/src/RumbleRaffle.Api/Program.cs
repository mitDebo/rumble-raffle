var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/hello", () => "Hoogey doogey!");

app.Run();

// Exposed so WebApplicationFactory<Program> can be used from integration tests.
public partial class Program { }
