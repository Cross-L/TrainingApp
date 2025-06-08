using DataAccess.Database;
using ReactiveUI;
using TrainingApp.ViewModels.Login;

namespace TrainingApp.ViewModels;

public class AppViewModel : ReactiveObject, IScreen
{
    public RoutingState Router { get; }

    
    public AppViewModel(ApplicationDbContext dbContext)
    {
        Router = new RoutingState();
        Router.Navigate.Execute(new LoginViewModel(dbContext, this));
    }
}