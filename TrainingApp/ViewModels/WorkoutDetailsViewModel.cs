using System.Collections.ObjectModel;
using System.Reactive;
using DataAccess.Database;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TrainingApp.Models;

namespace TrainingApp.ViewModels
{
    public class ReviewViewModel : ReactiveObject
    {
        [Reactive] public string Username { get; set; }
        [Reactive] public int? Rating { get; set; }
        [Reactive] public string? Comment { get; set; }
        [Reactive] public DateTime CreatedAt { get; set; }
    }

    public class WorkoutDetailsViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "workout-details";
        public IScreen HostScreen { get; }

        [Reactive] public string Name { get; private set; }
        [Reactive] public string Description { get; private set; }
        [Reactive] public string Duration { get; private set; }
        [Reactive] public string SelectedIntensity { get; private set; }
        [Reactive] public string SelectedLevel { get; private set; }
        [Reactive] public string ScheduleInterval { get; private set; }
        [Reactive] public string SelectedScheduleUnit { get; private set; }
        [Reactive] public string CaloriesBurned { get; private set; }
        [Reactive] public string FormattedSelectedTags { get; private set; }
        [Reactive] public string FormattedSelectedMuscleGroups { get; private set; }
        [Reactive] public string FormattedSelectedWorkoutTypes { get; private set; }
        [Reactive] public string FormattedSelectedGoals { get; private set; }

        // Новые свойства для отзыва
        [Reactive] public int? SelectedRating { get; set; }
        [Reactive] public string? Comment { get; set; }
        [Reactive] public int CurrentWorkoutId { get; private set; }

        // Список возможных оценок
        public ObservableCollection<int> RatingOptions { get; }

        // Команда для отправки отзыва
        public ReactiveCommand<Unit, Unit> SubmitReviewCommand { get; }

        // Свойство для проверки валидности отзыва
        public bool IsReviewValid => SelectedRating.HasValue || !string.IsNullOrWhiteSpace(Comment);

        public ObservableCollection<string> ScheduleUnits { get; }
        public ObservableCollection<string> Intensities { get; }
        public ObservableCollection<string> Levels { get; }

        public ObservableCollection<Tag> AvailableTags { get; }
        public ObservableCollection<Tag> SelectedTags { get; }

        public ObservableCollection<EquipmentItem> AvailableEquipment { get; }
        public ObservableCollection<EquipmentItem> SelectedEquipment { get; }

        public ObservableCollection<MuscleGroupItem> AvailableMuscleGroups { get; }
        public ObservableCollection<SelectableItem> AvailableWorkoutTypes { get; }
        public ObservableCollection<SelectableItem> AvailableGoals { get; }

        public ObservableCollection<ExerciseDetailsViewModel> Exercises { get; }
        
        [Reactive] public ObservableCollection<ReviewViewModel> Reviews { get; set; }

        // Команда для загрузки отзывов
        public ReactiveCommand<Unit, Unit> LoadReviewsCommand { get; }
        private readonly ApplicationDbContext _context;
        private readonly int _currentUserId;

