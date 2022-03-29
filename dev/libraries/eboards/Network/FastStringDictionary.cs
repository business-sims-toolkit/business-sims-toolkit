using System;
using System.Collections.Generic;
using System.Text;

namespace Network
{
	/// <summary>
	/// A reimplementation of the .NET 1 StringDictionary, with the same semantics
	/// (case-insensitive key lookups, and returning null when looking up a missing key),
	/// but based on the generic Dictionary<> type, which doesn't generate garbage on calls to
	/// ContainsKey() (!).
	/// </summary>
	public class FastStringDictionary : Dictionary<string, string>
	{
		public FastStringDictionary ()
			: base (StringComparer.InvariantCultureIgnoreCase)
		{
		}

		new public string this[string key]
		{
			get
			{
				if (ContainsKey(key))
				{
					return base[key];
				}

				return null;
			}

			set
			{
				base[key] = value;
			}
		}
	}
}