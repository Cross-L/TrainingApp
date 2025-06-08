using Splat;
using TrainingApp.ViewModels.Login;

namespace TrainingApp.Views.Login;

public partial class LoginPage
{
    public LoginPage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<LoginViewModel>();
    }
}