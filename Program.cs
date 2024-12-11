using GroqSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// All the way down to line 28 is how I get the application to tie into Groq
var apiKey = builder.Configuration["ApiKey"];
var apiModel = "llama-3.1-70b-versatile";

// This is where I do dependency injection.
// The Services container will construct objects in C#.
// So, at runtime, whenever an object is needed, to be passed into a controller
// the Services can instantiate the objects and inject the objects into the controllers.
// It's adding the IGroqClient interface using dependency injection so that
// the GroqClient object can be used in the controllers.
// This creates objects that call methods that are of type IGroqClient, kind of the way 
// back in Framework you use "using" at the top of every .cs file.
builder.Services.AddSingleton<IGroqClient>(sp =>
    new GroqClient(apiKey, apiModel)
        .SetTemperature(0.5)
        .SetMaxTokens(512)
        .SetTopP(1)
        .SetStop("NONE")
        .SetStructuredRetryPolicy(5)
);
// Now that I have done the above, I can update Views > Home > Index.cshtml with
// an interface to type prompts into a text box and get a response from Groq.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
