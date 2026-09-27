using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalAPI.Extensions;
using RentalAPI.Models;
using RentalAPI.Repository;
using RentalAPI.Repository.IRepository;
using RentalAPI.Services;
using RentalAPI.Services.IServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var databaseProvider = builder.Configuration["DatabaseProvider"];

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider == "PostgreSQL")
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection"));

    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection"));

    }
});

//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<IResidentRepository,ResidentRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IVisitorPhotoStorageService, VisitorPhotoStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISocietyAlertRepository, SocietyAlertRepository>();
builder.Services.AddScoped<ISocietyConfigurationRepository, SocietyConfigurationRepository>();
builder.Services.AddScoped<IPmAccountService, PmAccountService>();
builder.Services.AddScoped<IPmAccountRepository, PmAccountRepository>();
builder.Services.AddScoped<IPmAdminAccountRepository,PmAdminAccountRepository>();
builder.Services.AddScoped<IPmAdminAccountService,PmAdminAccountService>();





builder.Services.AddScoped<JwtService>();

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,

//        ValidIssuer = builder.Configuration["Jwt:Issuer"],
//        ValidAudience = builder.Configuration["Jwt:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))




//    };

//});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200","https://premisus360.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();

        Console.WriteLine("Database connection warmed up successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}

app.UseSwagger();

    app.UseSwaggerUI();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowFrontend");

// JWT
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
