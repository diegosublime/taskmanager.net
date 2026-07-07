using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SolaceSystems.Solclient.Messaging;
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

var SolaceSettingsConfig = builder.Configuration.GetSection(SolaceSettings.KeyName).Get<SolaceSettings>(); //Get from user secrets.json (no injection)
if (SolaceSettingsConfig is null)
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

#region MediatR configuration
builder.Services.AddMediatR(config =>
{
    //TODO: move to different assembly
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
#endregion

#region Messaging configuration

//Solace Connection
builder.Services.AddSingleton<ISolaceBusConnection>(serviceProvider =>
{
    //TODO: pass settings from secrets to connect to solace 
    //This is required before solace starts a session and a context
    ContextFactory.Instance.Init(new ContextFactoryProperties());

    return new SolaceBusConnection(
        new ContextProperties(),
        new SessionProperties()
        {
            Host = SolaceSettingsConfig.Host,
            VPNName = SolaceSettingsConfig.VPNName,
            UserName = SolaceSettingsConfig.UserName,
            Password = SolaceSettingsConfig.Password,
            SSLValidateCertificate = false, //only for local dev
            SSLValidateCertificateDate = false //only for local dev
        });

});

//Message Publisher
builder.Services.AddScoped<IIntegrationEventProducer, SolaceIntegrationEventBus>();

//Message Consumerr (Hosted Service)
builder.Services.AddHostedService<SolaceMessageConsumer>();

#endregion

#region Services injection
builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient()); //using default credentials in C:\Users\youruser\.aws - no need to pass credentials here 
builder.Services.AddScoped<DataRepository>();
builder.Services.AddScoped<TaskService>();
#endregion

#region Global error handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
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
app.UseExceptionHandler();

//Add endpoints
app.UseListTaskEndpoints();

app.Run();

#endregion