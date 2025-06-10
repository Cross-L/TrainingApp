using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataAccess.Database;
using ReactiveUI;
using Splat;
using TrainingApp.Services;
using TrainingApp.ViewModels.Login;
using TrainingApp.Views.Registration;
using TrainingApp.ViewModels.Registration;
using TrainingApp.Views.Login;
using TrainingApp.ViewModels;
using TrainingApp.Views;
using TrainingApp.Views.Home;

namespace TrainingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        const string pathToConfigFile = "appsettings.json"; 
        var appConfig = AppConfigLoader.LoadConfig(pathToConfigFile);

        if (appConfig == null)
        {
            throw new InvalidOperationException("Failed to load configuration from appsettings.json");
        }

#if DEBUG
        builder.Logging.AddDebug();
#endif
        
#if ANDROID
        var host = "192.168.1.114";
#else
        var host = appConfig.ConnectionStrings.Host;
#endif
        
        var connectionString = $"Host={host};" +
                               $"Port={appConfig.ConnectionStrings.Port};" +
                               $"Database={appConfig.ConnectionStrings.Database};" +
                               $"Username={appConfig.ConnectionStrings.Username};" +
                               $"Password={appConfig.ConnectionStrings.Password}";

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString,
                x => x.MigrationsAssembly("MigrationsProject")));

        builder.Services.AddSingleton<AppViewModel>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddTransient<RegistrationViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<RegistrationContinuationViewModel>();
        builder.Services.AddTransient<WorkoutsViewModel>();
        builder.Services.AddTransient<CreateWorkoutViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<WorkoutDetailsViewModel>();
        builder.Services.AddTransient<SubscribedWorkoutsViewModel>();

        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<RegistrationContinuationPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<WorkoutsPage>();
        builder.Services.AddTransient<CreateWorkoutPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<WorkoutDetailsPage>();
        builder.Services.AddTransient<SubscribedWorkoutsPage>();

        var app = builder.Build();

        var appViewModel = app.Services.GetRequiredService<AppViewModel>();
        Locator.CurrentMutable.RegisterConstant(appViewModel, typeof(IScreen));
        Locator.CurrentMutable.RegisterConstant(app.Services.GetRequiredService<UserService>(), typeof(UserService));
        Locator.CurrentMutable.Register(() => new LoginPage(), typeof(IViewFor<LoginViewModel>));
        Locator.CurrentMutable.Register(() => new RegistrationPage(), typeof(IViewFor<RegistrationViewModel>));
        Locator.CurrentMutable.Register(() => new RegistrationContinuationPage(),
            typeof(IViewFor<RegistrationContinuationViewModel>));
        Locator.CurrentMutable.Register(() => new HomePage(), typeof(IViewFor<HomeViewModel>));
        Locator.CurrentMutable.Register(() => new WorkoutsPage(), typeof(IViewFor<WorkoutsViewModel>));
        Locator.CurrentMutable.Register(() => new CreateWorkoutPage(), typeof(IViewFor<CreateWorkoutViewModel>));
        Locator.CurrentMutable.Register(() => new ProfilePage(), typeof(IViewFor<ProfileViewModel>));
        Locator.CurrentMutable.Register(() => new WorkoutDetailsPage(), typeof(IViewFor<WorkoutDetailsViewModel>));
        Locator.CurrentMutable.Register(() => new SubscribedWorkoutsPage(),
            typeof(IViewFor<SubscribedWorkoutsViewModel>));

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        return app;
    }
}