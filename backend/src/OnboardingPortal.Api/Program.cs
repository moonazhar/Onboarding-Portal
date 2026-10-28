var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var app = builder.Build();

// The full request pipeline (docs/architecture/overview.md §7) is added in the security foundation step.
app.UseExceptionHandler();

app.MapControllers();

await app.RunAsync();
