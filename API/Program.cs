using API.Data;
using API.Extensions;
using API.MiddleWare;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

//namespace API
//{
//    public class Program
//    {
//        public static async Task Main(string[] args)
//        {
//            var host = CreateHostBuilder(args).Build();
//            using var scope = host.Services.CreateScope();
//            var services = scope.ServiceProvider;
//            try
//            {
//                var context = services.GetRequiredService<DataContext>();
//                await context.Database.MigrateAsync();
//                await Seed.SeedUsers(context);

//            }
//            catch (Exception ex)
//            {
//                var logger = services.GetRequiredService<ILogger<Program>>();
//                logger.LogError(ex, "An error occured during migrations");
//            }

//            await host.RunAsync();
//        }

//        public static IHostBuilder CreateHostBuilder(string[] args) =>
//            Host.CreateDefaultBuilder(args)
//                .ConfigureWebHostDefaults(webBuilder =>
//                {
//                    webBuilder.UseStartup<Startup>();
//                });
//    }
//}

//// minimum hosting model
var builder = WebApplication.CreateBuilder(args);

// services container configureservices()
builder.Services.AddApplicationService(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }

    });
});

builder.Services.AddCors();
builder.Services.AddIdentityServices(builder.Configuration);

// middleware - configure() - pipeline
var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(x => x.AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("https://localhost:4200"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

//// db intialization
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<DataContext>();
    await context.Database.MigrateAsync();
    await Seed.SeedUsers(context);

}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occured during migrations");
}

app.Run();