using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Network;

namespace ResizingUi.ReadoutPanel
{
	class Entry : IDisposable
	{
		public readonly string Legend;
		public readonly string ReferenceString;
	    readonly IList<Node> dependencyNodes;
	    readonly ReadoutFetcher fetcher;
		readonly Color colour;

		public Entry (string legend, string referenceString, Color colour, IList<Node> dependencyNodes, ReadoutFetcher fetcher)
		{
			Legend = legend;
			ReferenceString = referenceString;
			this.dependencyNodes = new List<Node> (dependencyNodes);
			this.fetcher = fetcher;
			this.colour = colour;

			foreach (var node in dependencyNodes)
			{
				node.AttributesChanged += node_AttributesChanged;
			}
		}

		public void Dispose ()
		{
			foreach (var node in dependencyNodes)
			{
				node.AttributesChanged -= node_AttributesChanged;
			}
		}

		void node_AttributesChanged (Node sender, ArrayList attributes)
		{
			OnInvalidated();
		}

		public event EventHandler Invalidated;

		void OnInvalidated ()
		{
			Invalidated?.Invoke(this, new EventArgs ());
		}

		public string Value => fetcher(dependencyNodes);

		public Color Colour => colour;
	}
}