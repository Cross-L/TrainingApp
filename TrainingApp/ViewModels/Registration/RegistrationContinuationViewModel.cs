using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DataAccess.Database;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Microsoft.EntityFrameworkCore;
using TrainingApp.Models;

namespace TrainingApp.ViewModels.Registration
{
    public class RegistrationContinuationViewModel : ReactiveObject, IRoutableViewModel, IDisposable
    {
        public string? UrlPathSegment => "registration-continuation";
        public IScreen HostScreen { get; }

        private readonly ApplicationDbContext _context;

        public ObservableCollection<string> Genders { get; } = new ObservableCollection<string> { "Чоловік", "Жінка", "Інше" };
        public ObservableCollection<string> ActivityLevels { get; } = new ObservableCollection<string> { "Низький", "Середній", "Високий" };
        public ObservableCollection<string> TrainingTypes { get; } = new ObservableCollection<string>
        {
            "Силові тренування",
            "Кардіо",
            "Йога",
            "Пілатес",
            "Функціональний тренінг",
            "Високоінтенсивні тренування (HIIT)",
            "Розтяжка",
            "Бойові мистецтва",
            "Плавання",
            "Велоспорт",
            "Реабілітаційні тренування",
            "Інше"
        };

        public ObservableCollection<SelectableItem> Goals { get; } = new ObservableCollection<SelectableItem>
        {
            new SelectableItem("Втрата ваги"),
            new SelectableItem("Набір м'язової маси"),
            new SelectableItem("Покращення витривалості"),
            new SelectableItem("Релаксація"),
            new SelectableItem("Загальне здоров'я"),
            new SelectableItem("Інше")
        };

        [Reactive] public string SelectedGender { get; set; }
        [Reactive] public string SelectedActivityLevel { get; set; }
        [Reactive] public string SelectedTrainingType { get; set; }

        // Властивості для введення віку, ваги та зросту
        [Reactive] public string Age { get; set; }
        [Reactive] public string Weight { get; set; }
        [Reactive] public string Height { get; set; }
        [Reactive] public bool IsBusy { get; set; }
        [Reactive] public string GeneralError { get; set; }

        private readonly string _currentUserEmail;

        public ReactiveCommand<Unit, Unit> CompleteRegistrationCommand { get; }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public RegistrationContinuationViewModel(ApplicationDbContext context, IScreen screen, string currentUserEmail)
        {
            _context = context;
            HostScreen = screen;
            _currentUserEmail = currentUserEmail;

            // Підписка на зміни IsSelected кожного елемента в Goals
            foreach (var goal in Goals)
            {
                goal.WhenAnyValue(g => g.IsSelected)
                    .Subscribe(_ => { }) // Порожня підписка для активації зміни
                    .DisposeWith(_disposables);
            }

            // Створення Observable, яке відстежує, чи є хоча б одна вибрана ціль
            var goalsSelectedObservable = Goals
                .ToObservableChangeSet()
                .AutoRefresh(g => g.IsSelected)
                .ToCollection()
                .Select(g => g.Any(item => item.IsSelected))
                .StartWith(Goals.Any(g => g.IsSelected));

            // Створення Observable для перевірки валідності форми
            var formValidObservable = this.WhenAnyValue(
                    vm => vm.SelectedGender,
                    vm => vm.Age,
                    vm => vm.Weight,
                    vm => vm.Height,
                    vm => vm.SelectedActivityLevel,
                    vm => vm.SelectedTrainingType)
                .CombineLatest(goalsSelectedObservable, 
                    (values, goalsSelected) =>
                    {
                        var (gender, age, weight, height, activity, trainingType) = values;
                        return !string.IsNullOrWhiteSpace(gender) &&
                               IsValidAge(age) &&
                               IsValidWeight(weight) &&
                               IsValidHeight(height) &&
                               !string.IsNullOrWhiteSpace(activity) &&
                               !string.IsNullOrWhiteSpace(trainingType) &&
                               goalsSelected;
                    })
                .DistinctUntilChanged();

            // Ініціалізація команди
            CompleteRegistrationCommand = ReactiveCommand.CreateFromTask(CompleteRegistrationAsync, formValidObservable);
            CompleteRegistrationCommand.IsExecuting
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isExecuting => IsBusy = isExecuting)
                .DisposeWith(_disposables);

