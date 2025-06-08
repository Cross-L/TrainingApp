using System.Collections.ObjectModel;
using System.Reactive;
using DataAccess.Database;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TrainingApp.Models;

namespace TrainingApp.ViewModels
{
    public class ExerciseDetailsViewModel : ReactiveObject
    {
        [Reactive] public string Name { get; private set; }
        [Reactive] public string Description { get; private set; }
        [Reactive] public string DifficultyLevel { get; private set; }
        [Reactive] public string ExerciseType { get; private set; }
        [Reactive] public string Duration { get; private set; }
        [Reactive] public int Reps { get; private set; }
        [Reactive] public int Sets { get; private set; }
        [Reactive] public string Precautions { get; private set; }
        [Reactive] public ObservableCollection<ExerciseMedia> Media { get; private set; }

        public ObservableCollection<EquipmentItem> SelectedEquipment { get; private set; }
        
        [Reactive] public ObservableCollection<MediaDisplayItem> MediaDisplayItems { get; private set; }
        
        public ReactiveCommand<ExerciseMedia, Unit> OpenMediaCommand { get; }

        public ExerciseDetailsViewModel(ApplicationDbContext context, Exercise exercise)
        {
            Name = !string.IsNullOrWhiteSpace(exercise.Name) ? exercise.Name : "Не вказано";
            Description = !string.IsNullOrWhiteSpace(exercise.Description) ? exercise.Description : "Не вказано";
            DifficultyLevel = !string.IsNullOrWhiteSpace(exercise.DifficultyLevel) ? exercise.DifficultyLevel : "Не вказано";
            ExerciseType = !string.IsNullOrWhiteSpace(exercise.ExerciseType) ? exercise.ExerciseType : "Не вказано";
            Duration = exercise.Duration > 0 ? exercise.Duration.ToString() : "Не вказано";
            Reps = exercise.Reps ?? 0;
            Sets = exercise.Sets ?? 0;
            Precautions = !string.IsNullOrWhiteSpace(exercise.Precautions) ? exercise.Precautions : "Не вказано";

            Media = new ObservableCollection<ExerciseMedia>(exercise.ExerciseMedia ?? new List<ExerciseMedia>());
            var media = context.ExerciseMedia.Where(m => m.ExerciseId == exercise.ExerciseId).ToList();
            Media = new ObservableCollection<ExerciseMedia>(media);

            SelectedEquipment = new ObservableCollection<EquipmentItem>(
                exercise.EquipmentNeeded?.Split(", ").Select(name => new EquipmentItem { Name = name, IsSelected = true }) ?? new List<EquipmentItem>()
            );

            MediaDisplayItems = new ObservableCollection<MediaDisplayItem>();

            int photoCount = 0;
            int videoCount = 0;

            foreach (var m in media)
            {
                string displayName;

                if (IsPhoto(m.MediaUrl))
                {
                    photoCount++;
                    displayName = $"Фото {photoCount}";
                }
                else if (IsVideo(m.MediaUrl))
                {
                    videoCount++;
                    displayName = $"Відео {videoCount}";
                }
                else
                {
                    displayName = "Медіафайл";
                }

                MediaDisplayItems.Add(new MediaDisplayItem { Media = m, DisplayName = displayName });
            }

            OpenMediaCommand = ReactiveCommand.CreateFromTask<ExerciseMedia>(OpenMediaAsync);
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
                        $"Файл не знайдено: {Path.GetFileName(media.MediaUrl)}",
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
        
        private bool IsPhoto(string filePath)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return extensions.Contains(ext);
        }

        private bool IsVideo(string filePath)
        {
            var extensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return extensions.Contains(ext);
        }

    }
}