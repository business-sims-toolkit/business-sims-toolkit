using System;
using System.Collections;
using System.Xml;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using LibCore;

namespace CoreUtils
{
	public sealed class SkinningDefs
	{
		Hashtable data = new Hashtable();

		static SkinningDefs theInstance = null;

		public static SkinningDefs TheInstance
		{
			get
			{
				if (theInstance == null)
				{
					theInstance = new SkinningDefs();
				}

				return theInstance;
			}
		}

		/// <summary>
		/// Awful bodge to allow us to reload the skinning data if the singleton was instantiated
		/// before the app had unzipped the skin file!
		/// </summary>
		public static void Reload ()
		{
			theInstance = null;
		}

		public void Reload (string path)
		{
			data.Clear();

			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(path);

			foreach (XmlNode n in xdoc.DocumentElement.ChildNodes)
			{
				if (n.NodeType == XmlNodeType.Element)
				{
					data.Add(n.Name, n.InnerText);
				}
			}
		}

		SkinningDefs ()
		{
			string file = AppInfo.TheInstance.Location + "\\data\\skin.xml";

			if (File.Exists(file))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(file);
				//
				foreach (XmlNode n in xdoc.DocumentElement.ChildNodes)
				{
					if (n.NodeType == XmlNodeType.Element)
					{
						data.Add(n.Name, n.InnerText);
					}
				}
			}
		}

		public string GetData (string val, string def)
		{
			if (data.ContainsKey(val))
			{
				return (string) data[val];
			}

			return def;
		}

		public string GetData (string val)
		{
			return GetData(val, "");
		}

		public int GetIntData (string val, int defaultVal)
		{
			int defval = defaultVal;
			if (data.ContainsKey(val))
			{
				defval = CONVERT.ParseInt((string) data[val]);
			}

			return defval;
		}

		public int GetIntData (string val)
		{
			return GetIntDataNoDefault(val) ?? 0;
		}

		public int? GetIntDataNoDefault (string val)
		{
			if (data.ContainsKey(val))
			{
				return CONVERT.ParseInt((string) data[val]);
			}
			return null;
		}

		static Color ExtractColor (string rgbstr)
		{
			if (rgbstr == string.Empty)
			{
				return Color.White;
			}

			return CONVERT.ParseColor(rgbstr, Color.White);
		}

		public Color? GetColourData (string name, Color? defaultValue = null)
		{
			if (data.ContainsKey(name))
			{
				if (string.IsNullOrEmpty((string) data[name]))
				{
					return Color.Transparent;
				}

				return ExtractColor((string) data[name]);
			}

			return defaultValue;
		}

		public Color GetColorData (string val)
		{
			return GetColorDataGivenDefault(val, Color.White);
		}

		public Color GetColorData (string val, Color defaultValue)
		{
			return GetColorDataGivenDefault(val, defaultValue);
		}

		public Color GetColorData (string val, string defaultValue)
		{
			return GetColorDataGivenDefault(val, defaultValue);
		}

		public Color GetColorDataGivenDefault (string val, string defaultVal)
		{
			if (data.ContainsKey(val))
			{
				return ExtractColor((string) data[val]);
			}

			return ExtractColor(defaultVal);
		}

		public Color GetColorDataGivenDefault (string val, Color defaultVal)
		{
			if (data.ContainsKey(val))
			{
				return CONVERT.ParseColor((string) data[val], defaultVal);
			}

			return defaultVal;
		}

		public bool GetBoolData (string val, bool defaultVal)
		{
			bool defval = defaultVal;
			if (data.ContainsKey(val))
			{
				string bvalue_str = (string) data[val];
				if (bvalue_str != "")
				{
					bvalue_str = bvalue_str.ToLower();
					defval = (bvalue_str.IndexOf("true")) > -1;
				}
			}
			return defval;
		}

		public float GetFloatData (string val, float defaultVal)
		{
			return (float) GetDoubleData(val, defaultVal);
		}

		public double GetDoubleData (string val, double defaultVal)
		{
			string stringVal = GetData(val);

			return CONVERT.ParseDoubleSafe(stringVal, defaultVal);
		}

		public string GetFontName ()
		{
			return GetData("fontname");
		}

