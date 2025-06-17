using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавление служб
builder.Services.AddRazorPages();
builder.Services.AddDbContext<VeterinaryClinicContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<LogService>();

// Настройка аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login"; // Путь к странице входа
        options.LogoutPath = "/Profile?handler=Logout";
        options.AccessDeniedPath = "/AccessDenied"; // Путь к странице отказа в доступе
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // cookie только по HTTPS
        options.Cookie.SameSite = SameSiteMode.Strict; // или Lax, в зависимости от задачи
    });
builder.Services.AddTransient<IEmailSender>(provider =>
    new EmailSender(
        builder.Configuration["EmailSettings:SmtpServer"],
        int.Parse(builder.Configuration["EmailSettings:SmtpPort"]),
        builder.Configuration["EmailSettings:FromEmail"],
        builder.Configuration["EmailSettings:SmtpUsername"],
        builder.Configuration["EmailSettings:SmtpPassword"]
    ));
var app = builder.Build();

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
