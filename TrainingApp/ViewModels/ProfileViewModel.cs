using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DataAccess.Database;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;

namespace TrainingApp.ViewModels
{
    public class ProfileViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "profile";
        public IScreen HostScreen { get; }

        [Reactive] public string ProfileImageUrl { get; set; } = "default_profile_image.png";
        [Reactive] public string FullName { get; set; }
        [Reactive] public string Email { get; set; }
        [Reactive] public int CreatedWorkoutsCount { get; set; }
        [Reactive] public int SubscribeWorkoutsCount { get; set; }
        [Reactive] public decimal AverageWorkoutRating { get; set; }
        [Reactive] public bool CanChangePassword { get; set; }
        [Reactive] public string NewPassword { get; set; }
        [Reactive] public string ConfirmPassword { get; set; }
        [Reactive] public bool IsBusy { get; set; }
        
        [Reactive]
        public string Title { get; set; }
        
        public ReactiveCommand<Unit, Unit> UploadProfileImageCommand { get; }
        public ReactiveCommand<Unit, Unit> ChangePasswordCommand { get; }
        
        public ReactiveCommand<Unit, Unit> NavigateHomeCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateWorkoutsCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateFavoritesCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateProfileCommand { get; }

        private readonly ApplicationDbContext _context;
        private readonly int _currentUserId;
        private readonly DriveService _driveService;
        
        [Reactive] public bool IsOwnProfile { get; set; }

        public ProfileViewModel(ApplicationDbContext context, IScreen hostScreen, int userId, int currentUserId)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            HostScreen = hostScreen;
            
            _currentUserId = currentUserId;
            IsOwnProfile = _currentUserId == userId;

            Title = IsOwnProfile ? "Ваш профіль" : "Профіль користувача";

            _driveService = InitializeDriveServiceAsync().GetAwaiter().GetResult();

