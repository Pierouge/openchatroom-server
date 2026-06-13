using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// To ensure the connection to the MySQL DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

builder.Services.AddCors(options =>
{
  options.AddPolicy(
      "AllowWasmApp",
      policy =>
      {
        policy
              .SetIsOriginAllowed(_ => true) // To allow any origin
              .AllowAnyHeader()
              .AllowAnyMethod();
        // .AllowCredentials(); -> Not needed as no cookie setup
      }
  );
});

// Add the UserAccessor as a scoped service
builder.Services.AddScoped<UserAccessor>();

SymmetricSecurityKey jwtKey = KeyManager.GetOrGenKey(builder.Configuration.GetSection("Jwt").GetValue<string>("KeyFile")!);
builder.Services.AddSingleton(jwtKey);

builder
    .Services.AddAuthentication()
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        jwtOptions =>
        {
          // jwtOptions.MetadataAddress = builder.Configuration["Api:MetadataAddress"];
          // Optional if the MetadataAddress is specified
          jwtOptions.RequireHttpsMetadata = false;
          jwtOptions.Authority = builder
              .Configuration.GetSection("Jwt")
              .GetValue<string>("Authority");
          jwtOptions.Audience = builder
              .Configuration.GetSection("Jwt")
              .GetValue<string>("Audience");
          jwtOptions.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidIssuer = builder
              .Configuration.GetSection("Jwt")
              .GetValue<string>("Issuer"),
            ValidateAudience = true,
            ValidAudience = builder
              .Configuration.GetSection("Jwt")
              .GetValue<string>("Audience"),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = jwtKey
          };

          jwtOptions.MapInboundClaims = false;
        }
    );

builder.Services.AddSingleton<IJWTBuilder, JWTBuilder>();
builder.Services.AddScoped<IAuthorizationHandler, JwtValidityHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationType.Authenticated, policy =>
          {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new JwtValidityRequirement(false));
          })
    .AddPolicy(AuthorizationType.RefreshTokens, policy =>
    {
      policy.RequireAuthenticatedUser();
      policy.Requirements.Add(new JwtValidityRequirement(true));
    });

// Add the LoginStorage Service as a singleton
builder.Services.AddSingleton<ILoginStorage, LoginStorage>();
builder.Services.AddHostedService<LoginStorageCleaner>();

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationResultHandler>();

// Add Data Protection to the app
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration.GetSection("Encryptor").GetValue<string>("Directory")!))
    .SetApplicationName("OpenChatRoom");

// Add Encryptor classes for different purposes
builder.Services.AddSingleton<MessageEncryptor>();

WebApplication app = builder.Build();

// Force usage of HTTPS
app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowWasmApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<AppHub>("hub/app");

app.MapControllers();
app.Run();
