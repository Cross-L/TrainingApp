using Newtonsoft.Json;

namespace TrainingApp;

public static class AppConfigLoader
{
    public static AppConfig? LoadConfig(string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Files", relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File {fullPath} not found.");
        }

        var json = File.ReadAllText(fullPath);
        return JsonConvert.DeserializeObject<AppConfig>(json);
    }
}