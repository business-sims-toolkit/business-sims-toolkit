using System;
using System.Drawing;
using System.Windows.Forms;
using Network;
using LibCore;

namespace NodeTreeViewer
{
	/// <summary>
	/// Summary description for NodeView.
	/// </summary>
	public class NodeView : BasePanel
	{
		public enum Style
		{
			FULL,
			FROMLINK,
			TOLINK,
			TONODE,
			PARENT,
			CHILD
		};

		protected Node _node;
		protected Label title;

		public delegate void NodeClickedHandler(NodeView sender, Node n);
		public event NodeClickedHandler NodeClicked;

		protected Color color = Color.LightSteelBlue;

		public NodeView(Node n, Style s)
		{
			string model_uuid = n.Tree.uuid;

			this.BorderStyle = BorderStyle.FixedSingle;
			if(s == Style.TOLINK) color = Color.LightYellow;
			else if(s == Style.FROMLINK) color = Color.LightGreen;
			else if(s == Style.PARENT) color = Color.SteelBlue;
			else if(s == Style.CHILD) color = Color.Tan;

			_node = n;
			title = new Label();
			title.BackColor = color;
			title.Text = n.GetAttribute("name");
			title.Size = new Size(300,30);
			title.TextAlign = ContentAlignment.MiddleCenter;
			title.Cursor = Cursors.UpArrow;
			title.Click += title_Click;
			this.Controls.Add(title);

			int y = 30;

			// Always put type first and then alphabetically list the remaining.
			Label att = new Label();
			att.Text = "type";
			att.Size = new Size(150,20);
			att.Location = new Point(0,y);
			att.BackColor = color;
			this.Controls.Add(att);
			//
			Label val = new Label();
			val.Text = n.GetAttribute("type");
			val.Size = new Size(150,20);
			val.Location = new Point(150,y);
			val.BackColor = color;
			this.Controls.Add(val);
			//
			y += 20;
			//
			// Always put ID next.
			//
			Label id = new Label();
			id.Text = "ID";
			id.Size = new Size(150,20);
			id.Location = new Point(0,y);
			id.BackColor = color;
			this.Controls.Add(id);
			//
			val = new Label();
			val.Text = CONVERT.ToStr(n.ID);
			val.Size = new Size(150,20);
			val.Location = new Point(150,y);
			val.BackColor = color;
			this.Controls.Add(val);
			//
			y += 20;
			int i = 0;
			Color [] colours = new Color [] { Color.White, Color.LightGray };
			//
			if(s == Style.FULL)
			{
				foreach(AttributeValuePair avp in n.AttributesAsArrayList)
				{
					if(avp.Attribute != "type")
					{
						Color colour = colours[i % colours.Length];

						att = new Label();
						att.Text = avp.Attribute;
						att.Location = new Point(0,y);
						att.BackColor = colour;
						this.Controls.Add(att);
						//
						val = new Label();
						val.Text = avp.Value;
						val.Location = new Point(150,y);
						val.BackColor = colour;
						this.Controls.Add(val);
						//
						System.Drawing.Size defaultSize = new System.Drawing.Size (150, 1);
						int height = Math.Max(att.GetPreferredSize(defaultSize).Height, val.GetPreferredSize(defaultSize).Height);
						att.Size = new Size(150, height);
						val.Size = new Size(150,height);
						y = att.Bottom;
						i++;

						Node target = n.Tree.GetNamedNode(avp.Value);
						if ((target != null) && (target != n))
						{
							val.Font = new Font(val.Font, FontStyle.Underline);
							val.ForeColor = Color.Blue;
							val.Click += val_Click;
						}
					}
				}
			}
			//
			this.Size = new Size(300,y);
		}

		void val_Click (object sender, EventArgs e)
		{
			Label label = (Label) sender;
			string name = label.Text;
			Node target = _node.Tree.GetNamedNode(name);

			NodeClicked(this, target);
		}

		void title_Click(object sender, EventArgs e)
		{
			if(null != NodeClicked)
			{
				NodeClicked(this,this._node);
			}
		}
	}
}