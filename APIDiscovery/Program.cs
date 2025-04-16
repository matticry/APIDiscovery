using System.Text;
using APIDiscovery.Controllers.Middleware;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Services;
using APIDiscovery.Services.Commands;
using APIDiscovery.Services.Security;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;


DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5031");

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException());
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero  
        };
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddHostedService<UserActionConsumerService>();
builder.Services.AddScoped<CustomService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IFareService, FareService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath = "/uploads"
});
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();