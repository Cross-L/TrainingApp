using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using DataAccess.Database;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using TrainingApp.Models;
using TrainingApp.Services;

namespace TrainingApp.ViewModels
{
    public class CreateWorkoutViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "create-workout";
        public IScreen HostScreen { get; }

        [Reactive] public string HeaderText { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public string Duration { get; set; }
        [Reactive] public string SelectedIntensity { get; set; }
        [Reactive] public string SelectedLevel { get; set; }
        [Reactive] public string ScheduleInterval { get; set; }
        [Reactive] public string SelectedScheduleUnit { get; set; }

        public ObservableCollection<string> ScheduleUnits { get; }

        [Reactive] public ObservableCollection<Tag> AvailableTags { get; set; }
        [Reactive] public ObservableCollection<Tag> SelectedTags { get; set; }
        [Reactive] public string NewTagName { get; set; }
        [Reactive] public string FormattedSelectedTags { get; private set; }
        public ObservableCollection<string> Intensities { get; }
        public ObservableCollection<string> Levels { get; }
        public ObservableCollection<ExerciseViewModel> Exercises { get; set; }

        [Reactive] public string NewEquipmentName { get; set; }

        public ReactiveCommand<Unit, Unit> AddEquipmentCommand { get; }

        [Reactive] public ObservableCollection<EquipmentItem> AvailableEquipment { get; set; }
        [Reactive] public ObservableCollection<EquipmentItem> SelectedEquipment { get; set; }
        [Reactive] public string CaloriesBurned { get; set; }

        [Reactive] public ObservableCollection<MuscleGroupItem> AvailableMuscleGroups { get; set; }
        [Reactive] public string FormattedSelectedMuscleGroups { get; private set; }
        
        [Reactive] public ObservableCollection<SelectableItem> AvailableWorkoutTypes { get; set; }
        [Reactive] public ObservableCollection<SelectableItem> AvailableGoals { get; set; }

        [Reactive] public string FormattedSelectedWorkoutTypes { get; private set; }
        [Reactive] public string FormattedSelectedGoals { get; private set; }

        public ReactiveCommand<Unit, Unit> SaveWorkoutCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Tag, Unit> ToggleTagSelectionCommand { get; }
        public ReactiveCommand<Unit, Unit> AddTagCommand { get; }
        public ReactiveCommand<Unit, Unit> AddExerciseCommand { get; }
        public ReactiveCommand<ExerciseViewModel, Unit> RemoveExerciseCommand { get; }

        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly int _currentUserId;
        private readonly Workout? _workoutToEdit;

        public CreateWorkoutViewModel(ApplicationDbContext context, IScreen hostScreen, int userId, Workout? workout = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            HostScreen = hostScreen;
            _userService = Locator.Current.GetService<UserService>() ?? throw new NullReferenceException("UserService is not registered.");
            var user = _userService.GetUserById(userId) ?? throw new NullReferenceException("Current user not found.");
            _currentUserId = user.UserId;
            _workoutToEdit = workout;

            HeaderText = _workoutToEdit != null ? "Редагування тренування" : "Створення тренування";

            AvailableTags = new ObservableCollection<Tag>();
            SelectedTags = new ObservableCollection<Tag>();
            Exercises = new ObservableCollection<ExerciseViewModel>();
            FormattedSelectedTags = string.Empty;

            AvailableMuscleGroups = new ObservableCollection<MuscleGroupItem>
            {
                new MuscleGroupItem("Груди"),
                new MuscleGroupItem("Спина"),
                new MuscleGroupItem("Ноги"),
                new MuscleGroupItem("Плечі"),
                new MuscleGroupItem("Біцепс"),
                new MuscleGroupItem("Трицепс"),
                new MuscleGroupItem("Прес"),
                new MuscleGroupItem("Інше")
            };
            FormattedSelectedMuscleGroups = string.Empty;
            

            ScheduleUnits = new ObservableCollection<string>
            {
                "годин",
                "днів"
            };

            AvailableWorkoutTypes = new ObservableCollection<SelectableItem>(
                new List<SelectableItem>
                {
                    new SelectableItem("Силові тренування"),
                    new SelectableItem("Кардіо"),
                    new SelectableItem("Йога"),
                    new SelectableItem("Пілатес"),
                    new SelectableItem("Функціональний тренінг"),
                    new SelectableItem("Високоінтенсивні тренування (HIIT)"),
                    new SelectableItem("Розтяжка"),
                    new SelectableItem("Бойові мистецтва"),
                    new SelectableItem("Плавання"),
                    new SelectableItem("Велоспорт"),
                    new SelectableItem("Реабілітаційні тренування"),
                    new SelectableItem("Інше")
                }
            );

            // Ініціалізація AvailableGoals
            AvailableGoals = new ObservableCollection<SelectableItem>(
                new List<SelectableItem>
                {
                    new SelectableItem("Втрата ваги"),
                    new SelectableItem("Набір м'язової маси"),
                    new SelectableItem("Покращення витривалості"),
                    new SelectableItem("Релаксація"),
                    new SelectableItem("Загальне здоров'я"),
                    new SelectableItem("Інше")
                }
            );

            // Підписка на зміни вибору для WorkoutTypes
            foreach (var workoutType in AvailableWorkoutTypes)
            {
                workoutType.WhenAnyValue(x => x.IsSelected)
                    .Subscribe(_ => UpdateFormattedSelectedWorkoutTypes());
            }

            // Підписка на зміни вибору для Goals
            foreach (var goal in AvailableGoals)
            {
                goal.WhenAnyValue(x => x.IsSelected)
                    .Subscribe(_ => UpdateFormattedSelectedGoals());
            }

            FormattedSelectedWorkoutTypes = string.Empty;
            FormattedSelectedGoals = string.Empty;

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

            AvailableEquipment = new ObservableCollection<EquipmentItem>
            {
                new() { Name = "Штанга" },
                new() { Name = "Гантелі" },
                new() { Name = "Вага власного тіла" },
                new() { Name = "Кросівка" },
                new() { Name = "Блоки TRX" },
                new() { Name = "Скакалка" },
                new() { Name = "Медичний м'яч" },
                new() { Name = "Кетлбелл" },
                new() { Name = "Гімнастичний м'яч" },
                new() { Name = "Петлі для підйому" },
                new() { Name = "Еспандери" },
                new() { Name = "Тренажер для преса" },
                new() { Name = "Вертоліт (або турнік)" },
                new() { Name = "Силова рама" },
                new() { Name = "Тренажер для ніг" },
                new() { Name = "Бігова доріжка" },
                new() { Name = "Велотренажер" },
                new() { Name = "Грібний тренажер" },
                new() { Name = "Балансувальна платформа" },
                new() { Name = "Ролик для преса" },
                new() { Name = "Паралельні бруси" },
                new() { Name = "Жимова платформа" }
            };

            SelectedEquipment = new ObservableCollection<EquipmentItem>();
            AvailableEquipment.CollectionChanged += AvailableEquipment_CollectionChanged;

            foreach (var item in AvailableEquipment)
            {
                item.WhenAnyValue(x => x.IsSelected)
                    .Subscribe(_ => OnEquipmentItemSelectionChanged(item));
            }

            var canAddEquipment = this.WhenAnyValue(x => x.NewEquipmentName)
                .Select(name => !string.IsNullOrWhiteSpace(name) && !AvailableEquipment.Any(e => e.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)));

            AddEquipmentCommand = ReactiveCommand.CreateFromTask(AddEquipmentAsync, canAddEquipment);

            var canSave = CanSaveWorkout();
            SaveWorkoutCommand = ReactiveCommand.CreateFromTask(SaveWorkoutAsync, canSave);
            CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync);
            AddTagCommand = ReactiveCommand.CreateFromTask(AddTagAsync, this.WhenAnyValue(x => x.CanAddTag));
            AddExerciseCommand = ReactiveCommand.Create(AddExercise);
            RemoveExerciseCommand = ReactiveCommand.Create<ExerciseViewModel>(RemoveExercise);
            ToggleTagSelectionCommand = ReactiveCommand.Create<Tag>(ToggleTagSelection);

