
using Application.Interfaces;
using Application.Mappings;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Hangfire;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SmartBudgetTracker.Api.Extensions;
using SmartBudgetTracker.Api.Hubs;
using SmartBudgetTracker.Api.Middlewares;
using SmartBudgetTracker.Api.Services;
using System.Text;
using System.Threading.RateLimiting;

namespace SmartBudgetTracker.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() 
            .WriteTo.Console() 
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day) 
            .CreateLogger();

            builder.Host.UseSerilog();

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Budget Tracker API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
                
            });

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            builder.Services.Configure<JwtSettings>(jwtSettings);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Key"]))
                };
            });

            builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
            builder.Services.AddSignalR();


            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(

                        partitionKey: httpContext.Connection.RemoteIpAddress!.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100, 
                            Window = TimeSpan.FromMinutes(1) 
                        }));
            });

            //builder.Services.AddHangfire(config => config
            //        .UseSimpleAssemblyNameTypeSerializer()
            //        .UseRecommendedSerializerSettings()
            //        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));

            //builder.Services.AddHangfireServer();

            var app = builder.Build();

            //app.UseHangfireDashboard();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseResponseCompression();

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationHub");

            using (var scope = app.Services.CreateScope())
            {
                await SeedData.SeedAsync(scope.ServiceProvider);
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            //RecurringJob.AddOrUpdate<IRecurringTransactionService>(
            //    "process-due-transactions-job",      
            //    service => service.ProcessDueRecurringTransactionsAsync(),
            //    Cron.Daily());

            app.Run();
        }
    }
}
