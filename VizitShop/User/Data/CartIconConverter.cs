using System;
using System.Globalization;
using System.Windows.Data;

namespace VizitShop
{
    public class CartIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCartEmpty = (bool)value;
            return isCartEmpty ? "/Images/icon1.png" : "/Images/icon2.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}