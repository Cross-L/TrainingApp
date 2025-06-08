using System.Collections.ObjectModel;
using System.Globalization;
using TrainingApp.Models;

namespace TrainingApp.Converters
{
    public class GoalsSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var goals = value as ObservableCollection<SelectableItem>;
            if (goals == null)
                return false;
            
            return !goals.Any(g => g.IsSelected);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}