            // Обробка змін для валідації в реальному часі
            this.WhenAnyValue(
                    vm => vm.SelectedGender,
                    vm => vm.Age,
                    vm => vm.Weight,
                    vm => vm.Height,
                    vm => vm.SelectedActivityLevel,
                    vm => vm.SelectedTrainingType)
                .Subscribe(_ => ValidateFields())
                .DisposeWith(_disposables);
        }

        private async Task CompleteRegistrationAsync()
        {
            if (!IsFormValid())
            {
                GeneralError = "Будь ласка, виправте помилки у формі.";
                return;
            }

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    GeneralError = "Користувача не знайдено. Будь ласка, спробуйте зареєструватися знову.";
                    return;
                }

                currentUser.Gender = SelectedGender;
                currentUser.Age = int.Parse(Age);
                currentUser.Weight = decimal.Parse(Weight);
                currentUser.Height = decimal.Parse(Height);
                currentUser.PhysicalActivityLevel = SelectedActivityLevel;
                currentUser.PreferredTrainingType = SelectedTrainingType;

                var selectedGoals = Goals.Where(g => g.IsSelected).Select(g => g.Name).ToList();
                currentUser.Goals = string.Join(", ", selectedGoals);

                _context.Users.Update(currentUser);
                await _context.SaveChangesAsync();
                await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, _currentUserEmail));
            }
            catch (Exception ex)
            {
                GeneralError = $"Сталася помилка при завершенні реєстрації: {ex.Message}";
            }
        }

        /// <summary>
        /// Валідація всіх полів форми та встановлення відповідних повідомлень про помилки.
        /// </summary>
        private void ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(SelectedGender))
            {
                GeneralError = "Будь ласка, оберіть стать.";
                return;
            }

            if (!IsValidAge(Age))
            {
                GeneralError = "Будь ласка, введіть коректний вік (1-120).";
                return;
            }

            if (!IsValidWeight(Weight))
            {
                GeneralError = "Будь ласка, введіть коректну вагу (30-300 кг).";
                return;
            }

            if (!IsValidHeight(Height))
            {
                GeneralError = "Будь ласка, введіть коректний зріст (50-250 см).";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedActivityLevel))
            {
                GeneralError = "Будь ласка, оберіть рівень фізичної активності.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedTrainingType))
            {
                GeneralError = "Будь ласка, оберіть бажаний тип тренування.";
            }
            else
            {
                GeneralError = string.Empty;
            }
        }

        /// <summary>
        /// Перевірка, чи форма є дійсною.
        /// </summary>
        /// <returns>true, якщо форма дійсна; інакше false.</returns>
        private bool IsFormValid()
        {
            return string.IsNullOrEmpty(GeneralError);
        }

        /// <summary>
        /// Перевірка валідності віку.
        /// </summary>
        /// <param name="age">Вік у вигляді рядка.</param>
        /// <returns>true, якщо вік валідний; інакше false.</returns>
        private bool IsValidAge(string age)
        {
            if (int.TryParse(age, out int ageValue))
            {
                return ageValue > 0 && ageValue <= 120;
            }
            return false;
        }

        /// <summary>
        /// Перевірка валідності ваги.
        /// </summary>
        /// <param name="weight">Вага у вигляді рядка.</param>
        /// <returns>true, якщо вага валідна; інакше false.</returns>
        private bool IsValidWeight(string weight)
        {
            if (double.TryParse(weight, out double weightValue))
            {
                return weightValue >= 30 && weightValue <= 300;
            }
            return false;
        }

        /// <summary>
        /// Перевірка валідності зросту.
        /// </summary>
        /// <param name="height">Зріст у вигляді рядка.</param>
        /// <returns>true, якщо зріст валідний; інакше false.</returns>
        private bool IsValidHeight(string height)
        {
            if (double.TryParse(height, out double heightValue))
            {
                return heightValue >= 50 && heightValue <= 250;
            }
            return false;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == _currentUserEmail);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
