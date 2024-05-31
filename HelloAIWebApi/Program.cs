using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion("gpt-3.5-turbo", Environment.GetEnvironmentVariable("AI:OpenAI:ApiKey"));

var app = builder.Build();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async (Kernel kernel) =>
{
    var temp = Random.Shared.Next(-20, 55);
    return new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now),
            temp,
            await kernel.InvokePromptAsync<string>($"Short description of the weather at {temp}¢XC?")
        );
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
