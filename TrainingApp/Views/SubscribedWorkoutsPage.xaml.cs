using Splat;
using TrainingApp.ViewModels;

namespace TrainingApp.Views;

public partial class SubscribedWorkoutsPage
{
    public SubscribedWorkoutsPage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<SubscribedWorkoutsViewModel>();
    }
}