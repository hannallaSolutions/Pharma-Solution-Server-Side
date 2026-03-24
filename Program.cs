using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Middleware;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;
using SearchTool_ServerSide.Services;
using ServerSide;
using Microsoft.AspNetCore.Authorization;
using SearchTool_ServerSide.Authorization;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions == null)
{
    throw new ArgumentNullException(nameof(jwtOptions));
}
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin",
    builder => { builder.RequireRole("SuperAdmin"); });
    options.AddPolicy("Admin",
    builder => { builder.RequireRole("Admin", "SuperAdmin"); });
    options.AddPolicy("Pharmacist",
    builder => { builder.RequireRole("Pharmacist", "Admin", "SuperAdmin", "Doctor"); });
    options.AddPolicy("Doctor",
    builder => { builder.RequireRole("Pharmacist", "Admin", "SuperAdmin", "Doctor"); });

});

builder.Services.AddAuthentication()
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            };

        });
builder.Services.AddDbContext<SearchToolDBContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("SearchTool")));
builder.Services.AddDbContext<GlobalDBContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Global")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
//////////////////////////////////////////////
builder.Services.AddScoped<UserAccessToken>();
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
builder.Services.AddScoped<BranchRepository>();
builder.Services.AddScoped<DiseaseRepository>();
//////////////////////////////////////////////
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
builder.Services.AddScoped<DiseaseService>();
builder.Services.AddScoped<ScriptsRepository>();
builder.Services.AddScoped<ScriptsService>();

//////////////////////////////////////////////
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

//for permissions
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddAuthorization();

var allowedOrigins = new List<string>
{
    "https://medisearchtool.com",
    "https://pharmacy.medisearchtool.com",
    "https://medi-dev-test.hanna-west.com",
    "https://medi-beta-dev.brightpointsummit.com",
    "http://medi-beta-dev.brightpointsummit.com",
    "http://localhost:5173",
        "http://localhost:5174",
        "http://127.0.0.1:8000",

};

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserLogsMiddleware>();
app.UseMiddleware<PermissionMiddleware>();
app.MapControllers();
app.Run();
