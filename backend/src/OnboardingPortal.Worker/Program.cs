var builder = Host.CreateApplicationBuilder(args);

// Runs as a Windows Service on servers and as a console app during development.
builder.Services.AddWindowsService(options => options.ServiceName = "OnboardingPortal.Worker");

// Jobs are registered here as they are built (docs/architecture/overview.md §8.1).

var host = builder.Build();

await host.RunAsync();
