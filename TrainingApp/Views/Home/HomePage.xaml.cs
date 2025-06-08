using Splat;
using TrainingApp.ViewModels;

namespace TrainingApp.Views.Home;

public partial class HomePage
{
    public HomePage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<HomeViewModel>();
    }
}