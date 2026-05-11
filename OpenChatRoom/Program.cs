using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Storage for Session data
builder.Services.AddDistributedMemoryCache();

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
              .AllowAnyMethod()
              .AllowCredentials();
      }
  );
});

SymmetricSecurityKey jwtKey = KeyManager.getOrGenKey(builder.Configuration.GetSection("Jwt").GetValue<string>("KeyFile")!);
builder.Services.AddSingleton(jwtKey);

builder
    .Services.AddAuthentication()
    .AddJwtBearer(
        "user",
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
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = jwtKey
          };

          jwtOptions.MapInboundClaims = false;
        }
    );

builder.Services.AddSingleton<IJWTBuilder, JWTBuilder>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Authenticated", policy =>
          {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new JwtValidityRequirement());
          });

// Add the LoginStorage Service as a singleton
builder.Services.AddSingleton<ILoginStorage, LoginStorage>();
builder.Services.AddHostedService<LoginStorageCleaner>();

// Add the UserAccessor as a scoped service
builder.Services.AddScoped<UserAccessor>();

WebApplication app = builder.Build();

// Force usage of HTTPS
app.UseHsts();
app.UseHttpsRedirection();

app.UseCors("AllowWasmApp");

app.MapControllers();
app.Run();
