using System.Globalization;

namespace TrainingApp.Converters
{
    public class SubscribeButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSubscribed)
            {
                return isSubscribed ? "Відписатися" : "Підписатися";
            }
            return "Підписатися";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}