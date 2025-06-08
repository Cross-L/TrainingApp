namespace TrainingApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new ReactiveUI.Maui.RoutedViewHost();
        }
    }
}