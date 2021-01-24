using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MGS2Trainer
{
	public class DifficultyNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(byte))
			{
				byte index = (byte)value;
				return Trainer.Difficulties[index];
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(string))
            {
				return (byte)Trainer.Difficulties.IndexOf((string)value);
			}
			return false;
		}
	}



	public class DiffNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(byte))
			{
				byte index = (byte)value;
				return Trainer.Diffs[index];
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(string))
			{
				return (byte)Trainer.Diffs.IndexOf((string)value);
			}
			return false;
		}
	}

	public class DifficultyColorConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(byte))
			{
				byte index = (byte)value;
				var brush = Trainer.DiffBrushes[index];
				return brush;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(string))
			{
				return (byte)Trainer.DiffBrushes.IndexOf((Brush)value);
			}
			return false;
		}
	}
}