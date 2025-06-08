using Splat;
using TrainingApp.ViewModels;

namespace TrainingApp.Views;

public partial class WorkoutsPage
{
    public WorkoutsPage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<WorkoutsViewModel>();
    }
}