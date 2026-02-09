using System.Reflection;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using Resend;
using ondock.api.Services;
using ondock.api.Services.Interfaces;

namespace ondock.api.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Configure database services
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Npgsql to use timestamp with time zone
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddDbContext<OnDockDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
            );
        });

        return services;
    }

    /// <summary>
    /// Configure Identity services
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // Set to true in production
            options.SignIn.RequireConfirmedPhoneNumber = false;

            // Two-factor authentication
            options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
        })
        .AddEntityFrameworkStores<OnDockDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Configure JWT Authentication
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
        var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Set to true in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs/monitoring") || path.StartsWithSegments("/hubs/chat")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configure Application Settings
    /// </summary>
    public static IServiceCollection AddApplicationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<OAuthSettings>(configuration.GetSection("OAuthSettings"));
        services.Configure<LiveKitSettings>(configuration.GetSection("LiveKit"));
        services.Configure<FirebaseSettings>(configuration.GetSection("Firebase"));
        services.Configure<MagicPushSettings>(configuration.GetSection("MagicPush"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<ChatSettings>(configuration.GetSection("Chat"));
        services.Configure<AdminSettings>(configuration.GetSection("Admin"));
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        // Configure Resend email client with options
        services.AddOptions<ResendClientOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                var apiKey = config.GetSection("EmailSettings:ApiKey").Value;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    options.ApiToken = apiKey;
                }
            });

        services.AddHttpClient<IResend, ResendClient>();

        return services;
    }

    /// <summary>
    /// Register Application Services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        // Authentication & Authorization Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAppleAuthService, AppleAuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILiveKitTokenService, LiveKitTokenService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<IMonitoringRealtimeService, SignalRMonitoringRealtimeService>();
        services.AddScoped<IMagicPushService, MagicPushService>();

        // Chat
        services.AddSingleton<IChatSettingsProvider, ChatSettingsProvider>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IChatRealtimeService, SignalRChatRealtimeService>();
        services.AddScoped<IChatRoomScanner, ChatRoomScanner>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IChatBroadcastService, ChatBroadcastService>();

        // Contacts
        services.AddScoped<IContactService, ContactService>();

        // User Settings
        services.AddScoped<IUserSettingsService, UserSettingsService>();

        // Profile Categories
        services.AddScoped<IProfileCategoryService, ProfileCategoryService>();

        // Announcements
        services.AddScoped<IAnnouncementService, AnnouncementService>();

        // File Storage
        services.AddScoped<IFileStorageService, FileStorageService>();

        // Encryption
        services.AddScoped<IEncryptionService, EncryptionService>();

        // Identity
        services.AddScoped<IIdentityService, IdentityService>();

        // LoadView
        services.AddScoped<ILoadViewService, LoadViewService>();
        services.AddScoped<ILoadViewRealtimeService, SignalRLoadViewRealtimeService>();

        // Post
        services.AddScoped<IPostService, PostService>();
        
        // User Status (WhatsApp-like Stories)
        services.AddScoped<IUserStatusService, UserStatusService>();

        // TODO: Add more services as they are implemented
        // services.AddScoped<IChatService, ChatService>();
        // services.AddScoped<IRoadService, RoadService>();
        // services.AddScoped<ILocationService, LocationService>();

        // Email & Notifications
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFirebaseMessagingService, FirebaseMessagingService>();
        services.AddScoped<IFcmNotificationService, FcmNotificationService>();

        // Monitoring & Real-time Services
        services.AddScoped<IDockLightStatusProcessor, DockLightStatusProcessor>();
        services.AddScoped<IDockLightMonitoringService, DockLightMonitoringService>();
        services.AddScoped<IInactivityScanner, InactivityScanner>();
        services.AddScoped<ILiveKitService, LiveKitService>();
        services.AddScoped<IMonitoringSessionService, MonitoringSessionService>();

        // Background Services
        services.AddHostedService<LoadViewExpirationScanner>();

        return services;
    }

    /// <summary>
    /// Configure Mapster for object mapping
    /// </summary>
    public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        // Register Mapster mappings
        MappingConfig.RegisterMappings();

        // Add Mapster to DI
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    /// <summary>
    /// Configure Swagger with JWT support
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OnDock Platform API",
                Version = "v1",
                Description = "Real-time, location-based platform for professional truck drivers",
                Contact = new OpenApiContact
                {
                    Name = "NP-TECHOUSE Ltd",
                    Email = "support@ondock.com"
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
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

            // Enable XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Configure CORS policy
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Development CORS policy - Allow all for testing
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            // Production CORS policy - Strict and secure
            options.AddPolicy("Production", builder =>
            {
                builder.WithOrigins("https://ondock.com", "https://www.ondock.com", "http://192.168.1.64")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configure Hangfire for background job processing
    /// </summary>
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
                });
        });

        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.ServerName = "OnDock-HangfireServer";
        });

        return services;
    }

    /// <summary>
    /// Configure API Controllers with model validation
    /// </summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Customize model validation error response to match our error format
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        );

                    // Return BadRequest response with our custom error format
                    var errorResponse = new
                    {
                        error = new
                        {
                            message = "One or more validation errors occurred",
                            statusCode = 400,
                            validationErrors = errors,
                            timestamp = DateTime.UtcNow,
                            path = context.HttpContext.Request.Path.Value
                        }
                    };

                    return new BadRequestObjectResult(errorResponse);
                };
            });

        return services;
    }
}
