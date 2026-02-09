var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddControllers();
var app = builder.Build();

// Runs the app locally on the home network
// Used for testing the application on a mobile device
var url = "http://0.0.0.0:5000";
app.Urls.Add(url);

// Adds a static website if the user goes to / using a GET request (Browser)
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Website")
    ),
    RequestPath = "/Website"
});

app.MapGet("/", async context =>
{
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Website", "index.html");
    var htmlContent = await File.ReadAllTextAsync(filePath);

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(htmlContent);
});

// This is the debugger for whatever apps come through and request something
// It will tell you what the app has requested, like GET to /1.1/<endpoint>
app.Use(async (context, next) =>
{
    // Check if the request is a POST, PUT, or PATCH request
    // Most of the time for stuff like this, Twitter uses a POST request.
    var requestBody = await ReadRequestBodyAsync(context);

    var method = context.Request.Method;
    var originalPath = context.Request.Path;
    var queryString = context.Request.QueryString.Value;
    var combinedPath = originalPath + queryString;
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Received {Method} request to {Path}.", method, combinedPath);
    foreach (var header in context.Request.Headers)
    {
        logger.LogInformation("Request header: {Key}: {Value}", header.Key, header.Value);
    }
    if (method == "POST")
    {
        logger.LogInformation("The request body of that request is {RequestBody}", requestBody);
    }

    context.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody));

    await next.Invoke();
});

// Used to the read the request body of a POST, PUT or PATCH request
async Task<string> ReadRequestBodyAsync(Microsoft.AspNetCore.Http.HttpContext context)
{
    context.Request.EnableBuffering();
    using (var reader = new System.IO.StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
    {
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        return body;
    }
}

// Just remember to use http:// when your testing locally using your own URI.
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();