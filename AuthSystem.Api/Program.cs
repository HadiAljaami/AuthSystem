using AuthSystem.Api.Application.DTOs.Common;
using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Controllers;
using AuthSystem.Api.Infrastructure.Middlewares;
using AuthSystem.Api.Infrastructure.Persistence;
using AuthSystem.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    var response = ApiResponse<object>.FailureResponse(
                        "VALIDATION_ERROR",
                        "»Ì«‰«  «·ÿ·» €Ì— ’ÕÌÕ…",
                        errors
                    );

                    return new BadRequestObjectResult(response);
                };
            });


            // Add services to the container.
            builder.Services.AddControllers();


            builder.Services.AddScoped<ITokenService, JwtTokenService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AuthController>();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            app.UseMiddleware<GlobalExceptionMiddleware>();

            // Seed database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbSeeder.Seed(context);
            }

            // Swagger UI
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
