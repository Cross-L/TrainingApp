using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using DataAccess.Database;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TrainingApp.Models;
using TrainingApp.ViewModels.Registration;

namespace TrainingApp.ViewModels.Login
{
    public class LoginViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment => "login";
        public IScreen HostScreen { get; }
        [Reactive] public string Email { get; set; } = "";
        [Reactive] public string Password { get; set; } = "";

        // Властивість для відображення помилок
        [Reactive] public string ErrorMessage { get; set; }
        [Reactive] public bool IsBusy { get; set; }
        // Команди
        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<Unit, Unit> SignInWithGoogleCommand { get; }
        public ReactiveCommand<Unit, Unit> NavigateToRegistrationCommand { get; }

        // OAuth2 (Google) Configuration
        private const string ClientId = "950489629364-44kounjk77ol1uiq0me89ie8nujcpd7s.apps.googleusercontent.com";
        private const string RedirectUri =
            "com.googleusercontent.apps.950489629364-44kounjk77ol1uiq0me89ie8nujcpd7s:/oauth2redirect/google";

        private readonly ApplicationDbContext _context;

        public LoginViewModel(ApplicationDbContext context, IScreen screen)
        {
            _context = context;
            HostScreen = screen;

            // Ініціалізація команд
            var canLogin = this.WhenAnyValue(
                vm => vm.Email,
                vm => vm.Password,
                (email, password) =>
                    !string.IsNullOrWhiteSpace(email) &&
                    !string.IsNullOrWhiteSpace(password) &&
                    IsValidEmail(email)
            );

            LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync, canLogin);
            SignInWithGoogleCommand =
                ReactiveCommand.CreateFromTask(SignInWithGoogleAsync, outputScheduler: RxApp.MainThreadScheduler);
            NavigateToRegistrationCommand = ReactiveCommand.CreateFromTask(NavigateToRegistrationAsync);

            // Підписка на виконання команд для відображення індикатора завантаження
            LoginCommand.IsExecuting
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isExecuting => IsBusy = isExecuting);

            SignInWithGoogleCommand.IsExecuting
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isExecuting => IsBusy = isExecuting);
            
            // NavigateToRegistrationCommand.IsExecuting
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(isExecuting => IsBusy = isExecuting);

            // Обробка помилок команд
            LoginCommand.ThrownExceptions
                .Subscribe(ex => ErrorMessage = $"Помилка входу: {ex.Message}");

            SignInWithGoogleCommand.ThrownExceptions
                .Subscribe(ex => ErrorMessage = $"Помилка автентифікації через Google: {ex.Message}");

            // Валідація та відображення помилок при зміні полів
            this.WhenAnyValue(vm => vm.Email, vm => vm.Password)
                .Subscribe(values =>
                {
                    var email = values.Item1;
                    var password = values.Item2;

                    // Скидання помилок
                    ErrorMessage = string.Empty;

                    // Валідація Email
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        ErrorMessage = "Електронна пошта не може бути порожньою.";
                        return;
                    }
                    if (!IsValidEmail(email))
                    {
                        ErrorMessage = "Некоректний формат електронної пошти.";
                        return;
                    }

                    // Валідація Пароля
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        ErrorMessage = "Пароль не може бути порожнім.";
                    }
                });
        }
        
        private async Task LoginAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

                if (user == null)
                {
                    ErrorMessage = "Спочатку зареєструйтесь.";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        ErrorMessage = "Пароль не може бути порожнім.";
                        return;
                    }

                    var isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, user.Password);

                    if (!isPasswordValid)
                    {
                        ErrorMessage = "Невірний пароль.";
                        return;
                    }
                    
                    await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, Email));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Сталася помилка при вході: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Команда для автентифікації через Google.
        /// </summary>
        private async Task SignInWithGoogleAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            
            await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, "dmitry.ischenko2910@gmail.com"));
            return;

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
                    ErrorMessage = "Не вдалося отримати код авторизації.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка автентифікації: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
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

                    await GetUserInfoAsync(accessToken);
                }
                else
                {
                    ErrorMessage = "Не вдалося отримати токен доступу.";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Не вдалося обміняти код на токен доступу: {errorContent}";
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
                    Email = userInfo.Email;
                    Console.WriteLine($"Користувач: {userInfo.Name}, Email: {userInfo.Email}");
                    await SaveOrUpdateUserAsync(userInfo, accessToken);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Не вдалося отримати інформацію про користувача: {errorContent}";
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
                    await HostScreen.Router.Navigate.Execute(new RegistrationContinuationViewModel(_context, HostScreen, email));
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Помилка при збереженні користувача: {ex.Message}";
                }

            }
            else
            {
                existingUser.Email = email;
                existingUser.FirstName = firstName;
                existingUser.LastName = lastName;
                _context.Users.Update(existingUser);
                try
                {
                    await _context.SaveChangesAsync();
                    await HostScreen.Router.Navigate.Execute(new HomeViewModel(_context, HostScreen, Email));
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Помилка при оновленні користувача: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Метод для навігації до сторінки реєстрації.
        /// </summary>
        private async Task NavigateToRegistrationAsync()
        {
            try
            {
                await HostScreen.Router.Navigate.Execute(new RegistrationViewModel(_context, HostScreen));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Метод для перевірки валідності електронної пошти.
        /// </summary>
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
    }
}
