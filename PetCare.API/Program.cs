using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PetCare.API.Security;
using PetCare.Application.Common;
using PetCare.Application.Services;
using PetCare.Infrastructure.Data;
using PetCare.Infrastructure.Repositories.Interfaces;
using PetCare.Infrastructure.Repositories.Implementations;
using PetCare.Application.Services.Interfaces;
using PetCare.Application.Services.Implementations;
using PetCare.Infrastructure.Services;
using PetCare.Domain.Interfaces;
using Resend;

// Load environment variables from .env file
// Look for .env in the solution root (parent directory of PetCare.API)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}
else
{
    // Try current directory
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Override configuration with environment variables
builder.Configuration.AddEnvironmentVariables();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Render terminates TLS and forwards requests to the app over HTTP.
    // Trust forwarded proto/for so generated URLs keep https scheme.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PetCare API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// Database configuration
var connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("SupabaseConnection");

var useInMemoryDatabase = false;

if (string.IsNullOrWhiteSpace(connectionString))
{
    if (builder.Environment.IsDevelopment())
    {
        useInMemoryDatabase = true;
        Console.WriteLine("SUPABASE_CONNECTION_STRING is missing. Using InMemory database for Development.");
    }
    else
    {
        throw new InvalidOperationException("Database connection string is not configured.");
    }
}

if (builder.Environment.IsDevelopment())
{
    // Debug output (development only)
    Console.WriteLine($"Connection string configured: {!string.IsNullOrEmpty(connectionString)}");
}

builder.Services.AddDbContextPool<PetCareDbContext>(options =>
{
    if (useInMemoryDatabase)
    {
        options.UseInMemoryDatabase("PetCareDevDb");
        return;
    }

    options.UseNpgsql(
        connectionString!,
        b =>
        {
            b.MigrationsAssembly("PetCare.Infrastructure");
            b.CommandTimeout(120); // 120 seconds timeout
        }
    );
});

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(PetCare.Application.Mappings.MappingProfile));

// Register repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Resend email service
var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")
    ?? builder.Configuration["Resend:ApiKey"]
    ?? string.Empty;
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = resendApiKey;
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<PetCare.Domain.Interfaces.IEmailService, ResendEmailService>();

builder.Services.AddHttpClient();

// Image upload service - Switch between Cloudinary and Local storage
var useCloudinary = Environment.GetEnvironmentVariable("USE_CLOUDINARY") == "true" 
    || builder.Configuration.GetValue<bool>("UseCloudinary", false);

if (useCloudinary)
{
    builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();
    Console.WriteLine("Using Cloudinary for image uploads");
}
else
{
    builder.Services.AddScoped<IImageUploadService, LocalImageUploadService>();
    Console.WriteLine("Using Local storage for image uploads");
}

// Configure Image Upload Settings
builder.Services.Configure<ImageUploadSettings>(options =>
{
    options.StorageType = "Local";
    options.MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    options.AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    options.LocalStoragePath = "wwwroot/uploads";
    options.BaseUrl = "/uploads";
});

builder.Services.Configure<JwtSettings>(options =>
{
    var configuredKey = Environment.GetEnvironmentVariable("JWT_KEY");
    if (string.IsNullOrWhiteSpace(configuredKey))
        configuredKey = builder.Configuration["Jwt:Key"];

    if (string.IsNullOrWhiteSpace(configuredKey))
    {
        if (builder.Environment.IsDevelopment())
        {
            configuredKey = "PetCareDevJwtKey_OnlyForLocalDevelopment_ChangeMe123!";
        }
        else
        {
            throw new InvalidOperationException("JWT Key not configured");
        }
    }

    options.Key = configuredKey;
    options.Issuer = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_ISSUER"))
        ? (builder.Configuration["Jwt:Issuer"] ?? "PetCare.API")
        : Environment.GetEnvironmentVariable("JWT_ISSUER")!;
    options.Audience = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_AUDIENCE"))
        ? (builder.Configuration["Jwt:Audience"] ?? "PetCare.Client")
        : Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;
    
    var expiresMinutes = Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES");
    options.ExpiresInMinutes = !string.IsNullOrEmpty(expiresMinutes) 
        ? int.Parse(expiresMinutes) 
        : builder.Configuration.GetValue<int>("Jwt:ExpiresInMinutes", 60);
});

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
    jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "PetCareDevJwtKey_OnlyForLocalDevelopment_ChangeMe123!";
        Console.WriteLine("JWT_KEY is missing. Using Development fallback key.");
    }
    else
    {
        throw new InvalidOperationException("JWT Key not configured");
    }
}

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
if (string.IsNullOrWhiteSpace(jwtIssuer))
    jwtIssuer = builder.Configuration["Jwt:Issuer"];
