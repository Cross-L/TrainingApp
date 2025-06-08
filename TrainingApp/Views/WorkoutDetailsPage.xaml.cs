using Splat;
using TrainingApp.ViewModels;

namespace TrainingApp.Views;

public partial class WorkoutDetailsPage
{
    public WorkoutDetailsPage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<WorkoutDetailsViewModel>();
    }
}