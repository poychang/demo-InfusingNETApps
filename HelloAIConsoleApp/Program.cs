/*
 * Microsoft Build 2024
 * Infusing your .NET Apps with AI: Practical Tools and Techniques | BRK187
 * https://www.youtube.com/watch?v=jrNfKeGSuCg
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
// Add poly to retry and redaction the headers
builder.Services.ConfigureHttpClientDefaults(builder =>
{
    builder.AddStandardResilienceHandler();
    builder.RedactLoggedHeaders(["Authorization"]);
});
builder.Services.AddRedaction();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddSingleton<IFunctionInvocationFilter, PermissionFilter>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var kernel = builder
    .AddOpenAIChatCompletion("gpt-3.5-turbo", Environment.GetEnvironmentVariable("AI:OpenAI:ApiKey"))
    .Build();

// Add plugin
kernel.ImportPluginFromType<Demographic>();
// it is doing the same as below
//var plugin = KernelPluginFactory.CreateFromType<Demographic>();
//kernel.Plugins.Add(plugin);
// Add Bing Search
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
kernel.ImportPluginFromObject(new WebSearchEnginePlugin(new BingConnector(Environment.GetEnvironmentVariable("AI:Bing:ApiKey"))));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


var settings = new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

var chatService = kernel.GetRequiredService<IChatCompletionService>();
ChatHistory chat = new();
while (true)
{
    Console.Write("Q: ");
    chat.AddUserMessage(Console.ReadLine());

    var r = await chatService.GetChatMessageContentAsync(chat, settings, kernel);
    Console.WriteLine(r);
    chat.Add(r);
}

class Demographic
{
    [KernelFunction]
    public int GetPersonAge(string name)
    {
        return name switch
        {
            "Poy" => 25,
            _ => 0
        };
    }
}

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
class PermissionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"Allow {context.Function.Name}?");
        if (Console.ReadLine() == "y")
        {
            await next(context);
        }
        else
        {
            throw new Exception("Permission denied");
        }
    }
}
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.