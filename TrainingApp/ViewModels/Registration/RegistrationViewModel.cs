using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using DataAccess.Database;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TrainingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace TrainingApp.ViewModels.Registration
{
    public class RegistrationViewModel : ReactiveObject, IRoutableViewModel
    {
        public string? UrlPathSegment => "registration";
        public IScreen HostScreen { get; } 

        [Reactive] public string FirstName { get; set; }
        [Reactive] public string LastName { get; set; }
        [Reactive] public string Email { get; set; }
        [Reactive] public string Password { get; set; }

        [Reactive] public bool IsBusy { get; set; }
        [Reactive] public string PasswordError { get; set; }
        [Reactive] public string GeneralError { get; set; }

        public ReactiveCommand<Unit, Unit> SignInWithGoogleCommand { get; }
        public ReactiveCommand<Unit, Unit> ContinueCommand { get; }

        // OAuth2 (Google) Configuration
        private const string ClientId = "950489629364-44kounjk77ol1uiq0me89ie8nujcpd7s.apps.googleusercontent.com";

        private const string RedirectUri =
            "com.googleusercontent.apps.950489629364-44kounjk77ol1uiq0me89ie8nujcpd7s:/oauth2redirect/google";

        private readonly ApplicationDbContext _context;
#pragma warning disable CA1416
        public RegistrationViewModel(ApplicationDbContext context, IScreen screen)
        {
            _context = context;
            HostScreen = screen;
            
            var canContinue = this.WhenAnyValue(
                    vm => vm.FirstName,
                    vm => vm.LastName,
                    vm => vm.Email,
                    vm => vm.Password,
                    (firstName, lastName, email, password) =>
                        !string.IsNullOrWhiteSpace(firstName) &&
                        !string.IsNullOrWhiteSpace(lastName) &&
                        IsValidEmail(email) &&
                        IsValidPassword(password))
                .DistinctUntilChanged();
            
            ContinueCommand = ReactiveCommand.CreateFromTask(ContinueAsync, canContinue); 
            
            ContinueCommand.IsExecuting
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isExecuting => IsBusy = isExecuting);


            // Subscribe to changes and set the first encountered error in PasswordError
            this.WhenAnyValue(vm => vm.FirstName, vm => vm.LastName, vm => vm.Email, vm => vm.Password)
                .Subscribe(values =>
                {
                    var firstName = values.Item1;
                    var lastName = values.Item2;
                    var email = values.Item3;
                    var password = values.Item4;

                    // Reset PasswordError
                    PasswordError = string.Empty;

                    // Check for errors in order and set the first error encountered
                    if (string.IsNullOrWhiteSpace(firstName))
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(lastName))
                    {
                        return;
                    }

                    if (!IsValidEmail(email))
                    {
                        PasswordError = "Некоректний формат електронної пошти.";
                        return;
                    }

                    var passwordValidation = ValidatePassword(password);
                    if (!string.IsNullOrEmpty(passwordValidation))
                    {
                        PasswordError = passwordValidation;
                    }
                });
        }

        private async Task SignInWithGoogleAsync()
        {
            try
            {
                var authUrl = new Uri("https://accounts.google.com/o/oauth2/v2/auth" +
                                      $"?client_id={ClientId}" +
                                      $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                                      "&response_type=code" +
                                      "&scope=openid%20email%20profile" +
                                      "&access_type=offline" +
                                      "&prompt=consent");

                var callbackUrl = new Uri(RedirectUri);

                var authResult = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

                if (authResult.Properties.TryGetValue("code", out var code))
                {
                    await ExchangeCodeForTokenAsync(code, ClientId, RedirectUri);
                }
                else
                {
                    GeneralError = "Не вдалося отримати код авторизації.";
                }
            }
            catch (Exception ex)
            {
                GeneralError = $"Помилка автентифікації: {ex.Message}";
            }
        }

        private async Task ExchangeCodeForTokenAsync(string code, string clientId, string redirectUri)
        {
            var tokenRequestUrl = "https://oauth2.googleapis.com/token";

            var postData = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(tokenRequestUrl, new FormUrlEncodedContent(postData));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);
                var accessToken = tokenResponse?.AccessToken;
                if (accessToken != null)
                {
                    Console.WriteLine($"Успішна автентифікація. Токен доступу: {accessToken}");

                    // Retrieve user information
                    await GetUserInfoAsync(accessToken);
                }
                else
                {
                    GeneralError = "Не вдалося отримати токен доступу.";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                GeneralError = $"Не вдалося обміняти код на токен доступу: {errorContent}";
            }
        }

        private async Task GetUserInfoAsync(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<UserInfo>(content);

                if (userInfo != null)
                {
                    // Assign values from Google response
                    FirstName = userInfo.GivenName;
                    LastName = userInfo.FamilyName;
                    Email = userInfo.Email;

                    Console.WriteLine($"Користувач: {userInfo.Name}, Email: {userInfo.Email}");

                    // Save or update user in the database
                    await SaveOrUpdateUserAsync(userInfo, accessToken);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                GeneralError = $"Не вдалося отримати інформацію про користувача: {errorContent}";
            }
        }

        private async Task SaveOrUpdateUserAsync(UserInfo userInfo, string accessToken)
        {
            var googleUserId = userInfo.Id;
            var email = userInfo.Email;
            var firstName = userInfo.GivenName;
            var lastName = userInfo.FamilyName;

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser == null)
            {
                var newUser = new User
                {
                    GoogleUserId = googleUserId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    RegisteredVia = "google",
                    RegistrationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                _context.Users.Add(newUser);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    GeneralError = $"Помилка при збереженні користувача: {ex.Message}";
                }

                await HostScreen.Router.Navigate.Execute(
                    new RegistrationContinuationViewModel(_context, HostScreen, Email));
            }
            else
            {
                GeneralError = "Ви вже зареєстровані. Увійдіть в аккаунт";
            }
        }

        private async Task ContinueAsync()
        {
            if (!IsFormValid())
            {
                PasswordError = "Будь ласка, виправте помилки у формі.";
                return;
            }

            try
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

                if (existingUser != null)
                {
                    PasswordError = "Користувач з таким Email вже існує.";
                    return;
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

                var newUserManual = new User
                {
                    Email = Email,
                    FirstName = FirstName,
                    LastName = LastName,
                    Password = hashedPassword,
                    RegisteredVia = "manual",
                    RegistrationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                _context.Users.Add(newUserManual);
                await _context.SaveChangesAsync();
                Console.WriteLine("Користувач успішно зареєстрований.");

                await HostScreen.Router.Navigate.Execute(
                    new RegistrationContinuationViewModel(_context, HostScreen, Email));
            }
            catch (Exception ex)
            {
                PasswordError = $"Сталася помилка при реєстрації: {ex.Message}";
            }
        }

        // Method to validate email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Method to check password validity
        private bool IsValidPassword(string password)
        {
            return string.IsNullOrEmpty(ValidatePassword(password));
        }

        // Method to validate password and return a general error message if invalid
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

        private bool IsFormValid()
        {
            return string.IsNullOrEmpty(PasswordError);
        }
    }
}