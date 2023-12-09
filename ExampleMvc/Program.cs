using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(30);
    options.ExcludedHosts.Add("example.com");
    options.ExcludedHosts.Add("www.example.com");
});


// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    var jsonInputFormatter = options.InputFormatters
    .OfType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>()
    .Single();
    jsonInputFormatter.SupportedMediaTypes.Add("application/csp-report");
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");

    context.Response.Headers.Add("Feature-Policy", "accelerometer 'none'; camera 'none'; geolocation 'none'; gyroscope 'none'; magnetometer 'none'; microphone 'none'; payment 'none'; usb 'none'");
    
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    //context.Response.Headers.Add("X-Frame-Options", "DENY");


    var rng = RandomNumberGenerator.Create();
    var nonceBytes = new byte[32];
    rng.GetBytes(nonceBytes);
    var nonce = Convert.ToBase64String(nonceBytes);
    context.Items.Add("ScriptNonce", nonce);
    context.Response.Headers.Add("Content-Security-Policy",
    new[] { string.Format("script-src 'self' https://cdnjs.cloudflare.com https://gstatic.com https://google.com 'nonce-{0}'", nonce) });


    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("X-AspNet-Version");
    context.Response.Headers.Remove("X-AspNetMvc-Version");
    context.Response.Headers.Remove("Server");
    

    await next();
});


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
