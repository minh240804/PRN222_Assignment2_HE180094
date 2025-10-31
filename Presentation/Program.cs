using Microsoft.EntityFrameworkCore;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.DAO;
using Assignment2.DataAccess.Repositories;
using Assignment2.BusinessLogic;
using Presentation.Hubs;
using Presentation.Services;

namespace Assignment2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession();
            builder.Services.AddSignalR();

            // Configure database
            builder.Services.AddDbContext<FunewsManagementContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));
            
            // Configure Admin Account Options
            builder.Services.Configure<AdminAccountOptions>(
                builder.Configuration.GetSection("AdminAccount"));

            // Register DAOs
            builder.Services.AddScoped<AccountDAO>();
            builder.Services.AddScoped<CategoryDAO>();
            builder.Services.AddScoped<NewsArticleDAO>();
            builder.Services.AddScoped<TagDAO>();
            builder.Services.AddScoped<ArticleCommentDAO>();

            // Register Repositories
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<INewsArticleRepository, NewsArticleRepository>();
            builder.Services.AddScoped<ITagRepository, TagRepository>();
            builder.Services.AddScoped<IArticleCommentRepository, ArticleCommentRepository>();

            // Register Services
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<INewsArticleService, NewsArticleService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IArticleCommentService, ArticleCommentService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IDashboardNotificationService, DashboardNotificationService>();

            // Configure Session
            builder.Services.AddDistributedMemoryCache();
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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            
            app.UseSession();
            
            app.MapRazorPages();
            app.MapHub<NotificationHub>("/notificationHub");

            app.Run();
        }
    }
}
