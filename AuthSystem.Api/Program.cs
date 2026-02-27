using AuthSystem.Api.Application.DTOs.Common;
using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Controllers;
using AuthSystem.Api.Infrastructure.Middlewares;
using AuthSystem.Api.Infrastructure.Persistence;
using AuthSystem.Api.Infrastructure.Security;
using AuthSystem.Api.Infrastructure.Services;
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

            // AddHostedService<T> registers a background service with ASP.NET Core.
            // - The service runs automatically when the app starts.
            // - It executes tasks in the background (e.g., cleanup jobs).
            // - PasswordResetTokenCleanupService deletes expired/used reset tokens.
            // - RefreshTokenCleanupService deletes or revokes old refresh tokens.
            // This keeps the database clean and improves security.

            builder.Services.AddHostedService<PasswordResetTokenCleanupService>();
            builder.Services.AddHostedService<RefreshTokenCleanupService>();

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddHostedService<RefreshTokenCleanupService>();

            builder.Services.AddScoped<ITokenService, JwtTokenService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AuthController>();

            //// Swagger
            //builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "AuthSystem API", Version = "v1" });

                //  ⁄—Ì› ‰Ÿ«„ «·Õ„«Ì… (JWT) œ«Œ· Swagger
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "√œŒ· «· Êﬂ‰ «·–Ì Õ’·  ⁄·ÌÂ „‰ ⁄„·Ì… «·‹ Login „»«‘—…."
                });

                // Ã⁄· Swagger Ì—”· «· Êﬂ‰ „⁄ ﬂ· ÿ·» »‘ﬂ·  ·ﬁ«∆Ì ⁄‰œ  ›⁄Ì·Â
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



            //builder.Services.AddAuthentication().AddCookie(options =>
            //{
            //    options.Cookie.HttpOnly = true;
            //    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            //    options.Cookie.SameSite = SameSiteMode.Strict;
            //});

            //builder.Services.AddAuthentication().AddCookie();a
            //builder.Services.AddAuthorization();a
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

            app.UseAuthentication(); 
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
