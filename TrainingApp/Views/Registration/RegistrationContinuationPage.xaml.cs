using Splat;
using TrainingApp.ViewModels.Registration;

namespace TrainingApp.Views.Registration;

public partial class RegistrationContinuationPage
{
    public RegistrationContinuationPage()
    {
        ViewModel = Locator.Current.GetService<RegistrationContinuationViewModel>();
        InitializeComponent();
    }
}