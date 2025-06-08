using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DataAccess.Database;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using TrainingApp.Services;

namespace TrainingApp.ViewModels
{
    public class HomeViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "home";
        public IScreen HostScreen { get; }

        [Reactive] public string WelcomeMessage { get; set; }
        [Reactive] public string SearchQuery { get; set; }
        [Reactive] public bool IsBusy { get; set; }
        [Reactive] public ObservableCollection<Workout> RecommendedWorkouts { get; set; }
        [Reactive] public ObservableCollection<Workout> FavoriteWorkouts { get; set; }
        [Reactive] public ObservableCollection<Workout> PopularWorkouts { get; set; }
        
        public ReactiveCommand<Unit, Unit> NavigateHomeCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateWorkoutsCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateFavoritesCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateProfileCommand { get; }
        
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly User? _currentUser;
        private readonly int _currentUserId;

        public HomeViewModel(ApplicationDbContext context, IScreen hostScreen, string email)
        {
            _context = context;
            HostScreen = hostScreen;
            _userService = Locator.Current.GetService<UserService>();
            
            if (_userService is null)
            {
                throw new NullReferenceException("UserService is null");
            }
            _currentUser = _userService.GetUserByEmail(email);
            
            if (_currentUser is null)
            {
                throw new NullReferenceException("User is null");
            }
            
            RecommendedWorkouts = new ObservableCollection<Workout>();
            FavoriteWorkouts = new ObservableCollection<Workout>();
            PopularWorkouts = new ObservableCollection<Workout>();
            
            NavigateHomeCommand = ReactiveCommand.CreateFromTask(NavigateHomeAsync);
            NavigateWorkoutsCommand = ReactiveCommand.CreateFromTask(NavigateWorkoutsAsync);
            NavigateFavoritesCommand = ReactiveCommand.CreateFromTask(NavigateFavoritesAsync);
            NavigateProfileCommand = ReactiveCommand.CreateFromTask(NavigateProfileAsync);
            
            LoadDataAsync();
            WelcomeMessage = $"Ласкаво просимо, {_currentUser.FirstName}! Створюйте власні тренування або обирайте серед програм інших користувачів, " +
                             "оцінюйте та коментуйте, щоб віднайти те, що найкраще підходить саме Вам.";

        }

        private async void LoadDataAsync()
        {
            IsBusy = true;

            try
            {
                var recommended = await _context.Workouts
                    .Where(w => w.Level == _currentUser.PhysicalActivityLevel)
                    .OrderByDescending(w => w.AverageRating)
                    .Take(10)
                    .ToListAsync();

                RecommendedWorkouts = new ObservableCollection<Workout>(recommended);
                
                var favorites = await _context.UserWorkouts
                    .Where(uw => uw.UserId == _currentUserId)
                    .Select(uw => uw.Workout)
                    .ToListAsync();

                FavoriteWorkouts = new ObservableCollection<Workout>(favorites);
                
                var popular = await _context.Workouts
                    .OrderByDescending(w => w.Popularity)
                    .Take(10)
                    .ToListAsync();

                PopularWorkouts = new ObservableCollection<Workout>(popular);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task NavigateHomeAsync()
        {
        }

        private async Task NavigateWorkoutsAsync()
        {
            try
            {
                await HostScreen.Router.Navigate.Execute(new WorkoutsViewModel(_context, HostScreen, _currentUser.UserId));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        private async Task NavigateFavoritesAsync()
        {
            await HostScreen.Router.Navigate.Execute(new SubscribedWorkoutsViewModel(_context, HostScreen, _currentUser.UserId));
        }

        private async Task NavigateProfileAsync()
        {
            await HostScreen.Router.Navigate.Execute(new ProfileViewModel(_context, HostScreen, _currentUser.UserId, _currentUser.UserId));
        }
    }
}
