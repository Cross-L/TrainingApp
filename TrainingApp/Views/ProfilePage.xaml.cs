using System.Reactive.Disposables;
using ReactiveUI;
using Splat;
using TrainingApp.ViewModels;

namespace TrainingApp.Views;

public partial class ProfilePage
{
    
    public ProfilePage()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<ProfileViewModel>();
        
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Title, v => v.Title)
                .DisposeWith(disposables);
        });
    }
}
