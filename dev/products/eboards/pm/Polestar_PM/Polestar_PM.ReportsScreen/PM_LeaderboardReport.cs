using System;
using System.Collections;
using System.Collections.Generic;

using System.Drawing;

using System.IO;
using System.Text;
using System.Xml;

using CoreUtils;
using Network;
using GameManagement;

namespace Polestar_PM.ReportsScreen
{
	public class PM_LeaderboardReport
	{
		class BusinessComparerByTransactions : IComparer<string>
		{
			Dictionary<string, int> businessNameToTransactions;

			public BusinessComparerByTransactions (Dictionary<string, int> businessNameToTransactions)
			{
				this.businessNameToTransactions = businessNameToTransactions;
			}

			public int Compare (string businessA, string businessB)
			{
				return businessNameToTransactions[businessB] - businessNameToTransactions[businessA];
			}
		}

		public PM_LeaderboardReport ()
		{
		}

		public string BuildReport (NetworkProgressionGameFile gameFile, int round)
		{
			string headingBackColour = "85,183,221";
			string headingForeColour = "255,255,255";

			string highlightBackColour = "0,66,109";
			string highlightForeColour = "255,255,255";

			int rowHeight = CoreUtils.SkinningDefs.TheInstance.GetIntData("table_row_height", 19);

			LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create();
			XmlElement table = CreateChild(doc, "table");

			string resultsFile = gameFile.GetRoundFile(round, "Results.xml", GameManagement.GameFile.GamePhase.OPERATIONS);
			if (File.Exists(resultsFile))
			{
				LibCore.BasicXmlDocument results = LibCore.BasicXmlDocument.CreateFromFile(resultsFile);

				table.SetAttribute("columns", "5");
				table.SetAttribute("widths", "0.1,0.3,0.2,0.2,0.2");
				table.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
				table.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "255,255,255"));
				table.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
				table.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

				string padding = "   "; //3 spaces