            // Команды
            UploadProfileImageCommand = ReactiveCommand.CreateFromTask(UploadProfileImageAsync, this.WhenAnyValue(x => x.IsOwnProfile));
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePasswordAsync, this.WhenAnyValue(x => x.IsOwnProfile));

            // Навигационные команды
            NavigateHomeCommand = ReactiveCommand.CreateFromTask(NavigateHomeAsync);
            NavigateWorkoutsCommand = ReactiveCommand.CreateFromTask(NavigateWorkoutsAsync);
            NavigateFavoritesCommand = ReactiveCommand.CreateFromTask(NavigateFavoritesAsync);
            NavigateProfileCommand = ReactiveCommand.CreateFromTask(NavigateProfileAsync);

            LoadProfileData(userId);
        }
        
        private void LoadProfileData(int userId)
        {
            IsBusy = true;
            Task.Run(async () =>
            {
                try
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user != null)
                    {
                        FullName = $"{user.FirstName} {user.LastName}";
                        Email = user.Email;
                        
                        CreatedWorkoutsCount = await _context.Workouts.CountAsync(w => w.AuthorId == userId);
                        SubscribeWorkoutsCount = await _context.UserWorkouts
                            .Where(uw => _context.Workouts.Any
                                (w => w.WorkoutId == uw.WorkoutId && w.AuthorId == userId))
                            .Select(uw => uw.UserId)
                            .Distinct()
                            .CountAsync();
                        AverageWorkoutRating = await _context.WorkoutReviews
                            .Where(r => r.UserId == userId)
                            .Select(r => (decimal?)r.Rating)
                            .AverageAsync() ?? 0m;
                        CanChangePassword = IsOwnProfile && user.RegisteredVia != "google";
                        ProfileImageUrl = await GetProfileImageUrlAsync(userId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при завантаженні даних профілю: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }
        
        private async Task<string> GetProfileImageUrlAsync(int userId)
        {
            try
            {
                var listRequest = _driveService.Files.List();
                listRequest.Q = $"name contains 'profile_{userId}_' and trashed = false and '{GetGoogleDriveFolderId()}' in parents";
                listRequest.Spaces = "drive";
                listRequest.Fields = "files(id, name)";

                var result = await listRequest.ExecuteAsync();
                var files = result.Files;

                if (files != null && files.Count > 0)
                {
                    var profileFile = files.First();
                    return $"https://drive.google.com/uc?id={profileFile.Id}";
                }
                else
                {
                    return "default_profile_image.png";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при отриманні зображення профілю: {ex.Message}");
                return "default_profile_image.png";
            }
        }

        private async Task UploadProfileImageAsync()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Виберіть фото профілю",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    var filePath = result.FullPath;

                    if (string.IsNullOrEmpty(filePath))
                    {
                        await ShowAlert("Помилка", "Не вдалося отримати шлях до вибраного файлу.");
                        return;
                    }

                    var fileName = $"profile_{_currentUserId}_{Path.GetFileName(filePath)}";

                    await DeleteExistingProfileImageAsync();

                    var driveFileId = await UploadFileToGoogleDriveAsync(filePath, fileName);

                    if (!string.IsNullOrEmpty(driveFileId))
                    {
                        var timestamp = DateTime.UtcNow.Ticks;
                        var fileUrl = $"https://drive.google.com/uc?id={driveFileId}&t={timestamp}";
                        
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            ProfileImageUrl = fileUrl;
                        });
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні фото профілю: {ex.Message}");
                await ShowAlert("Помилка", "Не вдалося завантажити фото профілю.");
            }
        }


        private async Task DeleteExistingProfileImageAsync()
        {
            try
            {
                string fileNamePattern = $"profile_{_currentUserId}_*";

                var listRequest = _driveService.Files.List();
                listRequest.Q = $"name contains 'profile_{_currentUserId}_' and trashed = false and '{GetGoogleDriveFolderId()}' in parents";
                listRequest.Spaces = "drive";
                listRequest.Fields = "files(id, name)";

                var result = await listRequest.ExecuteAsync();
                var files = result.Files;

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        await _driveService.Files.Delete(file.Id).ExecuteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при видаленні існуючого зображення профілю: {ex.Message}");
            }
        }

        private async Task<string> UploadFileToGoogleDriveAsync(string filePath, string fileName)
        {
            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { GetGoogleDriveFolderId() }
                };

                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var request = _driveService.Files.Create(fileMetadata, fileStream, GetMimeType(filePath));
                request.Fields = "id";
                var result = await request.UploadAsync();

                if (result.Status == UploadStatus.Completed)
                {
                    var file = request.ResponseBody;
                    return file.Id;
                }
                else
                {
                    throw result.Exception ?? new Exception("Невідома помилка при завантаженні файлу.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні файлу на Google Drive: {ex.Message}");
                throw;
            }
        }

        private string GetGoogleDriveFolderId()
        {
            return "1HXcy_Bcl7LenKm8S9wuVkUxBqxoTfmtF";
        }

        private string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                _ => "application/octet-stream",
            };
        }

        private async Task<DriveService> InitializeDriveServiceAsync()
        {
            try
            {
                var assembly = typeof(ProfileViewModel).Assembly;
                var resourceName = "TrainingApp.Resources.Files.service_account.json";

                await using var stream = assembly.GetManifestResourceStream(resourceName);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при ініціалізації Google Drive API: {ex.Message}");
                throw;
            }
        }

        // Метод валидации пароля
        private string ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Пароль не може бути порожнім.";

            if (password.Length < 6 ||
                !Regex.IsMatch(password, @"[A-Z]") ||
                !Regex.IsMatch(password, @"[a-z]") ||
                !Regex.IsMatch(password, @"\d"))
            {
                return "Пароль має містити принаймні 6 символів, велику та малу літери та одну цифру.";
            }

            return string.Empty;
        }

        private async Task ChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await ShowAlert("Помилка", "Будь ласка, заповніть усі поля для зміни пароля.");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                await ShowAlert("Помилка", "Новий пароль і підтвердження не збігаються.");
                return;
            }

            // Валидация пароля
            var validationError = ValidatePassword(NewPassword);
            if (!string.IsNullOrEmpty(validationError))
            {
                await ShowAlert("Помилка", validationError);
                return;
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == _currentUserId);
                if (user != null)
                {
                    user.Password = HashPassword(NewPassword);
                    await _context.SaveChangesAsync();

                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;

                    await ShowAlert("Успіх", "Пароль успішно змінено.");
                }
                else
                {
                    await ShowAlert("Помилка", "Користувача не знайдено.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при зміні пароля: {ex.Message}");
                await ShowAlert("Помилка", "Сталася помилка при зміні пароля.");
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private async Task ShowAlert(string title, string message)
        {
            await Application.Current.MainPage.Dispatcher.DispatchAsync(async () =>
            {
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
            });
        }
        
        private async Task NavigateHomeAsync()
        {
            await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, Email));
        }

        private async Task NavigateWorkoutsAsync()
        {
            await HostScreen.Router.Navigate.Execute(new WorkoutsViewModel(_context, HostScreen, _currentUserId));
        }

        private async Task NavigateFavoritesAsync()
        {
            await HostScreen.Router.Navigate.Execute(new SubscribedWorkoutsViewModel(_context, HostScreen, _currentUserId));
        }

        private async Task NavigateProfileAsync()
        {
            
        }
    }
}
