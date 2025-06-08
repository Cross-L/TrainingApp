
using Splat;
using TrainingApp.ViewModels.Registration;

namespace TrainingApp.Views.Registration
{
    public partial class RegistrationPage
    {
        public RegistrationPage()
        {
            InitializeComponent();
            ViewModel = Locator.Current.GetService<RegistrationViewModel>();
        }
    }
}