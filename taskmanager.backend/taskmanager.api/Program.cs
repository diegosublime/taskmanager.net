using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration.UserSecrets;
using taskmanager.api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

//Configure jwt validation, this will validate issuer, lifetime and everything needed for the incoming JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        options.Authority = "***"; 
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = "***",
            ValidateAudience = false, 
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorizationBuilder()
    //Cognito does not send audience, so this is a work around to still validate the audience as client id
    .AddPolicy("ValidateAudiencePolicy", policy => policy.RequireClaim("client_id", "***"))
    .AddPolicy("ValidateReadScope", policy => policy.RequireClaim("scope", "taskmanagerAPI/read-task"));

builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient()); //using default credentials in C:\Users\youruser\.aws - no need to pass credentials here

builder.Services.AddScoped<DataRepository>();

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


app.MapGet("api/lists", async (string userId, DataRepository dataRepository, CancellationToken cancellationToken) =>
{
    var listResponse = await dataRepository.GetListsByUserAsync(userId, cancellationToken);

    return Results.Ok(listResponse);
})
.WithName("GetListsbyUserId");
//.RequireAuthorization("ValidateAudiencePolicy");

app.MapGet("api/lists/{listId}", async (string listId, DataRepository dataRepository, CancellationToken cancellationToken) =>
{
    var listResponse = await dataRepository.GetListByIdAsync(listId, cancellationToken);

    if (listResponse == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(listResponse);
})
.WithName("GetListById"); 
//.RequireAuthorization("ValidateAudiencePolicy");

app.MapGet("api/testdynamo", async (DataRepository dataRepository) => 
{
    var tableNames = await dataRepository.GetTableName();
    return tableNames;
});



app.Run();

public record ListTask(string Id, string UserId, string Name, string Description) { }

