using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using taskmanager.api;


#region Builder 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
 
#region AppSettings setup
    builder.Services.Configure<DBSettings>(builder.Configuration.GetSection(DBSettings.KeyName));  //Get from appsettings.json (injection)

    var AuthSettingsConfig = builder.Configuration.GetSection(AuthSettings.KeyName).Get<AuthSettings>(); //Get from user secrets.json (no injection)
    if (AuthSettingsConfig is null)
    {
        throw new NullReferenceException("not loading application settings");
    }
#endregion

#region CORS configuration
//Configure cors so that frontend can use this backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("taskmanager.angular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});
#endregion

#region Auth configuration
//Configure jwt validation, this will validate issuer, lifetime and everything needed for the incoming JWT
builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = AuthSettingsConfig.Issuer;
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = AuthSettingsConfig.Issuer,
                ValidateAudience = false,
                ValidateLifetime = true
            };
        });

    builder.Services.AddAuthorizationBuilder()
        //Cognito does not send audience, so this is a work around to still validate the audience as client id
        .AddPolicy("ValidateAudiencePolicy", policy => policy.RequireClaim("client_id", AuthSettingsConfig.ClientId))
        .AddPolicy("ValidateReadScope", policy => policy.RequireClaim("scope", "taskmanagerAPI/read-task"));
#endregion

#region Services injection
    builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient()); //using default credentials in C:\Users\youruser\.aws - no need to pass credentials here 
    builder.Services.AddScoped<DataRepository>();
#endregion


#endregion

#region Built app

var app = builder.Build();

app.UseCors("taskmanager.angular");

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//Add endpoints
app.UseListTaskEndpoints();

app.Run();

#endregion 