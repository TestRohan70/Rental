using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RentalAPI.Configurations;
using System.Text;

namespace RentalAPI.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind Jwt Section
            services.Configure<JwtSettings>(
                configuration.GetSection("Jwt"));

            var jwtSettings =
                configuration
                .GetSection("Jwt")
                .Get<JwtSettings>();

            services.AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)

            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,

                        ValidateAudience = true,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSettings!.Issuer,

                        ValidAudience = jwtSettings.Audience,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    jwtSettings.Key))
                    };
            });

            return services;
        }
    }
}