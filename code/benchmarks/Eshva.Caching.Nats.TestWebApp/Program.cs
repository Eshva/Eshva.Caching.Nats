using Eshva.Caching.Nats.TestWebApp.Bootstrapping;

var builder = WebApplication.CreateBuilder(args);
builder.AddConfiguration();
builder.AddServices();

var app = builder.Build();
app.MapEndpoints();

app.Run();
