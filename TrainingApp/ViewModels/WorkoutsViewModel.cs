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
    public class WorkoutViewModel(Workout workout, bool isSubscribed) : ReactiveObject
    {
        public Workout Workout { get; } = workout ?? throw new ArgumentNullException(nameof(workout));

        [Reactive] public bool IsSubscribed { get; set; } = isSubscribed;
    }

    public class WorkoutsViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "workouts";
        public IScreen HostScreen { get; }

        [Reactive] public string SearchQuery { get; set; }
        [Reactive] public bool IsBusy { get; set; }
        [Reactive] public ObservableCollection<Workout> MyWorkouts { get; set; }
        [Reactive] public ObservableCollection<WorkoutViewModel> RecommendedWorkouts { get; set; }

        [Reactive] public bool IsMyWorkoutsExpanded { get; set; } = true;
        [Reactive] public bool IsRecommendedWorkoutsExpanded { get; set; } = true;

        private List<Workout> _allMyWorkouts = new();

        private List<Workout> _allRecommendedWorkouts = new();
        public ReactiveCommand<Unit, Unit> ToggleMyWorkoutsExpandCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleRecommendedWorkoutsExpandCommand { get; }

        public ReactiveCommand<Unit, Unit> CreateWorkoutCommand { get; }
        public ReactiveCommand<Workout, Unit> EditWorkoutCommand { get; }
        public ReactiveCommand<Workout, Unit> DeleteWorkoutCommand { get; }
        public ReactiveCommand<WorkoutViewModel, Unit> SubscribeWorkoutCommand { get; }
        public ReactiveCommand<WorkoutViewModel, Unit> ViewWorkoutCommand { get; }

        public ReactiveCommand<Unit, Unit> NavigateHomeCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateFavoritesCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateProfileCommand { get; }

        public ReactiveCommand<int, Unit> GoToAuthorProfileCommand { get; }

        private readonly ApplicationDbContext _context;
        private readonly User? _currentUser;
        private readonly int _currentUserId;

        public WorkoutsViewModel(ApplicationDbContext context, IScreen hostScreen, int userId)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            HostScreen = hostScreen;
            var userService = Locator.Current.GetService<UserService>() ??
                              throw new NullReferenceException("UserService is not registered.");

            _currentUser = userService.GetUserById(userId) ??
                           throw new NullReferenceException("Current user not found.");
            _currentUserId = _currentUser.UserId;

            MyWorkouts = new ObservableCollection<Workout>();
            RecommendedWorkouts = new ObservableCollection<WorkoutViewModel>();

            CreateWorkoutCommand = ReactiveCommand.CreateFromTask(CreateWorkoutAsync);
            EditWorkoutCommand = ReactiveCommand.CreateFromTask<Workout>(EditWorkoutAsync);
            DeleteWorkoutCommand = ReactiveCommand.CreateFromTask<Workout>(DeleteWorkoutAsync);
            SubscribeWorkoutCommand = ReactiveCommand.CreateFromTask<WorkoutViewModel>(async workoutViewModel =>
            {
                await SubscribeWorkoutAsync(workoutViewModel);
            });

            ViewWorkoutCommand = ReactiveCommand.CreateFromTask<WorkoutViewModel>(ViewWorkoutAsync); // Инициализация

            NavigateHomeCommand = ReactiveCommand.CreateFromTask(NavigateHomeAsync);
            NavigateFavoritesCommand = ReactiveCommand.CreateFromTask(NavigateFavoritesAsync);
            NavigateProfileCommand = ReactiveCommand.CreateFromTask(NavigateProfileAsync);
            GoToAuthorProfileCommand = ReactiveCommand.CreateFromTask<int>(GoToAuthorProfile);

            ToggleMyWorkoutsExpandCommand = ReactiveCommand.Create(() =>
            {
                IsMyWorkoutsExpanded = !IsMyWorkoutsExpanded;
            });

            ToggleRecommendedWorkoutsExpandCommand = ReactiveCommand.Create(() =>
            {
                IsRecommendedWorkoutsExpanded = !IsRecommendedWorkoutsExpanded;
            });

            this.WhenAnyValue(x => x.SearchQuery)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(FilterWorkouts);

            LoadWorkoutsAsync();
            LoadRecommendedWorkoutsAsync();

            // this.WhenNavigatedTo(() =>
            // {
            //     LoadWorkoutsAsync();
            //     LoadRecommendedWorkoutsAsync();
            //     return Task.CompletedTask;
            // });
        }

        private async void LoadWorkoutsAsync()
        {
            IsBusy = true;
            try
            {
                var workouts = await _context.Workouts
                    .Where(w => w.AuthorId == _currentUserId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                _allMyWorkouts = workouts;
                MyWorkouts = new ObservableCollection<Workout>(workouts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке собственных тренировок: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GoToAuthorProfile(int authorId)
        {
            try
            {
                await HostScreen.Router.Navigate.Execute(new ProfileViewModel(_context, HostScreen, authorId,
                    _currentUser.UserId));
            }
            catch (Exception)
            {
                //
            }
        }


        private async void LoadRecommendedWorkoutsAsync()
        {
            IsBusy = true;
            try
            {
                var user = _currentUser;
                var userAge = user.Age ?? 25;
                var userWeight = user.Weight ?? 70;
                var userHeight = user.Height ?? 170;
                var userPhysicalActivityLevel = user.PhysicalActivityLevel ?? "Середній";

                var userGoals = !string.IsNullOrEmpty(user.Goals)
                    ? user.Goals.Split(',').Select(g => g.Trim()).ToList()
                    : new List<string>();

                var userPreferredTrainingTypes = !string.IsNullOrEmpty(user.PreferredTrainingType)
                    ? user.PreferredTrainingType.Split(',').Select(t => t.Trim()).ToList()
                    : new List<string>();

                decimal bmi = 0;
                if (userWeight > 0 && userHeight > 0)
                {
                    var heightInMeters = userHeight / 100m;
                    bmi = userWeight / (heightInMeters * heightInMeters);
                }

                bool isOverweight = bmi >= 25;

                string preferredIntensity;
                if (userAge < 30)
                    preferredIntensity = "висока";
                else if (userAge >= 30 && userAge <= 50)
                    preferredIntensity = "середня";
                else
                    preferredIntensity = "низька";

                var userWorkoutIds = await _context.UserWorkouts
                    .Where(uw => uw.UserId == _currentUserId)
                    .Select(uw => uw.WorkoutId)
                    .ToListAsync();

                var highRatedWorkoutIds = await _context.WorkoutReviews
                    .Where(wr => wr.UserId == _currentUserId && wr.Rating >= 4)
                    .Select(wr => wr.WorkoutId.Value)
                    .ToListAsync();

                var preferredWorkoutIds = userWorkoutIds
                    .Concat(highRatedWorkoutIds)
                    .Distinct()
                    .ToList();

                var userWorkouts = await _context.Workouts
                    .Where(w => preferredWorkoutIds.Contains(w.WorkoutId))
                    .ToListAsync();

                var preferredTags = userWorkouts
                    .Where(w => !string.IsNullOrEmpty(w.Tags))
                    .SelectMany(w => w.Tags.Split(',').Select(t => t.Trim()))
                    .Distinct()
                    .ToList();

                var preferredEquipment = userWorkouts
                    .Where(w => !string.IsNullOrEmpty(w.Equipment))
                    .SelectMany(w => w.Equipment.Split(',').Select(e => e.Trim()))
                    .Distinct()
                    .ToList();

                var preferredMuscleGroups = userWorkouts
                    .Where(w => !string.IsNullOrEmpty(w.MuscleGroups))
                    .SelectMany(w => w.MuscleGroups.Split(',').Select(m => m.Trim()))
                    .Distinct()
                    .ToList();

                var preferredLevels = userWorkouts
                    .Where(w => !string.IsNullOrEmpty(w.Level))
                    .Select(w => w.Level.Trim())
                    .Distinct()
                    .ToList();

                var preferredWorkoutGoals = userWorkouts
                    .Where(w => !string.IsNullOrEmpty(w.Goals))
                    .SelectMany(w => w.Goals.Split(',').Select(g => g.Trim()))
                    .Distinct()
                    .ToList();

                var preferredWorkoutTypes = userWorkouts
                    .Where(w => !string.IsNullOrEmpty(w.WorkoutTypes))
                    .SelectMany(w => w.WorkoutTypes.Split(',').Select(t => t.Trim()))
                    .Distinct()
                    .ToList();

                var recommendedWorkouts = await _context.Workouts
                    .Where(w => w.AuthorId != _currentUserId)
                    .AsNoTracking()
                    .ToListAsync();

                recommendedWorkouts = recommendedWorkouts
                    .Where(w => !preferredWorkoutIds.Contains(w.WorkoutId))
                    .ToList();

                var scoredWorkouts = recommendedWorkouts.Select(w =>
                {
                    int score = 0;

                    var workoutGoals = !string.IsNullOrEmpty(w.Goals)
                        ? w.Goals.Split(',').Select(g => g.Trim()).ToList()
                        : new List<string>();

                    var workoutTypes = !string.IsNullOrEmpty(w.WorkoutTypes)
                        ? w.WorkoutTypes.Split(',').Select(t => t.Trim()).ToList()
                        : new List<string>();

                    if (userGoals.Any() && workoutGoals.Any())
                    {
                        int goalMatches = userGoals.Intersect(workoutGoals).Count();
                        score += goalMatches * 10;
                    }

                    if (userPreferredTrainingTypes.Any() && workoutTypes.Any())
                    {
                        int typeMatches = userPreferredTrainingTypes.Intersect(workoutTypes).Count();
                        score += typeMatches * 8;
                    }

                    if (preferredWorkoutGoals.Any() && workoutGoals.Any())
                    {
                        int goalMatches = preferredWorkoutGoals.Intersect(workoutGoals).Count();
                        score += goalMatches * 5;
                    }

                    if (preferredWorkoutTypes.Any() && workoutTypes.Any())
                    {
                        int typeMatches = preferredWorkoutTypes.Intersect(workoutTypes).Count();
                        score += typeMatches * 5;
                    }

                    if (!string.IsNullOrEmpty(w.Tags))
                    {
                        var workoutTags = w.Tags.Split(',').Select(t => t.Trim());
                        score += workoutTags.Count(t => preferredTags.Contains(t)) * 3;
                    }

                    if (!string.IsNullOrEmpty(w.Equipment))
                    {
                        var workoutEquipment = w.Equipment.Split(',').Select(e => e.Trim());
                        score += workoutEquipment.Count(e => preferredEquipment.Contains(e)) * 2;
                    }

                    if (!string.IsNullOrEmpty(w.MuscleGroups))
                    {
                        var workoutMuscles = w.MuscleGroups.Split(',').Select(m => m.Trim());
                        score += workoutMuscles.Count(m => preferredMuscleGroups.Contains(m)) * 3;
                    }

                    if (!string.IsNullOrEmpty(w.Level) && preferredLevels.Contains(w.Level.Trim()))
                        score += 4;

                    if (w.Intensity == preferredIntensity)
                        score += 5;

                    if (isOverweight && workoutGoals.Contains("Втрата ваги"))
                        score += 5;

                    if (!string.IsNullOrEmpty(userPhysicalActivityLevel) && w.Duration.HasValue)
                    {
                        if (userPhysicalActivityLevel == "Високий" && w.Duration >= 60)
                            score += 3;
                        else if (userPhysicalActivityLevel == "Середній" && w.Duration >= 30 && w.Duration < 60)
                            score += 2;
                        else if (userPhysicalActivityLevel == "Низький" && w.Duration < 30)
                            score += 1;
                    }

                    return new { Workout = w, Score = score };
                }).ToList();

                var sortedRecommendedWorkouts = scoredWorkouts
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Workout)
                    .ToList();

                _allRecommendedWorkouts = sortedRecommendedWorkouts;
                
                var allWorkouts = _context.Workouts.Include(workout => workout.Author).ToList();
            
                foreach (var workout in sortedRecommendedWorkouts.ToList())
                {
                    workout.Author = allWorkouts.First(w => w.WorkoutId == workout.WorkoutId).Author;
                }

                RecommendedWorkouts = new ObservableCollection<WorkoutViewModel>(
                    sortedRecommendedWorkouts.Select(w =>
                        new WorkoutViewModel(w,
                            _context.UserWorkouts.Any(uw => uw.UserId == _currentUserId && uw.WorkoutId == w.WorkoutId))
                    )
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні рекомендованих тренувань: {ex.Message}");
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
                MyWorkouts = new ObservableCollection<Workout>(_allMyWorkouts);
                RecommendedWorkouts = new ObservableCollection<WorkoutViewModel>(
                    _allRecommendedWorkouts.Select(w => new WorkoutViewModel(w,
                        _context.UserWorkouts.Any(uw => uw.UserId == _currentUserId && uw.WorkoutId == w.WorkoutId)))
                );
                return;
            }

            // Приводим запрос к нижнему регистру для нечувствительного поиска
            var lowerQuery = query.ToLower();

            // Фильтрация и оценка собственных тренировок
            var filteredMyWorkouts = _allMyWorkouts
                .Select(w => new
                {
                    Workout = w,
                    Score = CalculateScore(w, lowerQuery)
                })
                .Where(w => w.Score > 0) // Отфильтровываем тренировки без совпадений
                .OrderByDescending(w => w.Score)
                .ThenByDescending(w => w.Workout.CreatedAt)
                .Select(w => w.Workout)
                .ToList();

            MyWorkouts = new ObservableCollection<Workout>(filteredMyWorkouts);

            // Фильтрация и оценка рекомендованных тренировок
            var filteredRecommendedWorkouts = _allRecommendedWorkouts
                .Select(w => new
                {
                    Workout = w,
                    Score = CalculateScore(w, lowerQuery)
                })
                .Where(w => w.Score > 0) // Отфильтровываем тренировки без совпадений
                .OrderByDescending(w => w.Score)
                .ThenByDescending(w => w.Workout.AverageRating)
                .ThenByDescending(w => w.Workout.Popularity)
                .Select(w => new WorkoutViewModel(w.Workout,
                    _context.UserWorkouts.Any(uw =>
                        uw.UserId == _currentUserId && uw.WorkoutId == w.Workout.WorkoutId)))
                .ToList();
            
            RecommendedWorkouts = new ObservableCollection<WorkoutViewModel>(filteredRecommendedWorkouts);
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


        private async Task NavigateHomeAsync()
        {
            await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, _currentUser.Email));
        }


        private async Task NavigateFavoritesAsync()
        {
            await HostScreen.Router.Navigate.Execute(new SubscribedWorkoutsViewModel(_context, HostScreen,
                _currentUser.UserId));
        }


        private async Task NavigateProfileAsync()
        {
            await HostScreen.Router.Navigate.Execute(new ProfileViewModel(_context, HostScreen, _currentUser.UserId,
                _currentUser.UserId));
        }

        private async Task NavigateWorkoutsAsync()
        {
        }

        private async Task CreateWorkoutAsync()
        {
            await HostScreen.Router.Navigate.Execute(new CreateWorkoutViewModel(_context, HostScreen,
                _currentUser.UserId));
        }

        private async Task EditWorkoutAsync(Workout workout)
        {
            if (workout == null)
                return;

            try
            {
                await HostScreen.Router.Navigate.Execute(new CreateWorkoutViewModel(_context, HostScreen,
                    _currentUser.UserId, workout));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task DeleteWorkoutAsync(Workout workout)
        {
            if (workout is null)
                return;

            var confirm = await ConfirmDeletionAsync(workout.Name);
            if (!confirm)
                return;

            try
            {
                _context.Workouts.Remove(workout);
                await _context.SaveChangesAsync();

                MyWorkouts.Remove(workout);
            }
            catch (Exception ex)
            {
            }
        }

        private async Task SubscribeWorkoutAsync(WorkoutViewModel workoutViewModel)
        {
            if (workoutViewModel == null || workoutViewModel.IsSubscribed)
                return;

            try
            {
                var userWorkout = new UserWorkout
                {
                    UserId = _currentUserId,
                    WorkoutId = workoutViewModel.Workout.WorkoutId,
                    AddedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                _context.UserWorkouts.Add(userWorkout);
                workoutViewModel.Workout.Popularity += 1;
                workoutViewModel.IsSubscribed = true;

                await _context.SaveChangesAsync();
                RecommendedWorkouts.Remove(workoutViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подписки на тренировку: {ex.Message}");
            }
        }

        private async Task ViewWorkoutAsync(WorkoutViewModel workout)
        {
            if (workout == null)
                return;

            try
            {
                await HostScreen.Router.Navigate.Execute(new WorkoutDetailsViewModel(HostScreen, _context,
                    workout.Workout, _currentUserId));
            }
            catch (Exception ex)
            {
            }
        }

        private async Task<bool> ConfirmDeletionAsync(string workoutName)
        {
            var isConfirmed = await Application.Current.MainPage.DisplayAlert(
                "Підтвердження",
                $"Ви впевнені, що хочете видалити тренування \"{workoutName}\"?",
                "Так",
                "Ні"
            );

            return isConfirmed;
        }
    }
} 