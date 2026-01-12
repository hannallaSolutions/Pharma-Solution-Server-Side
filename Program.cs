using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Middleware;
using SearchTool_ServerSide.Repository;
using SearchTool_ServerSide.Services;
using ServerSide;

var builder = WebApplication.CreateBuilder(args);

//  CORS (React dev)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173",
                    "https://medi-beta-dev.brightpointsummit.com"

        )
              .AllowAnyHeader()
              .AllowAnyMethod()
              
              .AllowCredentials();
    });
});



//  JWT options
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions == null)
{
    throw new ArgumentNullException(nameof(jwtOptions));
}
builder.Services.AddSingleton(jwtOptions);

//  Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin",
        policy => { policy.RequireRole("SuperAdmin"); });

    options.AddPolicy("Admin",
        policy => { policy.RequireRole("Admin", "SuperAdmin"); });

    options.AddPolicy("Pharmacist",
        policy => { policy.RequireRole("Pharmacist", "Admin", "SuperAdmin", "Doctor"); });
});

// Authentication (JWT Bearer)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

//  DB Contexts
builder.Services.AddDbContext<SearchToolDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SearchTool")));

builder.Services.AddDbContext<GlobalDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Global")));

//  Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

//  DI - Auth
builder.Services.AddScoped<UserAccessToken>();

//  DI - Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<DrugRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<LogRepository>();
builder.Services.AddScoped<BranchRepository>();
builder.Services.AddScoped<InsuranceRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<OrderItemRepository>();
builder.Services.AddScoped<SearchLogRepository>();
builder.Services.AddScoped<NadacRepository>();
builder.Services.AddScoped<MainCompanyRepository>();
builder.Services.AddScoped<DrugClassRepository>();

//  DI - Services
builder.Services.AddScoped<NadacService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DrugService>();
builder.Services.AddScoped<UserSevice>();
builder.Services.AddScoped<InsuranceService>();
builder.Services.AddScoped<LogsService>();
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<MainCompanyService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<DrugClassService>();
builder.Services.AddScoped<BranchService>();

//  Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Helpers
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

var app = builder.Build();

//  Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: keep HTTPS (you can disable in dev if you want)
// app.UseHttpsRedirection();

// Routing is IMPORTANT for CORS to work properly
app.UseRouting();

// CORS must be AFTER UseRouting and BEFORE Auth
app.UseCors("AllowReact");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<UserLogsMiddleware>();

app.MapControllers();

app.Run();
