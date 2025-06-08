using Splat;
using TrainingApp.ViewModels;

namespace TrainingApp.Views;

public partial class CreateWorkoutPage
{
    public CreateWorkoutPage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<CreateWorkoutViewModel>();
    }
}