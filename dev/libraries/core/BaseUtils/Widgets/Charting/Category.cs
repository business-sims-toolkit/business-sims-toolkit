using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Defines an entry in a Categorical scale.
	/// </summary>
	public class Category
	{
		string label;
		int index;
		Color color;
		Image image;

		/// <summary>
		/// Creates an instance of Category.
		/// </summary>
		/// <param name="label">The category label.</param>
		public Category(string label)
		{
			this.label = label;
			this.Color = Color.Red;
			this.image = null;
		}

		/// <summary>
		/// Creates an instance of Category.
		/// </summary>
		/// <param name="label">The category label.</param>
		/// <param name="color">The category colour.</param>
		public Category(string label, Color color)
		{
			this.label = label;
			this.color = color;
			this.image = null;
		}

		/// <summary>
		/// The category label.
		/// </summary>
		public string Label
		{
			get { return label; }
			set { label = value; }
		}

		/// <summary>
		/// The category index.
		/// </summary>
		public int Index
		{
			get { return index; }
			set { index = value; }
		}

		/// <summary>
		/// The category colour.
		/// </summary>
		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		/// <summary>
		/// The image associated with the category.
		/// </summary>
		public Image Image
		{
			get { return image; }
			set { image = value; }
		}
	}
}