        public WorkoutDetailsViewModel(IScreen hostScreen, ApplicationDbContext context, Workout workout, int userId)
        {
            HostScreen = hostScreen;
            _context = context;
            _currentUserId = userId;
            
            Name = !string.IsNullOrWhiteSpace(workout.Name) ? workout.Name : "Не вказано";
            Description = !string.IsNullOrWhiteSpace(workout.Description) ? workout.Description : "Не вказано";
            Duration = workout.Duration.HasValue ? workout.Duration.Value.ToString() : "Не вказано";
            SelectedIntensity = !string.IsNullOrWhiteSpace(workout.Intensity) ? workout.Intensity : "Не вказано";
            SelectedLevel = !string.IsNullOrWhiteSpace(workout.Level) ? workout.Level : "Не вказано";
            SelectedScheduleUnit = !string.IsNullOrWhiteSpace(ExtractUnit(workout.Schedule)) ? ExtractUnit(workout.Schedule) : "Не вказано";
            ScheduleInterval = !string.IsNullOrWhiteSpace(ExtractInterval(workout.Schedule)) ? ExtractInterval(workout.Schedule) : "Не вказано";
            CaloriesBurned = workout.CaloriesBurned.HasValue ? workout.CaloriesBurned.Value.ToString() : "Не вказано";

            CurrentWorkoutId = workout.WorkoutId;
            
            ScheduleUnits = new ObservableCollection<string>
            {
                "годин",
                "днів"
            };

            Intensities = new ObservableCollection<string>
            {
                "низька",
                "середня",
                "висока"
            };

            Levels = new ObservableCollection<string>
            {
                "початковий",
                "середній",
                "високий"
            };

            AvailableTags = new ObservableCollection<Tag>(
                workout.Tags?.Split(", ").Select(name => new Tag { Name = name }) ??
                new List<Tag>());
            SelectedTags = new ObservableCollection<Tag>(AvailableTags);

            AvailableEquipment = new ObservableCollection<EquipmentItem>(
                workout.Equipment?.Split(", ").Select(name => new EquipmentItem { Name = name, IsSelected = true }) ??
                new List<EquipmentItem>()
            );
            SelectedEquipment = new ObservableCollection<EquipmentItem>(AvailableEquipment.Where(e => e.IsSelected));

            AvailableMuscleGroups = new ObservableCollection<MuscleGroupItem>(
                workout.MuscleGroups?.Split(", ").Select(name => new MuscleGroupItem(name)) ??
                new List<MuscleGroupItem>()
            );

            AvailableWorkoutTypes = new ObservableCollection<SelectableItem>(
                workout.WorkoutTypes?.Split(", ").Select(name => new SelectableItem(name) { IsSelected = true }) ??
                new List<SelectableItem>()
            );

            AvailableGoals = new ObservableCollection<SelectableItem>(
                workout.Goals?.Split(", ").Select(name => new SelectableItem(name) { IsSelected = true }) ??
                new List<SelectableItem>()
            );

            var exercises = _context.WorkoutExercises
                .Where(we => we.WorkoutId == workout.WorkoutId)
                .Select(we => we.Exercise)
                .ToList();
            
            FormattedSelectedTags = SelectedTags.Any()
                ? string.Join(", ", SelectedTags.Select(t => t.Name))
                : "Не вказано";

            FormattedSelectedMuscleGroups = AvailableMuscleGroups.Any(m => m.IsSelected)
                ? string.Join(", ", AvailableMuscleGroups.Where(m => m.IsSelected).Select(m => m.Name))
                : "Не вказано";

            FormattedSelectedWorkoutTypes = AvailableWorkoutTypes.Any(wt => wt.IsSelected)
                ? string.Join(", ", AvailableWorkoutTypes.Where(wt => wt.IsSelected).Select(wt => wt.Name))
                : "Не вказано";

            FormattedSelectedGoals = AvailableGoals.Any(g => g.IsSelected)
                ? string.Join(", ", AvailableGoals.Where(g => g.IsSelected).Select(g => g.Name))
                : "Не вказано";

            Exercises = new ObservableCollection<ExerciseDetailsViewModel>(
                exercises.Select(e => new ExerciseDetailsViewModel(_context, e)) ?? new List<ExerciseDetailsViewModel>()
            );
            
            Reviews = new ObservableCollection<ReviewViewModel>();

            // Инициализация команды загрузки отзывов
            LoadReviewsCommand = ReactiveCommand.CreateFromTask(LoadReviewsAsync);
            LoadReviewsCommand.Execute().Subscribe();
            
            RatingOptions = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            
            var canSubmit = this.WhenAnyValue(
                vm => vm.SelectedRating,
                vm => vm.Comment,
                (rating, comment) => rating.HasValue || !string.IsNullOrWhiteSpace(comment));

            SubmitReviewCommand = ReactiveCommand.CreateFromTask(SubmitReviewAsync, canSubmit);
            
            this.WhenAnyValue(vm => vm.SelectedRating, vm => vm.Comment)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsReviewValid)));
        }
        
        private async Task LoadReviewsAsync()
        {
            var reviews = await _context.WorkoutReviews
                .Where(r => r.WorkoutId == CurrentWorkoutId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            Reviews.Clear();
            
            foreach (var review in reviews)
            {
                Reviews.Add(new ReviewViewModel
                {
                    Username = $"{review.User?.FirstName} {review.User?.LastName}",
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt.Value,
                });
            }
        }

        private string ExtractInterval(string schedule)
        {
            var parts = schedule.Split(' ');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int interval))
                return interval.ToString();
            return string.Empty;
        }

        private string ExtractUnit(string schedule)
        {
            var parts = schedule.Split(' ');
            if (parts.Length >= 4)
                return parts[3];
            return string.Empty;
        }

        private async Task SubmitReviewAsync()
        {
            try
            {
                if (SelectedRating == null && string.IsNullOrWhiteSpace(Comment))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення", "Оцініть тренування або напишіть коментар", "OK");
                    return;
                }

                var existingReview = await _context.WorkoutReviews
                    .FirstOrDefaultAsync(r => r.WorkoutId == CurrentWorkoutId && r.UserId == _currentUserId);

                if (existingReview != null)
                {
                    existingReview.Rating = SelectedRating;
                    existingReview.Comment = Comment;
                    existingReview.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                }
                else
                {
                    var review = new WorkoutReview
                    {
                        WorkoutId = CurrentWorkoutId,
                        UserId = _currentUserId,
                        Rating = SelectedRating,
                        Comment = Comment,
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                    };

                    _context.WorkoutReviews.Add(review);
                }

                await _context.SaveChangesAsync();
            
                await LoadReviewsAsync();
            
                var allReviews = await _context.WorkoutReviews
                    .Where(r => r.WorkoutId == CurrentWorkoutId)
                    .ToListAsync();

                var average = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
                var workout = await _context.Workouts.FindAsync(CurrentWorkoutId);
                if (workout != null)
                {
                    workout.AverageRating = Math.Round((decimal)average.Value, 2);
                }

                await _context.SaveChangesAsync();

                SelectedRating = null;
                Comment = string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            await Application.Current.MainPage.DisplayAlert("Повідомлення", "Відгук відправлено", "OK");
        }
    }
}
