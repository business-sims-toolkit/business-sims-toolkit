using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using Network;
using LibCore;

namespace NodeTreeViewer
{
	/// <summary>
	/// Summary description for GraphView.
	/// </summary>
	public class GraphView : BasePanel
	{
		protected Node _node;

		public delegate void NodeClickedHandler(GraphView sender, Node n);
		public event NodeClickedHandler NodeClicked;

		protected Control parent;
		protected Control main;
		protected ArrayList froms = new ArrayList();
		protected ArrayList tos = new ArrayList();
		protected ArrayList node_tos = new ArrayList();
		protected ArrayList tos_views = new ArrayList();
		protected ArrayList children = new ArrayList();

		public GraphView(Node n)
		{
			this.BackColor = Color.White;
			_node = n;
			this.AutoScroll = true;
			this.SetStyle(ControlStyles.Selectable,true);

			Node nparent = n.Parent;
			NodeView nvp = null;

			int centre = 150 + 350;

			PictureBox pb;

			if(null != nparent)
			{
				nvp = new NodeView(nparent,NodeView.Style.PARENT);
				nvp.Location = new Point(centre-150,5);
				nvp.NodeClicked += nvc_NodeClicked;
				this.Controls.Add(nvp);
				//
				pb = new PictureBox();
				pb.Size = new Size(40,40);
				pb.Location = new Point(nvp.Location.X + nvp.Width + 5,nvp.Top);
				pb.BorderStyle = BorderStyle.FixedSingle;
				pb.Image = BrowserIcons.TheInstance.GetImage(nparent.GetAttribute("type"));
				pb.SizeMode = PictureBoxSizeMode.StretchImage;
				this.Controls.Add(pb);
				//
				parent = nvp;
			}

			NodeView nv = new NodeView(n,NodeView.Style.FULL);
			nv.NodeClicked += nvc_NodeClicked;

			if(null != nvp)
			{
				nv.Location = new Point(centre-150,nvp.Height + 50);
			}

			this.Controls.Add(nv);

			//
			pb = new PictureBox();
			pb.Size = new Size(40,40);
			pb.Location = new Point(nv.Location.X + nv.Width + 5,nv.Top);
			pb.BorderStyle = BorderStyle.FixedSingle;
			pb.Image = BrowserIcons.TheInstance.GetImage(n.GetAttribute("type"));
			pb.SizeMode = PictureBoxSizeMode.StretchImage;
			this.Controls.Add(pb);
			//

			main = nv;

			// Do back and to links...

			int newTop = nv.Top;// + nv.Height + 50;

			int newBackLinkTop = newTop;

			int numBackLinks = 0;

			foreach(LinkNode link in n.BackLinks)
			{
				NodeView nvc = new NodeView(link,NodeView.Style.FROMLINK);
				nvc.Location = new Point(centre - 150 - 350,newBackLinkTop);
				this.Controls.Add(nvc);
				nvc.NodeClicked += nvc_NodeClicked;
				numBackLinks++;
				newBackLinkTop += nvc.Height + 50;

				froms.Add(nvc);
			}

			int possTop = nv.Top + nv.Height + 50;
			newTop = newBackLinkTop;

			if(possTop > newBackLinkTop)
			{
				newTop = possTop;
			}

			//

			int newToLinkTop = nv.Top;

			foreach(Node cn in n)
			{
				LinkNode l = cn as LinkNode;
				if(null != l)
				{
					NodeView nvc = new NodeView(l,NodeView.Style.TOLINK);
					nvc.Location = new Point(centre + 250,newToLinkTop);
					this.Controls.Add(nvc);
					nvc.NodeClicked += nvc_NodeClicked;
					newToLinkTop += nvc.Height + 10;

					tos.Add(nvc);
					node_tos.Add(l);

					//
					pb = new PictureBox();
					pb.Size = new Size(40,40);
					pb.Location = new Point(nvc.Location.X + nvc.Width + 5,nvc.Top);
					pb.BorderStyle = BorderStyle.FixedSingle;
					pb.Image = BrowserIcons.TheInstance.GetImage(l.GetAttribute("type"));
					pb.SizeMode = PictureBoxSizeMode.StretchImage;
					this.Controls.Add(pb);
					//

					if(null != l.To)
					{
						NodeView nvc2 = new NodeView(l.To,NodeView.Style.TONODE);
						nvc2.Location = new Point(centre + 300,newToLinkTop);
						this.Controls.Add(nvc2);
						nvc2.NodeClicked += nvc_NodeClicked;
						newToLinkTop += nvc2.Height + 10;

						tos_views.Add(nvc2);

						//
						pb = new PictureBox();
						pb.Size = new Size(40,40);
						pb.Location = new Point(nvc2.Location.X + nvc2.Width + 5,nvc2.Top);
						pb.BorderStyle = BorderStyle.FixedSingle;
						pb.Image = BrowserIcons.TheInstance.GetImage(l.To.GetAttribute("type"));
						pb.SizeMode = PictureBoxSizeMode.StretchImage;
						this.Controls.Add(pb);
						//
					}
					else
					{
						tos_views.Add(null);
						newToLinkTop += 10;
					}
				}
			}

			if(newToLinkTop > newTop)
			{
				newTop = newToLinkTop;
			}

			// end of links.

			//
			bool left = true;
			int leftYoff = newTop;
			//
			foreach(Node child in n)
			{
				if(!node_tos.Contains(child))
				{
					NodeView nvc = new NodeView(child,NodeView.Style.CHILD);
					//
					if(left)
					{
						if(leftYoff != newTop)
						{
							newTop = leftYoff;
						}
						nvc.Location = new Point(200-25,newTop);
						leftYoff = newTop + nvc.Height + 10;
					}
					else
					{
						nvc.Location = new Point(500+25,newTop);
					}
					//
					this.Controls.Add(nvc);
					nvc.NodeClicked += nvc_NodeClicked;
					//xoff += 350;

					children.Add(nvc);

					left = !left;
				}
			}
		}

		void nvc_NodeClicked(NodeView sender, Node n)
		{
			if(null != NodeClicked)
			{
				NodeClicked(this,n);
			}
		}
	
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint (e);

			if(main != null)
			{
				Point centre = new Point(main.Left+main.Width/2, main.Top+main.Height/2);

				if(parent != null)
				{
					Point pcentre = new Point(parent.Left+parent.Width/2, parent.Top+parent.Height/2);
					e.Graphics.DrawLine(Pens.Black, centre.X, centre.Y, pcentre.X, pcentre.Y);
				}

				foreach(Control c in tos)
				{
					e.Graphics.DrawLine(Pens.Black, centre.X, main.Top+55, main.Left+main.Width+55, main.Top+55);
					e.Graphics.DrawLine(Pens.Black, main.Left+main.Width+55, main.Top+55, main.Left+main.Width+55, c.Top+c.Height/2);
					e.Graphics.DrawLine(Pens.Black, main.Left+main.Width+55, c.Top+c.Height/2, c.Left, c.Top+c.Height/2);

					e.Graphics.DrawLine(Pens.Black, c.Left+c.Width/2, c.Top+c.Height, c.Left+c.Width/2, c.Top+c.Height+10);
				}

				foreach(Control c in froms)
				{
					e.Graphics.DrawLine(Pens.Black, c.Left+c.Width, c.Top+c.Height/2, main.Left, centre.Y);
				}
			}
		}
	}
}
