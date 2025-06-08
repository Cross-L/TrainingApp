using System.Globalization;

namespace TrainingApp.Converters
{
    public class BoolToArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isExpanded = (bool)value;
            return isExpanded ? "arrow_up.png" : "arrow_down.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
