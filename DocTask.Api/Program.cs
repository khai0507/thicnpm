// using DocTask.Api.Providers;
// using DocTask.Core.Dtos.Reminders;
// using System.Text;
// using Amazon.S3;
// using DockTask.Api.Configurations;
// using DockTask.Api.Handlers;
// using DocTask.Core.Dtos.Gemini;
// using DocTask.Core.Interfaces.Services;
// using DocTask.Core.Services;
// using DocTask.Data;
// using DocTask.Service.Services;
// using DotNetEnv;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.SignalR;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Options;
// using Microsoft.IdentityModel.Tokens;
// using OpenAI;
// using System.Text;
// using Microsoft.AspNetCore.Authentication.JwtBearer;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwagger();


// builder.Services.AddHttpClient<IGeminiService, GeminiService>();

// // Đạt đã thêm
// builder.Services.AddSignalR();
// builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
// Env.Load();
// builder.Services.AddHttpClient();


// Env.Load();
// Console.WriteLine("========== START DEBUG ==========");

// // Configure JSON serialization to handle circular references
// builder.Services.ConfigureHttpJsonOptions(options =>
// {
//     options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
//     options.SerializerOptions.WriteIndented = true;
// });

// // Configuration SQL
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
// {
//     options.UseSqlServer(Environment.GetEnvironmentVariable("DEFAULT_CONNECTION"));

// });

// // Configuration JWT
// builder.Services.Configure<JwtSetting>(options =>
// {
//     options.AccessSecretKey = Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET_KEY") ?? "";
//     options.RefreshSecretKey = Environment.GetEnvironmentVariable("JWT_REFRESH_SECRET_KEY") ?? "";
//     options.AccessTokenExpiry = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY") ?? "";
//     options.RefreshTokenExpiry = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY") ?? "";
//     options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "";
//     options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "";
// });

// // Configuration Cloudinary
// builder.Services.AddSingleton(new CloudinaryDotNet.Cloudinary(
//     new CloudinaryDotNet.Account(
//         Environment.GetEnvironmentVariable("CLOUDINARY_CLOUDNAME"),
//         Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY"),
//         Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
//     )
// ));


// // --- Bind Minio settings ---

// builder.Services.Configure<MinioSettings>(options =>
// {
//     options.ServiceURL = Environment.GetEnvironmentVariable("MINIO_SERVICE_URL") ?? "";
//     options.BucketName = Environment.GetEnvironmentVariable("MINIO_BUCKETNAME") ?? "";
//     options.AccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? "";
//     options.SecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? "";
// });
// Console.WriteLine($"MINIO_SERVICE_URL = {Environment.GetEnvironmentVariable("MINIO_SERVICE_URL")}");
// Console.WriteLine($"MINIO_BUCKETNAME = {Environment.GetEnvironmentVariable("MINIO_BUCKETNAME")}");
// Console.WriteLine($"MINIO_ACCESS_KEY  = {Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY")}");
// Console.WriteLine($"MINIO_SECRET_KEY  = {Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")}");

// builder.Services.AddSingleton<IAmazonS3>(sp =>
// {
//     var settings = sp.GetRequiredService<IOptions<MinioSettings>>().Value;

//     return new AmazonS3Client(
//         settings.AccessKey,
//         settings.SecretKey,
//         new AmazonS3Config
//         {
//             ServiceURL = settings.ServiceURL,
//             ForcePathStyle = true
//         }
//     );
// });


// // --- Đăng ký FileStorageService ---
// builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

// // Configuration OpenAI
// builder.Services.AddSingleton(new OpenAIClient( 
//     Environment.GetEnvironmentVariable("OPENAI_API_KEY")
// ));

// // Configuration GeminiAI
// builder.Services.AddSingleton(new GeminiDto.GeminiOptions
// {
//     ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? ""
// });

// builder.Services.AddControllerConfiguration();


// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll", policy =>
//     {
//         policy.AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials()

//               .WithOrigins("http://localhost:4200"); // frontend URL
//     });
// });



// // Authorization (tích hợp role-based)
// builder.Services.AddAuthentication("JwtAuth")  // Set default scheme
//     .AddJwtBearer("JwtAuth", options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "",
//             ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "",
//             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET_KEY") ?? "")),
//             ClockSkew = TimeSpan.Zero
//         };
//         // Cho phép token trong query cho SignalR
//         options.Events = new JwtBearerEvents
//         {
//             OnMessageReceived = context =>
//             {
//                 var accessToken = context.Request.Query["access_token"];
//                 var path = context.HttpContext.Request.Path;
//                 if (!string.IsNullOrEmpty(accessToken) &&
//                     path.StartsWithSegments("/notificationHub"))
//                 {
//                     context.Token = accessToken;
//                 }
//                 return Task.CompletedTask;
//             }
//         };
//     });

// builder.Services.AddAuthorization(options =>
// {
//     options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
//     options.AddPolicy("User", policy => policy.RequireRole("User"));
//     options.DefaultPolicy = new AuthorizationPolicyBuilder()
//         .RequireAuthenticatedUser()
//         .Build();
// });

// builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// builder.Services.AddApplicationContainer();

// using (var scope = builder.Services.BuildServiceProvider().CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     try
//     {
//         if (dbContext.Database.CanConnect())
//         {
//             Console.WriteLine("Kết nối đến SQL Server thành công!");
//         }
//         else
//         {
//             Console.WriteLine("Không thể kết nối đến SQL Server.");
//         }
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Lỗi khi kết nối đến SQL Server: {ex.Message}");
//     }
// }

