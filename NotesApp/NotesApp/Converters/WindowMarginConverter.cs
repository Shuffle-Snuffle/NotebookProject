using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace NotesApp.Converters
{
    public class WindowMarginConverter : IValueConverter
    {
        private readonly Thickness normalMargin = new Thickness(10);
        private readonly Thickness maximizedMargin = new Thickness(0);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WindowState state)
            {
                return state == WindowState.Maximized ? maximizedMargin : normalMargin;
            }

            return normalMargin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
