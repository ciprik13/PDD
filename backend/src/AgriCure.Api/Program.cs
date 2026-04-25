var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "AgriCure API up");

app.Run();

/// <summary>
/// Public partial Program declaration so WebApplicationFactory&lt;Program&gt; can target it
/// from integration tests.
/// </summary>
public partial class Program;
