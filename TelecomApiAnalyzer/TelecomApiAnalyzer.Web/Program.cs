using TelecomApiAnalyzer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register application services
builder.Services.AddScoped<IApiDocumentAnalyzer, ApiDocumentAnalyzer>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<IPostmanCollectionGenerator, PostmanCollectionGenerator>();
builder.Services.AddScoped<ITestRunnerService, TestRunnerServiceEnhanced>();
builder.Services.AddScoped<IOptusApiService, OptusApiService>();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Add session support for workflow state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ApiAnalyzer}/{action=Index}/{id?}");

app.Run();
