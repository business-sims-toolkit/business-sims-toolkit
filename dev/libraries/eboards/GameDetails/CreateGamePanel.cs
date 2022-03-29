using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using CommonGUI;
using CoreUtils;
using LibCore;
using Licensor;

namespace GameDetails
{
	public class CreateGamePanel : Panel
	{
		Field title;
		Field client;
			  
		Field region;
		Field country;
		Field location;
		Field venue;
			  
		Field players;
		Field purpose;
			  
		Field chargeCompany;

		Field notes;

		Field userName;
		Field password;

		Button ok;
		Button cancel;
		Button emergency;

		List<Field> fields;

		public event EventHandler CancelClicked;
		public event EventHandler OkClicked;
		public event EventHandler EmergencyClicked;

		IProductLicensor productLicensor;
		IProductLicence productLicence;

		Licensor.GameDetails gameDetails;

		public Licensor.GameDetails GameDetails
		{
			get
			{
				if (gameDetails == null)
				{
					UpdateDetails();
				}

				return gameDetails;
			}
		}

		public string Purpose => purpose.Value;
		public int Players => CONVERT.ParseIntSafe(players.Value, 0);

		List<string> regions;
		Dictionary<string, List<string>> regionToCountries;

		CreateGamePanelMode mode;

		public CreateGamePanel (IProductLicensor productLicensor, IProductLicence productLicence, UserDetails userDetails, CreateGamePanelMode mode = CreateGamePanelMode.Normal)
		{
			regions = new List<string> ();
			regionToCountries = new Dictionary<string, List<string>>();
			var xml = BasicXmlDocument.CreateFromFile(AppInfo.TheInstance.Location + @"\data\countries.xml");
			foreach (XmlElement regionElement in xml.SelectNodes("geography/region"))
			{
				var region = regionElement.Attributes["name"].Value;
				regions.Add(region);

				regionToCountries.Add(region, new List<string>());

				foreach (XmlElement countryElement in regionElement.SelectNodes("country"))
				{
					var country = countryElement.Attributes["name"].Value;
					regionToCountries[region].Add(country);
				}
			}

			this.productLicensor = productLicensor;
			this.productLicence = productLicence;
			this.mode = mode;

			title = Field.CreateTextField("Game Title");
			client = Field.CreateTextField("Client");
			region = Field.CreateComboBoxField("Region", regions);
			country = Field.CreateComboBoxField("Country", new string [0]);
			location = Field.CreateTextField("City");
			venue = Field.CreateTextField("Venue");
			players = Field.CreateFilteredTextField("Players", TextBoxFilterType.Digits);
			purpose = Field.CreateComboBoxField("Purpose", File.ReadAllLines(AppInfo.TheInstance.Location + @"\data\purposes.txt"));

			chargeCompany = Field.CreateTextField("Charge Company");
			chargeCompany.IsOptional = true;

			notes = Field.CreateTextField("Notes");
			notes.IsOptional = true;

			userName = Field.CreateTextField("TAC");
			userName.Value = productLicence?.UserDetails?.UserName ?? userDetails.UserName;
			userName.Enabled = false;

			password = Field.CreatePasswordField("Password");

			ok = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			ok.Text = "OK";
			ok.Click += ok_Click;

			cancel = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			cancel.Text = "Cancel";
			cancel.Click += cancel_Click;

			emergency = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			emergency.Text = "Emergency!";
			emergency.Click += emergency_Click;

			fields = new List<Field> (new [] { title, client, region, country, location, venue, players, purpose, chargeCompany, notes });
			if ((mode != CreateGamePanelMode.EmergencyGameCreation)
				&& (mode != CreateGamePanelMode.EmergencyActivationAndGameCreation))
			{
				fields.Add(userName);
				fields.Add(password);
			}

			foreach (var field in fields)
			{
				field.ValueChanged += field_ValueChanged;
				Controls.Add(field);
			}

			if (mode == CreateGamePanelMode.Unbilled)
			{
				foreach (var field in fields)
				{
					if (field != title)
					{
						field.IsOptional = true;
					}
				}
			}

			Controls.Add(ok);
			Controls.Add(emergency);
			emergency.Hide();
			Controls.Add(cancel);

			DoSize();
			UpdateFields(null);

			fields[0].Select();
			fields[0].Focus();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int margin = 0;
			var buttonSize = new Size((Width - (2 * margin)) / 5, fields[0].PreferredSize.Height);

			var heightAvailable = Height - fields.Sum(f => f.PreferredSize.Height);
			int rows = fields.Count;
			if (mode != CreateGamePanelMode.EmergencyActivationAndGameCreation)
			{
				heightAvailable -= buttonSize.Height;
				rows += 1;
			}
			float leading = heightAvailable / (float) (rows + 1);
			float y = leading;
			foreach (var field in fields)
			{
				field.FieldNameWidthFraction = ((mode == CreateGamePanelMode.EmergencyActivationAndGameCreation) ? 0.5f : 0.2f);
				field.Bounds = new Rectangle (margin, (int) y, Width - (2 * margin), field.PreferredSize.Height);
				y = field.Bottom + leading;
			}

			switch (mode)
			{
				case CreateGamePanelMode.EmergencyActivationAndGameCreation:
					ok.Hide();
					emergency.Hide();
					cancel.Hide();
					break;

				case CreateGamePanelMode.Normal:
				case CreateGamePanelMode.Unbilled:
					ok.Show();
					emergency.Hide();
					cancel.Show();
					break;

				case CreateGamePanelMode.EmergencyGameCreation:
					ok.Show();
					emergency.Show();
					cancel.Show();
					break;
			}

			var buttonGap = (Width - (2 * margin)) / 4;
			ok.Bounds = new Rectangle((Width / 2) - (buttonGap / 2) - buttonSize.Width, (int) y, buttonSize.Width, buttonSize.Height);
			emergency.Bounds = new Rectangle((Width / 2) - (buttonSize.Width / 2), (int) y, buttonSize.Width, buttonSize.Height);
			cancel.Bounds = new Rectangle((Width / 2) + (buttonGap / 2), (int) y, buttonSize.Width, buttonSize.Height);
		}

