using samrtCheckPF.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace samrtCheckPF
{
    class BGConvert : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {

            //if ((int)value > 90)
            if ((string)value == GlobalValue.ALG_RESULT_TRUE)
                {

                return GlobalValue.BRUSH_PASS_COLOR;

            }
            else if ((string)value == GlobalValue.ALG_RESULT_FALSE)
            {

                return GlobalValue.BRUSH_FAIL_COLOR;

            }

            else

                return new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)

        {

            throw new NotImplementedException();

        }
    }
}
