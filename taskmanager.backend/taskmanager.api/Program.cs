using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration.UserSecrets;

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
 

app.MapGet("api/lists", (string userId) =>
{
    var lists = new List<ListTask>() 
    { 
        new ListTask("1", "user1", "Work the garden", "This is task 1, belongs to user 1"),
        new ListTask("2", "user1", "Feed the cat", "This is task 2, belongs to user 1"),
        new ListTask("3", "user2", "Clean the house", "This is task 3, belongs to user 2")
    };

    var listsResponse = lists.Where(l => l.UserId == userId).ToList();

    return listsResponse;
})
.WithName("GetLists")
.RequireAuthorization("ValidateAudiencePolicy");

app.Run();

internal record ListTask(string Id, string UserId, string Name, string Description) { }

