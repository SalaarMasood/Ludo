// using LudoApp.Server.Hubs;
using LudoApp.Server.Hubs; 
using LudoApp.Server.Services;    // For MongoDbService
using LudoApp.Server.Models;     // For MongoDbSettings
using LudoApp.Shared;            // For User model


var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:5260")  // client URL
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // needed for SignalR
        });
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>(); 


// --- MongoDB Setup Registration ---
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));
// Register MongoDbService as a singleton (one instance for the app's lifetime)
builder.Services.AddSingleton<MongoDbService>();


var app = builder.Build();

app.UseCors("AllowClient");

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ─── ADD THESE TWO LINES TO SERVE YOUR Blazor CLIENT ───────
app.UseBlazorFrameworkFiles();   // <–– serves wwwroot from LudoApp.Client
app.UseStaticFiles();            // <–– serves static assets (css, js, etc.)
// ────────────────────────────────────────────────────────────

// ------------------------------>SIGNUP ENDPOINT
app.MapPost("/api/signup", async (User user, MongoDbService mongoService) =>
{
    // Basic server-side validation (even if client-side validation is present)
    if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
    {
        return Results.BadRequest("Username and Password are required.");
    }

    // Check if username already exists
    var existingUser = await mongoService.GetUserByUsernameAsync(user.Username);
    if (existingUser != null)
    {
        return Results.Conflict("Username already exists."); // HTTP 409 Conflict
    }

    // Create the new user in MongoDB
    await mongoService.CreateUserAsync(user);
    Console.WriteLine($"[SERVER] New user signed up: {user.Username}");
    return Results.Ok("User created successfully."); // HTTP 200 OK
})
.WithName("Signup") // For Swagger documentation
.WithOpenApi();     // To include in Swagger UI
// ----------------------------------



// ----------------------------->LOGIN ENDPOINT
app.MapPost("/api/login", async (User loginUser, MongoDbService mongoService) =>
{
    // Basic server-side validation
    if (string.IsNullOrWhiteSpace(loginUser.Username) || string.IsNullOrWhiteSpace(loginUser.Password))
    {
        return Results.BadRequest("Username and password are required.");
    }

    // Attempt to find the user by username
    var userInDb = await mongoService.GetUserByUsernameAsync(loginUser.Username);

    // Check if user exists AND if the plain text password matches
    if (userInDb == null || userInDb.Password != loginUser.Password)
    {
        Console.WriteLine($"[SERVER] Login failed for user: {loginUser.Username}");
        return Results.BadRequest("Invalid username or password."); 
    }

    Console.WriteLine($"[SERVER] User logged in: {loginUser.Username}");
    return Results.Ok("Login successful."); // HTTP 200 OK
})
.WithName("Login")
.WithOpenApi();



app.MapHub<GameHub>("/gamehub");

// ─── FALLBACK TO Blazor CLIENT ─────────────────────────────
app.MapFallbackToFile("index.html");  // <–– any non-/api request goes to wwwroot/index.html
// ────────────────────────────────────────────────────────────

// Add this endpoint after your existing login/signup endpoints

app.MapGet("/api/user/{username}/wins", async (string username, MongoDbService mongoService) =>
{
    if (string.IsNullOrWhiteSpace(username))
    {
        return Results.BadRequest("Username is required.");
    }

    var user = await mongoService.GetUserByUsernameAsync(username);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    return Results.Ok(user.Wins.ToString());
})
.WithName("GetUserWins")
.WithOpenApi();

app.Run();