		public Font GetPixelSizedFont (float size, FontStyle style = FontStyle.Regular)
		{
			return ConstantSizeFont.NewFontPixelSized(GetFontName(), size, style);
		}

		public Font GetFont (float size, FontStyle style)
		{
			return ConstantSizeFont.NewFont(GetFontName(), size, style);
		}

		public Font GetFont (float size)
		{
			return GetFont(size, FontStyle.Regular);
		}

		public Font GetFontWithStyle (string name)
		{
			var fontSizeAndStyleSplit = GetData(name).Split(',');

			if (float.TryParse(fontSizeAndStyleSplit[0], out var fontSize))
			{
				if (fontSize <= 0)
				{
					throw new Exception("Invalid font size.");
				}

				var style = fontSizeAndStyleSplit.Length == 2 ? fontSizeAndStyleSplit[1] : null;

				return GetFont(fontSize, ParseFontStyle(style, FontStyle.Regular));
			}
			else
			{
				throw new Exception("Font size missing");
			}
		}

		public FontStyle GetFontStyle (string type, FontStyle defaultStyle = FontStyle.Regular)
		{
			return ParseFontStyle(GetData(type), defaultStyle);
		}

		static FontStyle ParseFontStyle (string value, FontStyle defaultStyle)
		{
			return Enum.TryParse<FontStyle>(value, true, out var style) ? style : defaultStyle;
		}

		public Point GetPointData (string name, int x, int y)
		{
			return GetPointData(name, new Point(x, y));
		}

		public Point GetPointData (string name, Point point)
		{
			return CONVERT.ParsePoint(GetData(name, CONVERT.ToStr(point)));
		}

		public PointF GetPointFData (string name, float x, float y)
		{
			return GetPointFData(name, new PointF(x, y));
		}

		public PointF GetPointFData (string name, PointF point)
		{
			return CONVERT.ParsePointF(GetData(name, CONVERT.ToStr(point)));
		}

		public Size GetSizeData (string name, int w, int h)
		{
			return GetSizeData(name, new Size(w, h));
		}

		public Size GetSizeData (string name, Size size)
		{
			return CONVERT.ParseSize(GetData(name, CONVERT.ToStr(size)));
		}

		public SizeF GetSizeFData (string name, float w, float h)
		{
			return GetSizeFData(name, new SizeF(w, h));
		}

		public SizeF GetSizeFData (string name, SizeF size)
		{
			return CONVERT.ParseSizeF(GetData(name, CONVERT.ToStr(size)));
		}

		public string GetCurrencySymbol ()
		{
			return GetData("currency_symbol", "$");
		}

		public string GetCurrencyString (double amount, int decimalPlaces = 0)
		{
			return CONVERT.Format("{0}{1}{2}", (amount < 0) ? "-" : "", GetCurrencySymbol(),
				CONVERT.ToPaddedStrWithThousands(Math.Abs(amount), decimalPlaces));
		}

		public T GetEnum<T> (string name, T defaultValue) where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
				throw new ArgumentException("T must be an enumerated type");

			var valueStr = GetData(name);

			if (Enum.TryParse(valueStr, true, out T value))
			{
				return value;
			}

			return defaultValue;
		}

		public Button CreateWindowsButton (FontStyle style = FontStyle.Regular, int? fontSize = null)
		{
			if (fontSize == null)
			{
				fontSize = SkinningDefs.TheInstance.GetIntData("windows_button_text_size", 9);
			}

			var button = new Button
			{
				Font = GetPixelSizedFont(fontSize.Value, style)
			};

			if (GetBoolData("windows_buttons_styled", true))
			{
				button.BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
				button.ForeColor =
					SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
			}
			else
			{
				button.BackColor = Color.LightGray;
			}

			if (! GetBoolData("windows_button_border", true))
			{
				button.FlatStyle = FlatStyle.Flat;
				button.FlatAppearance.BorderSize = 0;
			}

			return button;
		}

		public Label CreateLabel (string text, float fontSize = 10, FontStyle style = FontStyle.Regular)
		{
			return new Label { Font = GetFont(fontSize, style), Text = text };
		}

		public TextBox CreateTextBox (float fontSize = 10, FontStyle style = FontStyle.Regular)
		{
			return new TextBox { Font = GetFont(fontSize, style) };
		}
	}
}