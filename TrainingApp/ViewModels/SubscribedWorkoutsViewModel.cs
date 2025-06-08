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
    public class SubscribedWorkoutsViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "subscribed_workouts";
        public IScreen HostScreen { get; }

        [Reactive] public string SearchQuery { get; set; }
        [Reactive] public bool IsBusy { get; set; }
        [Reactive] public ObservableCollection<WorkoutViewModel> SubscribedWorkouts { get; set; }
        private List<Workout> _allSubscribedWorkouts = new();

        public ReactiveCommand<Unit, Unit> NavigateHomeCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateWorkoutsCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateFavoritesCommand { get; }

        public ReactiveCommand<WorkoutViewModel, Unit> UnsubscribeWorkoutCommand { get; }
        public ReactiveCommand<WorkoutViewModel, Unit> ViewWorkoutCommand { get; }

        public ReactiveCommand<int, Unit> GoToAuthorProfileCommand { get; }

        private readonly ApplicationDbContext _context;
        private readonly User? _currentUser;
        private readonly int _currentUserId;

        public SubscribedWorkoutsViewModel(ApplicationDbContext context, IScreen hostScreen, int userId)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            HostScreen = hostScreen;
            var userService = Locator.Current.GetService<UserService>() ??
                               throw new NullReferenceException("UserService is not registered.");

            _currentUser = userService.GetUserById(userId) ??
                           throw new NullReferenceException("Current user not found.");
            _currentUserId = _currentUser.UserId;

            SubscribedWorkouts = new ObservableCollection<WorkoutViewModel>();

            UnsubscribeWorkoutCommand = ReactiveCommand.CreateFromTask<WorkoutViewModel>(async workoutViewModel =>
            {
                await UnsubscribeWorkoutAsync(workoutViewModel);
            });

            ViewWorkoutCommand = ReactiveCommand.CreateFromTask<WorkoutViewModel>(ViewWorkoutAsync);

            NavigateHomeCommand = ReactiveCommand.CreateFromTask(NavigateHomeAsync);
            NavigateWorkoutsCommand = ReactiveCommand.CreateFromTask(NavigateWorkoutsAsync);
            NavigateFavoritesCommand = ReactiveCommand.CreateFromTask(NavigateFavoritesAsync);
            NavigateProfileCommand = ReactiveCommand.CreateFromTask(NavigateProfileAsync);

            GoToAuthorProfileCommand = ReactiveCommand.CreateFromTask<int>(GoToAuthorProfile);

            this.WhenAnyValue(x => x.SearchQuery)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(FilterWorkouts);

            LoadSubscribedWorkoutsAsync();

            this.WhenNavigatedTo(() =>
            {
                LoadSubscribedWorkoutsAsync();
                return Task.CompletedTask;
            });
        }

        private async void LoadSubscribedWorkoutsAsync()
        {
            IsBusy = true;
            try
            {
                var subscribedWorkouts = await _context.UserWorkouts
                    .Where(uw => uw.UserId == _currentUserId)
                    .OrderByDescending(uw => uw.AddedAt)
                    .Include(uw => uw.Workout)
                        .ThenInclude(w => w.Author)
                    .Select(uw => uw.Workout)
                    .ToListAsync();

                _allSubscribedWorkouts = subscribedWorkouts;
                SubscribedWorkouts = new ObservableCollection<WorkoutViewModel>(
                    subscribedWorkouts.Select(w => new WorkoutViewModel(w, true))
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FilterWorkouts(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                SubscribedWorkouts = new ObservableCollection<WorkoutViewModel>(
                    _allSubscribedWorkouts.Select(w => new WorkoutViewModel(w, true))
                );
                return;
            }

            var lowerQuery = query.ToLower();

            var filteredWorkouts = _allSubscribedWorkouts
                .Select(w => new
                {
                    Workout = w,
                    Score = CalculateScore(w, lowerQuery)
                })
                .Where(w => w.Score > 0)
                .OrderByDescending(w => w.Score)
                .Select(w => new WorkoutViewModel(w.Workout, true))
                .ToList();

            SubscribedWorkouts = new ObservableCollection<WorkoutViewModel>(filteredWorkouts);
        }

        private int CalculateScore(Workout workout, string query)
        {
            int score = 0;

            if (!string.IsNullOrEmpty(workout.Tags) && workout.Tags.ToLower().Contains(query))
                score += 100;

            if (!string.IsNullOrEmpty(workout.Name) && workout.Name.ToLower().Contains(query))
                score += 80;

            if (workout.Author != null)
            {
                var authorFullName = $"{workout.Author.FirstName} {workout.Author.LastName}".ToLower();
                if (authorFullName.Contains(query))
                    score += 60;
            }

            if (!string.IsNullOrEmpty(workout.Description) && workout.Description.ToLower().Contains(query))
                score += 40;

            if (!string.IsNullOrEmpty(workout.Goals) && workout.Goals.ToLower().Contains(query))
                score += 30;

            if (!string.IsNullOrEmpty(workout.MuscleGroups) && workout.MuscleGroups.ToLower().Contains(query))
                score += 20;

            return score;
        }

        private async Task UnsubscribeWorkoutAsync(WorkoutViewModel workoutViewModel)
        {
            if (workoutViewModel == null || !workoutViewModel.IsSubscribed)
                return;

            var confirm = await ConfirmUnsubscriptionAsync(workoutViewModel.Workout.Name);
            if (!confirm)
                return;

            try
            {
                var userWorkout = await _context.UserWorkouts
                    .FirstOrDefaultAsync(uw => uw.UserId == _currentUserId && uw.WorkoutId == workoutViewModel.Workout.WorkoutId);

                if (userWorkout != null)
                {
                    _context.UserWorkouts.Remove(userWorkout);
                    workoutViewModel.Workout.Popularity -= 1;
                    workoutViewModel.IsSubscribed = false;

                    await _context.SaveChangesAsync();
                    SubscribedWorkouts.Remove(workoutViewModel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка відписки від тренування: {ex.Message}");
            }
        }

        private async Task ViewWorkoutAsync(WorkoutViewModel workout)
        {
            if (workout == null)
                return;

            try
            {
                await HostScreen.Router.Navigate.Execute(new WorkoutDetailsViewModel(HostScreen, _context, workout.Workout, _currentUserId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка переходу до деталей тренування: {ex.Message}");
            }
        }

        private async Task NavigateHomeAsync()
        {
            await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, _currentUser.Email));
        }

        private async Task NavigateWorkoutsAsync()
        {
            await HostScreen.Router.Navigate.Execute(new WorkoutsViewModel(_context, HostScreen, _currentUserId));
        }

        private async Task NavigateFavoritesAsync()
        {
        }

        private async Task NavigateProfileAsync()
        {
            await HostScreen.Router.Navigate.Execute(new ProfileViewModel(_context, HostScreen, _currentUser.UserId, _currentUser.UserId));
        }

        private async Task GoToAuthorProfile(int authorId)
        {
            try
            {
                await HostScreen.Router.Navigate.Execute(new ProfileViewModel(_context, HostScreen, authorId, _currentUserId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка переходу до профілю автора: {ex.Message}");
            }
        }

        private async Task<bool> ConfirmUnsubscriptionAsync(string workoutName)
        {
            var isConfirmed = await Application.Current.MainPage.DisplayAlert(
                "Підтвердження",
                $"Ви впевнені, що хочете відписатися від тренування \"{workoutName}\"?",
                "Так",
                "Ні"
            );

            return isConfirmed;
        }
    }
}
