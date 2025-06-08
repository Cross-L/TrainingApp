// TagSelectedConverter.cs

using System.Collections.ObjectModel;
using System.Globalization;
using DataAccess.Database;

namespace TrainingApp.Converters
{
    public class TagSelectedConverter : IValueConverter
    {
        public Color SelectedColor { get; set; } = Color.FromHex("#4CAF50"); // Зеленый
        public Color UnselectedColor { get; set; } = Color.FromHex("#555555"); // Серый

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tag tag && parameter is ObservableCollection<Tag> selectedTags)
            {
                return selectedTags.Contains(tag) ? SelectedColor : UnselectedColor;
            }
            return UnselectedColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}