using DataAccess.Database;

namespace TrainingApp.Services;

public class UserService(ApplicationDbContext dbContext)
{
    public User? GetUserByEmail(string email)
    {
        return dbContext.Users.FirstOrDefault(u => u.Email == email);
    }
    
    public User? GetUserById(int id)
    {
        return dbContext.Users.FirstOrDefault(u => u.UserId == id);
    }

    public async Task UpdateUserAsync(User user)
    {
        // Logic to update user data
    }
}