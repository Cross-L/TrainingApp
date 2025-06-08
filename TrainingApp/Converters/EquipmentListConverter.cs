using System.Collections.ObjectModel;
using System.Globalization;
using TrainingApp.Models;

namespace TrainingApp.Converters
{
    public class EquipmentListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<EquipmentItem> equipment)
            {
                return string.Join(", ", equipment.Where(e => e.IsSelected).Select(e => e.Name));
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}