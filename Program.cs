using DragonAPI.Context;
using DragonAPI.Server;
using DragonAPI.SignalR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Core.MySqlStringBuilder(builder);

// Configure services
APIBuildCore.BuildDatabase(builder, connectionString);
APIBuildCore.Authentication(builder);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policyBuilder =>
        {
            policyBuilder.WithOrigins("http://127.0.0.1:8000", builder.Configuration["Data:API_Node_Address"])
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddSignalR();

var app = builder.Build();

// Core initialization
new Core(app.Configuration, app.Services, connectionString);

// Database migration and data seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RuinDbContext>();
    context.Database.Migrate();
    DataBuilder.InsertData();
}

// Middleware pipeline configuration
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();

app.UseCors("AllowLocalhost");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LogHub>("/logHub");

app.Run();
