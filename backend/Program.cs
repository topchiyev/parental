using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Parental.Backend.Repositories;

namespace Parental.Backend;

public class Program
{
    private const string _originPolicy = "ParentalOriginPolicy";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddConsole();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSingleton<DbRepository>();
        builder.Services.AddHostedService<HostedServices.StartupInitializer>();

        #region CORS

        builder.Services.AddCors(options => options.AddPolicy(_originPolicy,
            configurationBuilder => configurationBuilder
                .SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition")
                .AllowCredentials()
        ));

        #endregion


        #region Authentication

        string jwtTokenKey = string.Empty;
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("Parental.Backend.jwt-token.key"))
        {
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    jwtTokenKey = reader.ReadToEnd();
                    if (string.IsNullOrEmpty(jwtTokenKey))
                    {
                        Console.WriteLine("JWT token key not found.");
                        return;
                    }
                }
            }
        }

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
            };
        });

        #endregion

        var app = builder.Build();

        app.UseCors(_originPolicy);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