				XmlElement headerRow = CreateChild(table, "rowdata");
				CreateCellChild(headerRow, padding + "Position", ContentAlignment.MiddleLeft, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
				CreateCellChild(headerRow, padding + "Business Name", ContentAlignment.MiddleLeft, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
				CreateCellChild(headerRow, "Total Transactions"+padding, ContentAlignment.MiddleRight, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
				CreateCellChild(headerRow, "Market Share" + padding, ContentAlignment.MiddleRight, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
				CreateCellChild(headerRow, "Revenue" + padding, ContentAlignment.MiddleRight, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
				headerRow.SetAttribute("colour", headingBackColour);
				headerRow.SetAttribute("textcolour", headingForeColour);


				foreach (XmlNode business in results.DocumentElement.ChildNodes)
				{
					ArrayList cells = new ArrayList();

					XmlElement row = CreateChild(table, "rowdata");
					cells.Add(row);
					cells.Add(CreateCellChild(row, padding + business.Attributes["rank"].Value, ContentAlignment.MiddleLeft, FontStyle.Regular));
					cells.Add(CreateCellChild(row, padding + business.Attributes["name"].Value, ContentAlignment.MiddleLeft, FontStyle.Regular));
					cells.Add(CreateCellChild(row, FormatThousands(LibCore.CONVERT.ParseIntSafe(business.Attributes["transactions"].Value, 0))+padding, ContentAlignment.MiddleRight, FontStyle.Regular));
					cells.Add(CreateCellChild(row, business.Attributes["market_share"].Value + "%" + padding, ContentAlignment.MiddleRight, FontStyle.Regular));
					cells.Add(CreateCellChild(row, FormatMoney(LibCore.CONVERT.ParseIntSafe(business.Attributes["revenue"].Value, 0)) + padding, ContentAlignment.MiddleRight, FontStyle.Regular));

					XmlAttribute isPlayerAttribute = business.Attributes["is_player"];
					bool isPlayer = false;
					if (isPlayerAttribute != null)
					{
						isPlayer = LibCore.CONVERT.ParseBool(isPlayerAttribute.Value, false);
					}

					if (isPlayer)
					{
						foreach (XmlElement cell in cells)
						{
							cell.SetAttribute("colour", highlightBackColour);
							cell.SetAttribute("textcolour", highlightForeColour);
						}
					}
				}
			}

			string filename = gameFile.GetRoundFile(round, "LeaderboardReport.xml", GameFile.GamePhase.OPERATIONS);
			if (Directory.Exists(Path.GetDirectoryName(filename)))
			{
				doc.Save(filename);
			}
			else
			{
				filename = "";
			}

			return filename;
		}

		XmlElement CreateChild (XmlDocument doc, string name)
		{
			XmlElement element = doc.CreateElement(name);
			doc.AppendChild(element);

			return element;
		}

		XmlElement CreateChild (XmlNode parent, string name)
		{
			XmlElement element = parent.OwnerDocument.CreateElement(name);
			parent.AppendChild(element);

			return element;
		}

		XmlElement CreateCellChild (XmlNode parent, string content)
		{
			return CreateCellChild(parent, content, ContentAlignment.MiddleCenter, FontStyle.Regular);
		}

		XmlElement CreateCellChild (XmlNode parent, string content, ContentAlignment alignment, FontStyle style)
		{
			XmlElement element = parent.OwnerDocument.CreateElement("cell");
			parent.AppendChild(element);
			element.SetAttribute("val", content);

			string alignString = "";
			switch (alignment)
			{
				case ContentAlignment.BottomCenter:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.TopCenter:
					alignString = "";
					break;

				case ContentAlignment.BottomLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.TopLeft:
					alignString = "left";
					break;

				case ContentAlignment.BottomRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.TopRight:
					alignString = "right";
					break;
			}

			if (alignString != "")
			{
				element.SetAttribute("align", alignString);
			}

			if (style == FontStyle.Bold)
			{
				element.SetAttribute("textstyle", "bold");
			}

			return element;
		}

		string FormatThousands (int a)
		{
			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder("");
			int digits = 0;
			for (int character = raw.Length - 1; character >= 0; character--)
			{
				builder.Insert(0, raw[character]);
				digits++;

				if (((digits % 3) == 0) && (character > 0))
				{
					builder.Insert(0, ",");
				}
			}

			return builder.ToString();
		}

		string FormatMoney (int a)
		{
			return "$" + FormatThousands(a);
		}

		public string BuildYearlyReport (NetworkProgressionGameFile gameFile)
		{
			string headingBackColour = "85,183,221";
			string headingForeColour = "255,255,255";

			string highlightBackColour = "0,66,109";
			string highlightForeColour = "255,255,255";

			int rowHeight = CoreUtils.SkinningDefs.TheInstance.GetIntData("table_row_height", 19);

			LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create();
			XmlElement table = CreateChild(doc, "table");

			LibCore.BasicXmlDocument[] results = new LibCore.BasicXmlDocument[gameFile.LastRoundPlayed];
			for (int round = 0; round < gameFile.LastRoundPlayed; round++)
			{
				string resultsFile = gameFile.GetRoundFile(round + 1, "Results.xml", GameManagement.GameFile.GamePhase.OPERATIONS);

				if (File.Exists(resultsFile))
				{
					results[round] = LibCore.BasicXmlDocument.CreateFromFile(resultsFile);
				}
			}

			table.SetAttribute("columns", "3");
			table.SetAttribute("widths", "0.08,0.31,0.31,0.31");
			table.SetAttribute("rowheight", LibCore.CONVERT.ToStr(rowHeight));
			table.SetAttribute("border_colour", CoreUtils.SkinningDefs.TheInstance.GetData("table_border_colour", "255,255,255"));
			table.SetAttribute("row_colour_1", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour", "255,255,255"));
			table.SetAttribute("row_colour_2", CoreUtils.SkinningDefs.TheInstance.GetData("table_row_colour_alternate", "255,255,255"));

			XmlElement headerRow = CreateChild(table, "rowdata");
			CreateCellChild(headerRow, "Position", ContentAlignment.MiddleCenter, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
			CreateCellChild(headerRow, "Business Name", ContentAlignment.MiddleCenter, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
			CreateCellChild(headerRow, "Total Transactions", ContentAlignment.MiddleCenter, FontStyle.Bold).SetAttribute("textcolour", headingForeColour);
			headerRow.SetAttribute("colour", headingBackColour);
			headerRow.SetAttribute("textcolour", headingForeColour);

			string playerName = "";

			Dictionary<string, int> businessNameToTransactions = new Dictionary<string, int> ();

			for (int round = 0; round < results.Length; round++)
			{
				if (results[round] != null)
				{
					foreach (XmlNode business in results[round].DocumentElement.ChildNodes)
					{
						string name = business.Attributes["name"].Value;
						int transactions = LibCore.CONVERT.ParseIntSafe(business.Attributes["transactions"].Value, 0);

						if (!businessNameToTransactions.ContainsKey(name))
						{
							businessNameToTransactions.Add(name, transactions);
						}
						else
						{
							businessNameToTransactions[name] += transactions;
						}

						XmlAttribute isPlayerAttribute = business.Attributes["is_player"];
						if (isPlayerAttribute != null)
						{
							if (LibCore.CONVERT.ParseBool(isPlayerAttribute.Value, false))
							{
								playerName = name;
							}
						}
					}
				}
			}

			List<string> businessNamesOrderedByTransactions = new List<string> (businessNameToTransactions.Keys);
			businessNamesOrderedByTransactions.Sort(new BusinessComparerByTransactions (businessNameToTransactions));

			int rank = 0;
			int position = 0;
			int previousTransactions = 0;
			foreach (string businessName in businessNamesOrderedByTransactions)
			{
				int transactions = businessNameToTransactions[businessName];
				position++;

				if ((rank == 0) || (previousTransactions != transactions))
				{
					rank = position;
				}
				previousTransactions = transactions;

				ArrayList cells = new ArrayList ();

				XmlElement row = CreateChild(table, "rowdata");
				cells.Add(row);
				cells.Add(CreateCellChild(row, LibCore.CONVERT.ToStr(rank)));
				cells.Add(CreateCellChild(row, businessName));
				cells.Add(CreateCellChild(row, FormatThousands(transactions)));

				if (businessName == playerName)
				{
					foreach (XmlElement cell in cells)
					{
						cell.SetAttribute("colour", highlightBackColour);
						cell.SetAttribute("textcolour", highlightForeColour);
					}
				}
			}

			string filename = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "YearlyLeaderboardReport.xml", GameFile.GamePhase.OPERATIONS);
			doc.Save(filename);

			return filename;
		}
	}
}