            this.WhenAnyValue(x => x.NewTagName)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(CanAddTag)));

            SelectedTags.CollectionChanged += (s, e) => UpdateFormattedSelectedTags();

            AvailableMuscleGroups.CollectionChanged += (s, e) => UpdateFormattedSelectedMuscleGroups();
            foreach (var muscleGroup in AvailableMuscleGroups)
            {
                muscleGroup.WhenAnyValue(m => m.IsSelected)
                    .Subscribe(_ => UpdateFormattedSelectedMuscleGroups());
            }

            LoadAvailableTagsAsync();

            if (_workoutToEdit != null)
            {
                InitializeForEdit(_workoutToEdit);
            }
        }

        private void AvailableEquipment_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (EquipmentItem item in e.NewItems)
                {
                    item.WhenAnyValue(x => x.IsSelected)
                        .Subscribe(_ => OnEquipmentItemSelectionChanged(item));
                }
            }
        }
        
        private void UpdateFormattedSelectedWorkoutTypes()
        {
            FormattedSelectedWorkoutTypes = string.Join(", ", AvailableWorkoutTypes
                .Where(wt => wt.IsSelected)
                .Select(wt => wt.Name));
            this.RaisePropertyChanged(nameof(FormattedSelectedWorkoutTypes));
        }
        
        private void UpdateFormattedSelectedGoals()
        {
            FormattedSelectedGoals = string.Join(", ", AvailableGoals
                .Where(g => g.IsSelected)
                .Select(g => g.Name));
            this.RaisePropertyChanged(nameof(FormattedSelectedGoals));
        }

        private void OnEquipmentItemSelectionChanged(EquipmentItem item)
        {
            if (item.IsSelected)
            {
                if (!SelectedEquipment.Contains(item))
                    SelectedEquipment.Add(item);
            }
            else
            {
                if (SelectedEquipment.Contains(item))
                    SelectedEquipment.Remove(item);
            }
        }

        private async Task AddEquipmentAsync()
        {
            var trimmedName = NewEquipmentName.Trim();
            if (string.IsNullOrEmpty(trimmedName))
            {
                await Application.Current.MainPage.DisplayAlert("Помилка", "Назва обладнання не може бути порожньою.", "OK");
                return;
            }

            if (AvailableEquipment.Any(e => e.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                await Application.Current.MainPage.DisplayAlert("Помилка", "Обладнання з такою назвою вже існує.", "OK");
                return;
            }

            var newEquipmentItem = new EquipmentItem { Name = trimmedName };
            AvailableEquipment.Add(newEquipmentItem);
            NewEquipmentName = string.Empty;

            newEquipmentItem.WhenAnyValue(x => x.IsSelected)
                .Subscribe(_ => OnEquipmentItemSelectionChanged(newEquipmentItem));
        }

        private void UpdateFormattedSelectedTags()
        {
            FormattedSelectedTags = string.Join(", ", SelectedTags.Select(tag => tag.Name));
            this.RaisePropertyChanged(nameof(FormattedSelectedTags));
        }

        private void UpdateFormattedSelectedMuscleGroups()
        {
            FormattedSelectedMuscleGroups = string.Join(", ", AvailableMuscleGroups
                .Where(m => m.IsSelected)
                .Select(m => m.Name));
            this.RaisePropertyChanged(nameof(FormattedSelectedMuscleGroups));
        }

        private void ToggleTagSelection(Tag tag)
        {
            if (tag == null) return;

            if (SelectedTags.Contains(tag))
            {
                SelectedTags.Remove(tag);
            }
            else
            {
                SelectedTags.Add(tag);
            }
        }

        private async Task AddTagAsync()
        {
            try
            {
                var trimmedTagName = NewTagName.Trim();

                if (string.IsNullOrWhiteSpace(trimmedTagName))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Назва тегу не може бути порожньою.", "OK");
                    return;
                }

                if (AvailableTags.Any(t => t.Name.Equals(trimmedTagName, StringComparison.OrdinalIgnoreCase)))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення", "Тег з такою назвою вже існує.",
                        "OK");
                    return;
                }

                var newTag = new Tag { Name = trimmedTagName };
                _context.Tags.Add(newTag);
                await _context.SaveChangesAsync();

                AvailableTags.Add(newTag);
                NewTagName = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при додаванні тегу: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Помилка", "Не вдалося додати новий тег.", "OK");
            }
        }

        private void AddExercise()
        {
            var newExercise = new ExerciseViewModel(_currentUserId.ToString(), SelectedEquipment)
            {
                ExerciseNumber = Exercises.Count + 1,
            };
            Exercises.Add(newExercise);
        }

        private void RemoveExercise(ExerciseViewModel exercise)
        {
            Exercises.Remove(exercise);
            for (var i = 0; i < Exercises.Count; i++)
            {
                Exercises[i].ExerciseNumber = i + 1;
            }
        }

        private async Task SaveWorkoutAsync()
        {
            try
            {
                if (!Exercises.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Додайте хоча б одну вправу.", "OK");
                    return;
                }

                if (!int.TryParse(Duration, out int durationMinutes) || durationMinutes <= 0 || durationMinutes > 300)
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Тривалість тренування повинна бути від 1 до 300 хвилин.", "OK");
                    return;
                }

                var totalExerciseDuration = 0;
                int exerciseIndex = 1;
                foreach (var exerciseVm in Exercises)
                {
                    foreach (var equipment in exerciseVm.SelectedEquipment)
                    {
                        if (SelectedEquipment.All(e => e.Name != equipment))
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Повідомлення",
                                $"Вправа '{exerciseVm.Name}' використовує обладнання '{equipment}', яке не вибране для тренування.",
                                "OK"
                            );
                            return;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(exerciseVm.Name))
                    {
                        await Application.Current.MainPage.DisplayAlert("Повідомлення",
                            $"Вправа #{exerciseIndex}: Введіть назву вправи.", "OK");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(exerciseVm.DifficultyLevel))
                    {
                        await Application.Current.MainPage.DisplayAlert("Повідомлення",
                            $"Вправа '{exerciseVm.Name}': Виберіть рівень складності.", "OK");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(exerciseVm.ExerciseType))
                    {
                        await Application.Current.MainPage.DisplayAlert("Повідомлення",
                            $"Вправа '{exerciseVm.Name}': Виберіть тип вправи.", "OK");
                        return;
                    }

                    if (!int.TryParse(exerciseVm.Duration, out int exDuration) || exDuration <= 0 || exDuration > 300)
                    {
                        await Application.Current.MainPage.DisplayAlert("Повідомлення",
                            $"Вправа '{exerciseVm.Name}': Тривалість повинна бути від 1 до 300 хвилин.", "OK");
                        return;
                    }

                    totalExerciseDuration += exDuration;
                    exerciseIndex++;
                }

                if (!int.TryParse(ScheduleInterval, out int interval) || interval <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Інтервал періодичності повинен бути додатнім числом.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedScheduleUnit))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Будь ласка, виберіть одиницю часу для періодичності.", "OK");
                    return;
                }
                
                if (!SelectedEquipment.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Оберіть обладнання. Якщо воно не потрібне, оберіть вагу власного тіла.", "OK");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(FormattedSelectedGoals))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Виберіть хоча б одну ціль тренування.", "OK");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(FormattedSelectedWorkoutTypes))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Виберіть тип тренування.", "OK");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(SelectedScheduleUnit))
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Будь ласка, виберіть одиницю часу для періодичності.", "OK");
                    return;
                }

                if (!int.TryParse(CaloriesBurned, out int exCalories) || exCalories < 0 || exCalories > 10000)
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Спалювані калорії повинні бути цілим числом від 0 до 10000 ккал.", "OK");
                    return;
                }

                if (SelectedScheduleUnit == "днів" && interval > 14)
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Максимальна періодичність тренувань - раз на 14 днів.", "OK");
                    return;
                }
                else if (SelectedScheduleUnit == "годин" && interval > 336)
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Максимальна періодичність тренувань - раз на 336 годин.", "OK");
                    return;
                }

                string formattedSchedule = $"Раз на {ScheduleInterval} {SelectedScheduleUnit}";

                if (totalExerciseDuration > durationMinutes)
                {
                    await Application.Current.MainPage.DisplayAlert("Повідомлення",
                        "Сумарна тривалість усіх вправ перевищує тривалість тренування.", "OK");
                    return;
                }

                var driveService = await InitializeDriveServiceAsync();

                await using var transaction = await _context.Database.BeginTransactionAsync();
                List<string> uploadedFileIds = new List<string>();

                try
                {
                    Workout workout;

                    if (_workoutToEdit != null)
                    {
                        workout = await _context.Workouts
                            .Include(w => w.WorkoutExercises)
                            .FirstOrDefaultAsync(w => w.WorkoutId == _workoutToEdit.WorkoutId);

                        if (workout == null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Помилка", "Тренування не знайдено.", "OK");
                            return;
                        }

                        workout.Name = this.Name;
                        workout.Description = this.Description;
                        workout.Duration = durationMinutes;
                        workout.Intensity = this.SelectedIntensity;
                        workout.Level = this.SelectedLevel;
                        workout.Schedule = formattedSchedule;
                        workout.CaloriesBurned = exCalories;
                        workout.Tags = SelectedTags.Any() ? string.Join(", ", SelectedTags.Select(t => t.Name)) : string.Empty;
                        workout.MuscleGroups = AvailableMuscleGroups.Any(m => m.IsSelected)
                            ? string.Join(", ", AvailableMuscleGroups.Where(m => m.IsSelected).Select(m => m.Name))
                            : string.Empty;
                        workout.Equipment = SelectedEquipment.Any()
                            ? string.Join(", ", SelectedEquipment.Select(e => e.Name))
                            : string.Empty;
                        
                        workout.Goals = FormattedSelectedGoals;
                        workout.WorkoutTypes = FormattedSelectedWorkoutTypes;
                    }
                    else
                    {
                        workout = new Workout
                        {
                            Name = this.Name,
                            Description = this.Description,
                            Duration = durationMinutes,
                            Intensity = this.SelectedIntensity,
                            Schedule = formattedSchedule,
                            Level = this.SelectedLevel,
                            AuthorId = _currentUserId,
                            Equipment = SelectedEquipment.Any() ? string.Join(", ", SelectedEquipment.Select(e => e.Name)) : string.Empty,
                            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                            AverageRating = 0,
                            Popularity = 0,
                            MuscleGroups = AvailableMuscleGroups.Any(m => m.IsSelected)
                                ? string.Join(", ", AvailableMuscleGroups.Where(m => m.IsSelected).Select(m => m.Name))
                                : string.Empty,
                            CaloriesBurned = exCalories,
                            Tags = SelectedTags.Any() ? string.Join(", ", SelectedTags.Select(t => t.Name)) : string.Empty
                        };
                        workout.Goals = FormattedSelectedGoals;
                        workout.WorkoutTypes = FormattedSelectedWorkoutTypes;
                        
                        _context.Workouts.Add(workout);
                        await _context.SaveChangesAsync();
                    }

                    if (_workoutToEdit != null)
                    {
                        var existingExercises = _context.Exercises
                            .Include(e => e.WorkoutExercises)
                            .Include(e => e.ExerciseMedia)
                            .Where(e => e.WorkoutExercises.Any(we => we.WorkoutId == workout.WorkoutId))
                            .ToList();

                        var existingMediaFiles = existingExercises.SelectMany(e => e.ExerciseMedia).ToList();

                        // Видаляємо файли з Google Drive
                        foreach (var media in existingMediaFiles)
                        {
                            if (!string.IsNullOrEmpty(media.DriveFileId))
                            {
                                try
                                {
                                    await DeleteFileFromGoogleDriveAsync(driveService, media.DriveFileId);
                                }
                                catch
                                {
                                }
                            }
                        }

                        _context.WorkoutExercises.RemoveRange(workout.WorkoutExercises);
                        _context.ExerciseMedia.RemoveRange(existingMediaFiles);
                        _context.Exercises.RemoveRange(existingExercises);
                        await _context.SaveChangesAsync();
                    }

                    var sequence = 1;
                    foreach (var exerciseVm in Exercises)
                    {
                        var exercise = new Exercise
                        {
                            Name = exerciseVm.Name,
                            Description = exerciseVm.Description,
                            DifficultyLevel = exerciseVm.DifficultyLevel,
                            ExerciseType = exerciseVm.ExerciseType,
                            Duration = int.Parse(exerciseVm.Duration),
                            Reps = exerciseVm.Reps,
                            Sets = exerciseVm.Sets,
                            Precautions = exerciseVm.Precautions,
                            AuthorId = _currentUserId,
                            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                            AverageRating = 0,
                            Popularity = 0,
                            EquipmentNeeded = exerciseVm.SelectedEquipment.Any()
                                ? string.Join(", ", exerciseVm.SelectedEquipment)
                                : string.Empty
                        };

                        _context.Exercises.Add(exercise);
                        await _context.SaveChangesAsync();

                        var workoutExercise = new WorkoutExercise
                        {
                            WorkoutId = workout.WorkoutId,
                            ExerciseId = exercise.ExerciseId,
                            Sequence = sequence++,
                            Reps = exercise.Sets > 0 ? exercise.Reps : 0,
                            Sets = exercise.Sets,
                            RestTime = 60
                        };

                        _context.WorkoutExercises.Add(workoutExercise);

                        foreach (var mediaVm in exerciseVm.Media)
                        {
                            if (string.IsNullOrEmpty(mediaVm.DriveFileId) && !string.IsNullOrEmpty(mediaVm.MediaUrl))
                            {
                                var driveFileId = await UploadFileToGoogleDriveAsync(
                                    driveService,
                                    _currentUserId.ToString(),
                                    mediaVm.MediaUrl,
                                    Path.GetFileName(mediaVm.MediaUrl));

                                uploadedFileIds.Add(driveFileId);

                                mediaVm.DriveFileId = driveFileId;
                            }

                            var exerciseMedia = new ExerciseMedia
                            {
                                ExerciseId = exercise.ExerciseId,
                                MediaType = mediaVm.MediaType,
                                MediaUrl = mediaVm.MediaUrl,
                                DriveFileId = mediaVm.DriveFileId,
                                UploadedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                            };

                            _context.ExerciseMedia.Add(exerciseMedia);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    await Application.Current.MainPage.DisplayAlert("Повідомлення", "Тренування збережено.", "OK");
                    await HostScreen.Router.Navigate.Execute(new WorkoutsViewModel(_context, HostScreen, _currentUserId));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    foreach (var fileId in uploadedFileIds)
                    {
                        try
                        {
                            await DeleteFileFromGoogleDriveAsync(driveService, fileId);
                        }
                        catch
                        {
                           
                        }
                    }

                    Console.WriteLine($"Помилка при збереженні тренування: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Помилка", "Не вдалося зберегти тренування.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при збереженні тренування: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Помилка", "Не вдалося зберегти тренування.", "OK");
            }
        }

        private async Task<string> UploadFileToGoogleDriveAsync(DriveService driveService, string userId,
            string filePath, string fileName)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = $"{userId}_{fileName}",
                Parents = new List<string> { "1HXcy_Bcl7LenKm8S9wuVkUxBqxoTfmtF" }
            };

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var request = driveService.Files.Create(fileMetadata, fileStream, GetMimeType(filePath));
            request.Fields = "id";
            var result = await request.UploadAsync();

            if (result.Status == UploadStatus.Completed)
            {
                var file = request.ResponseBody;
                return file.Id;
            }

            throw result.Exception ?? new Exception("Невідома помилка при завантаженні файлу.");
        }

        private async Task DeleteFileFromGoogleDriveAsync(DriveService driveService, string fileId)
        {
            await driveService.Files.Delete(fileId).ExecuteAsync();
        }

        private string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".mp4" or ".mov" => "video/mp4",
                _ => "application/octet-stream",
            };
        }

        private async Task<DriveService> InitializeDriveServiceAsync()
        {
            return await Task.Run(() =>
            {
                var assembly = typeof(CreateWorkoutViewModel).Assembly;
                var resourceName = "TrainingApp.Resources.Files.service_account.json";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Файл {resourceName} не знайдено в ресурсах додатку.");
                }

                var credential = GoogleCredential.FromStream(stream).CreateScoped(DriveService.Scope.Drive);

                return new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "TrainingApp",
                });
            });
        }

        private IObservable<bool> CanSaveWorkout()
        {
            return this.WhenAnyValue(
                x => x.Name,
                x => x.Duration,
                x => x.SelectedIntensity,
                x => x.SelectedLevel,
                x => x.ScheduleInterval,
                x => x.SelectedScheduleUnit,
                (name, duration, intensity, level, interval, unit) =>
                    !string.IsNullOrWhiteSpace(name) &&
                    int.TryParse(duration, out _) &&
                    !string.IsNullOrWhiteSpace(intensity) &&
                    !string.IsNullOrWhiteSpace(level) &&
                    int.TryParse(interval, out int validInterval) && validInterval > 0 &&
                    !string.IsNullOrWhiteSpace(unit) &&
                    ((unit == "днів" && validInterval <= 14) ||
                     (unit == "годин" && validInterval <= 336))
            );
        }

        private async Task CancelAsync()
        {
            await HostScreen.Router.NavigateBack.Execute();
        }

        private async void LoadAvailableTagsAsync()
        {
            try
            {
                var tags = await _context.Tags.ToListAsync();
                AvailableTags = new ObservableCollection<Tag>(tags);

                if (_workoutToEdit != null && !string.IsNullOrEmpty(_workoutToEdit.Tags))
                {
                    var tagsToSelect = _workoutToEdit.Tags.Split(", ").ToList();
                    foreach (var tagName in tagsToSelect)
                    {
                        var tag = AvailableTags.FirstOrDefault(t => t.Name == tagName);
                        if (tag != null)
                        {
                            SelectedTags.Add(tag);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні тегів: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Помилка", "Не вдалося завантажити теги.", "OK");
            }
        }

        public bool CanAddTag => !string.IsNullOrWhiteSpace(NewTagName) &&
                                 !AvailableTags.Any(t =>
                                     t.Name.Equals(NewTagName.Trim(), StringComparison.OrdinalIgnoreCase));

        private void InitializeForEdit(Workout workout)
        {
            Name = workout.Name ?? string.Empty;
            Description = workout.Description ?? string.Empty;
            Duration = workout.Duration?.ToString() ?? string.Empty;
            SelectedIntensity = workout.Intensity ?? string.Empty;
            FormattedSelectedGoals = workout.Goals ?? string.Empty;
            SelectedLevel = workout.Level ?? string.Empty;
            ScheduleInterval = ExtractInterval(workout.Schedule);
            SelectedScheduleUnit = ExtractUnit(workout.Schedule);
            CaloriesBurned = workout.CaloriesBurned?.ToString() ?? "0";

            if (!string.IsNullOrEmpty(workout.MuscleGroups))
            {
                var muscleGroups = workout.MuscleGroups.Split(", ");
                foreach (var mg in muscleGroups)
                {
                    var group = AvailableMuscleGroups.FirstOrDefault(m => m.Name == mg);
                    if (group != null)
                    {
                        group.IsSelected = true;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(workout.Goals))
            {
                var goals = workout.Goals.Split(", ");
                foreach (var g in goals)
                {
                    var goal = AvailableGoals.FirstOrDefault(m => m.Name == g);
                    if (goal != null)
                    {
                        goal.IsSelected = true;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(workout.WorkoutTypes))
            {
                var types = workout.WorkoutTypes.Split(", ");
                foreach (var t in types)
                {
                    var goal = AvailableWorkoutTypes.FirstOrDefault(m => m.Name == t);
                    if (goal != null)
                    {
                        goal.IsSelected = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(workout.Equipment))
            {
                var equipment = workout.Equipment.Split(", ");
                foreach (var eq in equipment)
                {
                    var item = AvailableEquipment.FirstOrDefault(e => e.Name == eq);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(workout.Tags))
            {
                var tags = workout.Tags.Split(", ");
                foreach (var tagName in tags)
                {
                    var tag = AvailableTags.FirstOrDefault(t => t.Name == tagName);
                    if (tag != null)
                    {
                        SelectedTags.Add(tag);
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(workout.WorkoutTypes))
            {
                var workoutTypes = workout.WorkoutTypes.Split(", ");
                foreach (var wt in workoutTypes)
                {
                    var item = AvailableWorkoutTypes.FirstOrDefault(w => w.Name == wt);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
            }

            // Ініціалізація Goals
            if (!string.IsNullOrEmpty(workout.Goals))
            {
                var goals = workout.Goals.Split(", ");
                foreach (var g in goals)
                {
                    var item = AvailableGoals.FirstOrDefault(goal => goal.Name == g);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
            }

            var exercises = _context.Exercises
                .Include(e => e.WorkoutExercises)
                .Include(e => e.ExerciseMedia)
                .Where(e => e.WorkoutExercises.Any(we => we.WorkoutId == workout.WorkoutId))
                .ToList();

            var counter = 1;
            foreach (var exercise in exercises)
            {
                var exerciseVm = new ExerciseViewModel(_currentUserId.ToString(), AvailableEquipment)
                {
                    ExerciseNumber = counter,
                    Name = exercise.Name,
                    Description = exercise.Description ?? string.Empty,
                    DifficultyLevel = exercise.DifficultyLevel ?? string.Empty,
                    ExerciseType = exercise.ExerciseType ?? string.Empty,
                    Duration = exercise.Duration?.ToString() ?? "0",
                    Reps = exercise.Reps ?? 0,
                    Sets = exercise.Sets ?? 0,
                    Precautions = exercise.Precautions ?? string.Empty
                };

                foreach (var equipmentName in (exercise.EquipmentNeeded ?? string.Empty).Split(", ").Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var equipmentItem = exerciseVm.EquipmentOptions.FirstOrDefault(e => e.Name == equipmentName);
                    if (equipmentItem != null)
                    {
                        equipmentItem.IsSelected = true;
                    }
                }

                if (exercise.ExerciseMedia != null && exercise.ExerciseMedia.Any())
                {
                    foreach (var media in exercise.ExerciseMedia)
                    {
                        var mediaModel = new ExerciseMedia
                        {
                            MediaType = media.MediaType,
                            MediaUrl = media.MediaUrl,
                            DriveFileId = media.DriveFileId,
                            UploadedAt = media.UploadedAt
                        };
                        exerciseVm.Media.Add(mediaModel);
                    }
                }

                Exercises.Add(exerciseVm);
                counter++;
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
    }
}