// var app = builder.Build();

// // ép container resolve IAmazonS3 ngay khi khởi động
// using (var scope = app.Services.CreateScope())
// {
//     var s3 = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
//     Console.WriteLine("IAmazonS3 client initialized successfully!");
// }
// var minioSection = builder.Configuration.GetSection("Minio");
// Console.WriteLine("[DEBUG] Minio from appsettings:");
// foreach (var kv in minioSection.GetChildren())
// {
//     Console.WriteLine($"    {kv.Key} = {kv.Value}");
// }


// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// var enableSwagger = Environment.GetEnvironmentVariable("ENABLE_SWAGGER");
// if (app.Environment.IsDevelopment() || string.Equals(enableSwagger, "true", StringComparison.OrdinalIgnoreCase))
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(options =>
//     {
//         options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocTask API v1");
//         // Optional: serve Swagger at app root
//         // options.RoutePrefix = string.Empty;
//     });
// }
// app.UseCors("AllowAll");
// app.UseJwtAuthentication();
// app.UseAuthentication();
// app.UseAuthorization();

// // Đạt đã thêm
// app.MapHub<NotificationHub>("/notificationHub");



// app.MapControllers();
// app.UseExceptionHandler(_ => {});
// app.UseHttpsRedirection();


// app.Run();
using DocTask.Api.Providers;
using DocTask.Core.Dtos.Reminders;
using System.Text;
using Amazon.S3;
using DockTask.Api.Configurations;
using DockTask.Api.Handlers;
using DocTask.Core.Dtos.Gemini;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Services;
using DocTask.Data;
using DocTask.Service.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// ========================
// 🧱 Load ENV + Config
// ========================
Env.Load();
Console.WriteLine("========== START DEBUG ==========");

// ========================
// 📦 Add core services
// ========================
builder.Services.AddEndpointsApiExplorer();
// --- XÓA DÒNG builder.Services.AddSwaggerGen(); CŨ VÀ DÁN ĐOẠN NÀY VÀO ---
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "DocTask API", Version = "v1" });

    // Cấu hình để hiện nút Authorize (Nhập token)
    option.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập 'Bearer [space] token' vào ô dưới. Ví dụ: Bearer eyJhbGci...",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[]{}
        }
    });
});
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// ========================
// 🔄 JSON Serialization
// ========================
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

// ========================
// 💾 Database Configuration
// ========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("⚠️  Không tìm thấy chuỗi kết nối SQL Server trong appsettings.json!");
}
else
{
    Console.WriteLine($"🔗 Chuỗi kết nối SQL: {connectionString}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// ========================
// 🔐 JWT Configuration
// ========================
builder.Services.Configure<JwtSetting>(options =>
{
    options.AccessSecretKey = Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET_KEY") ?? "";
    options.RefreshSecretKey = Environment.GetEnvironmentVariable("JWT_REFRESH_SECRET_KEY") ?? "";
    options.AccessTokenExpiry = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY") ?? "";
    options.RefreshTokenExpiry = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY") ?? "";
    options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "";
    options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "";
});

// ========================
// ☁️ Cloudinary Configuration
// ========================
builder.Services.AddSingleton(new CloudinaryDotNet.Cloudinary(
    new CloudinaryDotNet.Account(
        Environment.GetEnvironmentVariable("CLOUDINARY_CLOUDNAME"),
        Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY"),
        Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
    )
));


builder.Services.Configure<MinioSettings>(options =>
{
    options.ServiceURL = Environment.GetEnvironmentVariable("MINIO_SERVICE_URL") ?? "";
    options.BucketName = Environment.GetEnvironmentVariable("MINIO_BUCKETNAME") ?? "";
    options.AccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? "";
    options.SecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? "";
});

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MinioSettings>>().Value;

    return new AmazonS3Client(
        settings.AccessKey,
        settings.SecretKey,
        new AmazonS3Config
        {
            ServiceURL = settings.ServiceURL,
            ForcePathStyle = true
        }
    );
});

builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

// ========================
// 🤖 OpenAI + Gemini
// ========================
builder.Services.AddSingleton(new OpenAIClient(
    Environment.GetEnvironmentVariable("OPENAI_API_KEY")
));

builder.Services.AddSingleton(new GeminiDto.GeminiOptions
{
    ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? ""
});

// ========================
// ⚙️ Controller + CORS
// ========================
builder.Services.AddControllerConfiguration();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithOrigins("http://localhost:4200");
    });
});

// ========================
// 🔑 Authentication & Authorization
// ========================
builder.Services.AddAuthentication("JwtAuth")
    .AddJwtBearer("JwtAuth", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "",
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET_KEY") ?? "")
            ),
            ClockSkew = TimeSpan.Zero
        };

        // Cho phép token trong query cho SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplicationContainer();


using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (dbContext.Database.CanConnect())
            Console.WriteLine("✅ Kết nối SQL Server thành công!");
        else
            Console.WriteLine("❌ Không thể kết nối đến SQL Server!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Lỗi kết nối SQL Server: {ex.Message}");
    }
}

var app = builder.Build();

// ========================
// 🧰 Middleware pipeline
// ========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseJwtAuthentication();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();
app.UseHttpsRedirection();
app.UseExceptionHandler(_ => { });

app.Run();
