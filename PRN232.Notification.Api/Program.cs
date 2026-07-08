using PRN232.Notification.Application;
using PRN232.Notification.Infrastructure;
using PRN232.Notification.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and SignalR support
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for SignalR connection from Front-End (Vite port 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc đối với WebSockets/SignalR
    });
});

// Register clean architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GradingHub>("/gradingHub");

app.Run();
