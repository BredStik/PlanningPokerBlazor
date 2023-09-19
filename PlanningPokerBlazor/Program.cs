using Microsoft.AspNetCore.ResponseCompression;
using Orleans;
using Orleans.Hosting;
using PlanningPokerBlazor.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(builder =>
{
    builder.UseLocalhostClustering();
    builder.AddMemoryGrainStorage("games");
    builder.UseDashboard(opt => opt.HostSelf = true);
});

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
//builder.Services.AddSingleton<GameTracker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapHub<GameHub>("/gamehub");

/*app.MapGet("/resetgametracker", (GameTracker tracker) =>
{
    tracker.Reset();
    return Results.StatusCode(StatusCodes.Status202Accepted);
});*/

app.Map("/dashboard", x => x.UseOrleansDashboard());

app.Run();