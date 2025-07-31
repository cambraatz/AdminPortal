using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;

// token initialization...
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using AdminPortal.Server.Services;
using AdminPortal.Server.Services.Interfaces;

using Serilog;

// new modification to CORS package...
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {builder.Environment.EnvironmentName}");
//Console.WriteLine($"\"Jwt:Key\": {builder.Configuration["Jwt:Key"]}");
//Console.WriteLine($"\"Jwt:Issuer\": {builder.Configuration["Jwt:Issuer"]}");
//Console.WriteLine($"\"Jwt:Audience\": {builder.Configuration["Jwt:Audience"]}");

var logPath = builder.Environment.IsProduction()
    ? Path.Combine(builder.Environment.ContentRootPath, "log", "logs.log")
    : Path.Combine("log", "logs.log");

builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
);

if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5500);
    });

    builder.Services.AddCors(options => {
        options.AddPolicy(name: MyAllowSpecificOrigins,
            policy => {
                policy.SetIsOriginAllowed(origin => new Uri(origin).Host.EndsWith("tcsservices.com"))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });
}
else
{
    builder.Services.AddCors(options => {
        options.AddPolicy(name: MyAllowSpecificOrigins,
            policy => {
                policy.WithOrigins("https://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });
}



// Add services to the container.
//builder.Services.AddControllers();

// Adding Serializers, this is a new attempt...
// JSON Serializer
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
});

// token initialization...
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,
        AudienceValidator = (audiencesInToken, securityToken, validationParameters) =>
        {
            // These are the values your app *expects* from its configuration
            string expectedAudience = builder.Configuration["Jwt:Audience"]!; // Should be "localhost:5173"
            string expectedIssuer = builder.Configuration["Jwt:Issuer"]!;     // Should be "localhost:7242"

            // This list contains the audiences YOUR APP considers valid for an incoming token
            var allowedAudiencesForThisApp = new List<string>
            {
                expectedAudience,
                expectedIssuer
            };

            Console.WriteLine($"--- AUDIENCE VALIDATOR DEBUG ---");
            Console.WriteLine($"  Expected Audience (from config): '{expectedAudience}'");
            Console.WriteLine($"  Expected Issuer (from config): '{expectedIssuer}'");
            // This will show you exactly what strings the token handler passed into the validator
            Console.WriteLine($"  Token Audiences (raw from param): {string.Join(", ", audiencesInToken.Select(a => $"'{a}'"))}");
            Console.WriteLine($"  Allowed Audiences for This App (from config): {string.Join(", ", allowedAudiencesForThisApp.Select(a => $"'{a}'"))}");

            // This is the core validation logic: Does ANY audience in the token match ANY of our allowed audiences?
            bool hasValidAudience = audiencesInToken.Intersect(allowedAudiencesForThisApp, StringComparer.OrdinalIgnoreCase).Any();

            if (hasValidAudience)
            {
                Console.WriteLine($"  RESULT: Audience Validation Succeeded!");
            }
            else
            {
                Console.WriteLine($"  RESULT: Audience Validation FAILED - No common audience found.");
            }
            Console.WriteLine($"----------------------------------");

            return hasValidAudience;
        },

        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        
        //ValidIssuer = "https://login.tcsservices.com",
        //ValidAudience = builder.Configuration["Jwt:Audience"],
        
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Try to read the token from the "access_token" cookie
            if (context.Request.Cookies.ContainsKey("access_token"))
            {
                context.Token = context.Request.Cookies["access_token"];
            }
            // If not found in cookie, you could also check headers (default behavior)
            // Or prioritize cookies if that's your primary method
            else if (context.Request.Headers.ContainsKey("Authorization"))
            {
                // If it's in the header, make sure it's a "Bearer " token
                string authorizationHeader = context.Request.Headers["Authorization"];
                if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authorizationHeader.Substring("Bearer ".Length).Trim();
                }
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            // Log full exception for detailed debugging
            //_logger.LogError(context.Exception, "JWT Authentication failed.");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated successfully for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// var app = builder.Build();

builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ISessionService, SessionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Forwarded Headers (if behind a proxy like Nginx in Production)
if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}

app.UseHttpsRedirection(); // Redirects HTTP to HTTPS

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors(MyAllowSpecificOrigins);

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always
});

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();