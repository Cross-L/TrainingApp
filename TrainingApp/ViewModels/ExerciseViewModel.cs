using System.Collections.ObjectModel;
using System.Reactive;
using DataAccess.Database;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TrainingApp.Models;

namespace TrainingApp.ViewModels
{
    public class ExerciseViewModel : ReactiveObject
    {
        [Reactive] public string Name { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public string DifficultyLevel { get; set; }
        [Reactive] public string ExerciseType { get; set; }
        [Reactive] public string Duration { get; set; }
        [Reactive] public int Reps { get; set; }
        [Reactive] public int Sets { get; set; }
        [Reactive] public string Precautions { get; set; }
        [Reactive] public ObservableCollection<ExerciseMedia> Media { get; set; }

        public ObservableCollection<string> DifficultyLevels { get; }
        public ObservableCollection<string> ExerciseTypes { get; }

        [Reactive] public ObservableCollection<EquipmentItem> EquipmentOptions { get; set; }

        [Reactive] public int ExerciseNumber { get; set; }
        public string ExerciseTitle => $"Вправа {ExerciseNumber}";
        [Reactive] public bool IsExpanded { get; set; }

        public IEnumerable<string> SelectedEquipment => EquipmentOptions
            .Where(e => e.IsSelected)
            .Select(e => e.Name);

        public ReactiveCommand<Unit, Unit> AddMediaCommand { get; }
        public ReactiveCommand<ExerciseMedia, Unit> RemoveMediaCommand { get; }
        public ReactiveCommand<ExerciseMedia, Unit> OpenMediaCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleExpandCommand { get; }

        public ObservableCollection<int> RepsOptions { get; }
        public ObservableCollection<int> SetsOptions { get; }

        private readonly string _userId;

        public ExerciseViewModel(string userId, ObservableCollection<EquipmentItem> availableEquipment)
        {
            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            Media = new ObservableCollection<ExerciseMedia>();
            DifficultyLevels = new ObservableCollection<string>
            {
                "початковий",
                "середній",
                "високий"
            };
            ExerciseTypes = new ObservableCollection<string>
            {
                "силова",
                "кардіо",
                "йога",
                "пілатес",
                "функціональна",
                "HIIT",
                "розтяжка",
                "бойові мистецтва",
                "плавання",
                "велоспорт",
                "реабілітаційна",
                "інше"
            };

            EquipmentOptions = new ObservableCollection<EquipmentItem>(availableEquipment.Select
                (e => new EquipmentItem { Name = e.Name }));

            RepsOptions = new ObservableCollection<int>(Enumerable.Range(1, 100));
            SetsOptions = new ObservableCollection<int>(Enumerable.Range(1, 20));

            AddMediaCommand = ReactiveCommand.CreateFromTask(AddMediaAsync);
            RemoveMediaCommand = ReactiveCommand.CreateFromTask<ExerciseMedia>(RemoveMediaAsync);
            ToggleExpandCommand = ReactiveCommand.Create(() =>
            {
                IsExpanded = !IsExpanded;
                return Unit.Default;
            });
            OpenMediaCommand = ReactiveCommand.CreateFromTask<ExerciseMedia>(OpenMediaAsync);
        }

#pragma warning disable CA1416

        private async Task AddMediaAsync()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "image/*", "video/*" } },
                    { DevicePlatform.iOS, new[] { "public.image", "public.movie" } },
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov" } }
                });
                var options = new PickOptions
                {
                    PickerTitle = "Виберіть фото або відео",
                    FileTypes = customFileType
                };

                var results = await FilePicker.Default.PickMultipleAsync(options);
                foreach (var file in results)
                {
                    var mediaType = GetMediaType(file.FileName);

                    var media = new ExerciseMedia
                    {
                        MediaType = mediaType,
                        MediaUrl = file.FullPath,
                        DriveFileId = null,
                        UploadedAt = DateTime.UtcNow
                    };

                    Media.Add(media);
                }
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Помилка",
                    "Не вдалося вибрати файли.",
                    "OK"
                );
            }
        }

        private async Task RemoveMediaAsync(ExerciseMedia media)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Підтвердження",
                $"Ви впевнені, що хочете видалити файл {Path.GetFileName(media.MediaUrl)}?",
                "Так",
                "Ні"
            );

            if (confirm)
            {
                Media.Remove(media);
            }
        }

        private async Task OpenMediaAsync(ExerciseMedia media)
        {
            try
            {
                var path = media.MediaUrl;
                if (File.Exists(path))
                {
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(path)
                    });
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Помилка",
                        $"Файл не знайдено: {media.FileName}",
                        "OK"
                    );
                }
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Помилка",
                    "Не вдалося відкрити файл.",
                    "OK"
                );
            }
        }

        private string GetMediaType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" => "Фото",
                ".mp4" or ".mov" => "Відео",
                _ => "Невідомий тип",
            };
        }
    }
}