jwtIssuer ??= "PetCare.API";

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"JWT Key loaded: {jwtKey.Length} characters");
}

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
if (string.IsNullOrWhiteSpace(jwtAudience))
    jwtAudience = builder.Configuration["Jwt:Audience"];
jwtAudience ??= "PetCare.Client";

var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
    ?? builder.Configuration["Google:ClientId"];

if (string.IsNullOrWhiteSpace(googleClientId))
{
    Console.WriteLine("WARNING: GOOGLE_CLIENT_ID is missing. Google login may fail.");
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
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();

                var authHeader = context.Request.Headers.Authorization.ToString();
                var rawToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader[7..].Trim()
                    : null;

                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (blacklist.IsBlacklisted(rawToken, jti))
                {
                    context.Fail("Token has been revoked");
                }

                return Task.CompletedTask;
            }
        };
    });

// CORS configuration - environment-based for security
builder.Services.AddCors(options =>
{
    var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
        ?? builder.Configuration["AllowedOrigins"];

    options.AddPolicy("AppCorsPolicy", policy =>
    {
        if (string.IsNullOrEmpty(allowedOrigins) || allowedOrigins == "*")
        {
            // Allow all origins (development or when explicitly set to *)
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            Console.WriteLine($"CORS: Allowing all origins ({builder.Environment.EnvironmentName} mode)");
        }
        else
        {
            // Production - specific origins only
            policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
            Console.WriteLine($"CORS: Allowing origins: {allowedOrigins}");
        }
    });
});
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowFrontend",
//        policy =>
//        {
//            policy.WithOrigins("http://localhost:5173")
//                  .AllowAnyHeader()
//                  .AllowAnyMethod();
//        });
//});
var app = builder.Build();

// Ensure core roles exist so role assignment and authorization policies work.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PetCareDbContext>();
    var requiredRoles = new[] { "Customer", "Admin", "Staff" };

    foreach (var roleName in requiredRoles)
    {
        var exists = await context.Roles.AnyAsync(r => r.RoleName == roleName);
        if (!exists)
        {
            context.Roles.Add(new PetCare.Domain.Entities.Role
            {
                RoleName = roleName,
                Description = $"Auto-generated role {roleName}"
            });
        }
    }

    await context.SaveChangesAsync();
}

// Seed database on startup (comment out after first successful run)
// Uncomment only when you need to re-seed the database
/*
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PetCareDbContext>();
    await DbInitializer.SeedAsync(context);
    
    // Seed from PetFinder API (only needed once)
    var httpClient = new HttpClient();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbInitializer.SeedFromPetFinderAsync(context, httpClient, configuration);
}
*/

// Configure the HTTP request pipeline
var enableSwagger = app.Environment.IsDevelopment()
    || string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase)
    || builder.Configuration.GetValue<bool>("EnableSwagger", false);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PetCare API V1");
        c.RoutePrefix = ""; // Swagger chạy ở root
    });
}
// Enable serving static files from wwwroot
app.UseResponseCompression();
app.UseStaticFiles();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("AppCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

static string BuildImageFallbackSvg(string label)
{
        var safeLabel = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(label) ? "Product" : label);
        return $"""
<svg xmlns="http://www.w3.org/2000/svg" width="480" height="360" viewBox="0 0 480 360">
    <defs>
        <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stop-color="#f3f4f6"/>
            <stop offset="100%" stop-color="#e5e7eb"/>
        </linearGradient>
    </defs>
    <rect width="480" height="360" fill="url(#bg)"/>
    <rect x="130" y="85" width="220" height="150" rx="14" fill="none" stroke="#9ca3af" stroke-width="6"/>
    <circle cx="185" cy="140" r="16" fill="none" stroke="#9ca3af" stroke-width="6"/>
    <path d="M150 205l45-42 36 30 40-44 35 56" fill="none" stroke="#9ca3af" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>
    <text x="240" y="284" text-anchor="middle" fill="#4b5563" font-family="Arial, sans-serif" font-size="26" font-weight="600">{safeLabel}</text>
</svg>
""";
}

app.MapGet("/img/{**path}", (string? path) =>
{
        var label = Path.GetFileNameWithoutExtension(path ?? string.Empty);
        return Results.Content(BuildImageFallbackSvg(label), "image/svg+xml");
});

app.MapGet("/uploads/{**path}", (string? path) =>
{
        var label = Path.GetFileNameWithoutExtension(path ?? string.Empty);
        return Results.Content(BuildImageFallbackSvg(label), "image/svg+xml");
});

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
