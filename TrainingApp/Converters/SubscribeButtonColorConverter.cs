using System.Globalization;

namespace TrainingApp.Converters
{
    public class SubscribeButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSubscribed)
            {
                return isSubscribed ? Color.FromHex("#FF4444") : Color.FromHex("#4CAF50");
            }
            return Color.FromHex("#4CAF50");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}