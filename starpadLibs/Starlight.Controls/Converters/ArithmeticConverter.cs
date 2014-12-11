#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Text.RegularExpressions;
#endregion

namespace Taloware.Starlight.Controls.Converters
{
	// Source: http://learnwpf.com/Posts/Post.aspx?postId=9b745fe8-7d51-4d01-a8c7-f31083c4be94
	public class ArithmeticConverter : IValueConverter
	{
		#region Fields

		private const string ArithmeticParseExpression = "([+\\-*/]{1,1})\\s{0,}(\\-?[\\d\\.]+)";
		private Regex arithmeticRegex = new Regex(ArithmeticParseExpression);

		#endregion

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is double && parameter != null)
			{
				string param = parameter.ToString();

				if (param.Length > 0)
				{
					Match match = arithmeticRegex.Match(param);
					if (match != null && match.Groups.Count == 3)
					{
						string operation = match.Groups[1].Value.Trim();
						string numericValue = match.Groups[2].Value;

						double number = 0;
						if (double.TryParse(numericValue, out number)) // this should always succeed or our regex is broken
						{
							double valueAsDouble = (double)value;
							double returnValue = 0;

							switch (operation)
							{
								case "+":
									returnValue = valueAsDouble + number;
									break;

								case "-":
									returnValue = valueAsDouble - number;
									break;

								case "*":
									returnValue = valueAsDouble * number;
									break;

								case "/":
									returnValue = valueAsDouble / number;
									break;
							}

							return returnValue;
						}
					}
				}
			}

			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}

		#endregion
	}
}