		void field_ValueChanged (object sender, EventArgs args)
		{
			UpdateFields(sender);
		}

		void UpdateFields (object sender)
		{
			if (mode == CreateGamePanelMode.Unbilled)
			{
				foreach (var field in fields)
				{
					if (field != title)
					{
						field.Enabled = false;
					}
				}

				ok.Enabled = true;
			}
			else
			{
				foreach (var field in fields)
				{
					if ((field != country)
						&& (field != location)
						&& (field != venue)
						&& (field != userName))
					{
						field.Enabled = true;
					}
				}

				if (string.IsNullOrEmpty(region.Value))
				{
					country.Enabled = false;
					location.Enabled = false;
					venue.Enabled = false;
				}
				else
				{
					country.Enabled = true;
					location.Enabled = true;
					venue.Enabled = true;

					if (sender == region)
					{
						country.ChangeValues(regionToCountries[region.Value]);
						country.Value = null;
					}
				}

				bool fieldsFilled = fields.All(f => f.IsOptional || !string.IsNullOrEmpty(f.Value));
				ok.Enabled = fieldsFilled;
				emergency.Enabled = fieldsFilled;
			}
		}

		void UpdateDetails ()
		{
			gameDetails = new Licensor.GameDetails (title.Value, venue.Value,
				location.Value, client.Value, region.Value, country.Value,
				chargeCompany.Value, notes.Value, purpose.Value, CONVERT.ParseIntSafe(players.Value, 0));
		}

		void ok_Click (object sender, EventArgs args)
		{
			UpdateDetails();

			if ((mode == CreateGamePanelMode.Normal)
				&& (productLicence != null)
				&& ! productLicence.VerifyPassword(password.Value))
			{
				password.SetError("Invalid password");
				return;
			}

			OnOkClicked();
		}

		public string Password => password.Value;

		void OnOkClicked ()
		{
			OkClicked?.Invoke(this, EventArgs.Empty);
		}

		void cancel_Click (object sender, EventArgs args)
		{
			OnCancelClicked();
		}

		void OnCancelClicked ()
		{
			CancelClicked?.Invoke(this, EventArgs.Empty);
		}

		void emergency_Click (object sender, EventArgs args)
		{
			OnEmergencyClicked();
		}

		void OnEmergencyClicked ()
		{
			EmergencyClicked?.Invoke(this, EventArgs.Empty);
		}

		public bool IsUnbillableMode => (mode == CreateGamePanelMode.Unbilled);
	}
}