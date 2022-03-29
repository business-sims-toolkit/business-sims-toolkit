using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Licensor;
using ResizingUi;

namespace GameDetails
{
	public class ProductActivationWindow : Form
	{
		RichTextBox eulaBox;
		Button eulaConsentButton;

		RichTextBox gdprBox;
		Button gdprConsentButton;

		Field userName;
		Field password;
		Field givenName;
		Field familyName;
		Field companyName;
		Field email;
		Field masterTrainer;
		List<Field> fields;

		Button ok;
		Button cancel;
		Button emergency;
		bool choiceButtonClicked;

		CreateGamePanel createEmergencyGamePanel;
		EmergencyActivationCodesPanel emergencyCodesPanel;
		PicturePanel image;

		bool showEmergencyButton;

		IProductLicensor productLicensor;

		UserDetails userDetails;
		public UserDetails UserDetails => userDetails;

		Licensor.GameDetails gameDetails;
		public Licensor.GameDetails GameDetails => gameDetails;

		public bool ShowEmergencyButton
		{
			get => showEmergencyButton;

			set
			{
				showEmergencyButton = value;
				DoSize();
			}
		}

		public string ResponseCode => emergencyCodesPanel?.ResponseCode;

		bool isInEmergencyMode;
		public bool IsInEmergencyMode => isInEmergencyMode;

		public ProductActivationWindow (IProductLicensor productLicensor, UserDetails userDetails = null)
		{
			this.productLicensor = productLicensor;

			eulaBox = new ReadOnlyRichTextBox ();
			eulaBox.LoadFile(AppInfo.TheInstance.Location + @"\data\eula.rtf");
			Controls.Add(eulaBox);

			eulaConsentButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			eulaConsentButton.Text = "I agree to the EULA";
			eulaConsentButton.Click += eulaConsentButton_Click;
			Controls.Add(eulaConsentButton);

			gdprBox = new ReadOnlyRichTextBox ();
			gdprBox.LoadFile(AppInfo.TheInstance.Location + @"\data\GDPR.rtf");
			Controls.Add(gdprBox);

			gdprConsentButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			gdprConsentButton.Text = "I accept the policy";
			gdprConsentButton.Click += gdprConsentButton_Click;
			Controls.Add(gdprConsentButton);

			fields = new List<Field> ();

			userName = Field.CreateTextField("Trainer Account Code (TAC)");
			fields.Add(userName);

			password = Field.CreatePasswordField("Password");
			fields.Add(password);

			givenName = Field.CreateTextField("Given Name");
			fields.Add(givenName);

			familyName = Field.CreateTextField("Family Name");
			fields.Add(familyName);

			companyName = Field.CreateTextField("Company Name");
			fields.Add(companyName);

			email = Field.CreateTextField("Email");
			fields.Add(email);

			masterTrainer = Field.CreateTextField("Master Trainer's Name");
			fields.Add(masterTrainer);

			foreach (var field in fields)
			{
				Controls.Add(field);
				field.ValueChanged += field_ValueChanged;
			}

			ok = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			ok.Text = "OK";
			ok.Click += ok_Click;
			Controls.Add(ok);

			cancel = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			cancel.Text = "Cancel";
			cancel.Click += cancel_Click;
			Controls.Add(cancel);

			emergency = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			emergency.Text = "Emergency!";
			emergency.Click += emergency_Click;
			Controls.Add(emergency);

			image = new PicturePanel();
			image.ZoomWithLetterboxing(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\main_logo.png"));
			image.BackColor = BackColor;
			Controls.Add(image);

			Text = "Activate Product License";

			FormBorderStyle = FormBorderStyle.FixedDialog;
			StartPosition = FormStartPosition.CenterScreen;
			Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);

			Size = new Size (1024, 768);

			UpdateButtons();

			fields[0].Select();

			if (userDetails != null)
			{
				userName.Value = userDetails.UserName;
				password.Value = userDetails.Password;
				givenName.Value = userDetails.GivenName;
				familyName.Value = userDetails.FamilyName;
				companyName.Value = userDetails.CompanyName;
				email.Value = userDetails.Email;
				masterTrainer.Value = userDetails.MasterTrainer;
			}
		}

		public void SkipEulaSection ()
		{
			eulaConsentButton_Click(this, EventArgs.Empty);
		}

		public void SkipGdprSection ()
		{
			gdprConsentButton_Click(this, EventArgs.Empty);
		}

		void field_ValueChanged (object sender, EventArgs args)
		{
			UpdateButtons();
		}

		void UpdateButtons ()
		{
			var readyToGo = fields.All(f => ! string.IsNullOrEmpty(f.Value));
			ok.Enabled = readyToGo;
			emergency.Enabled = readyToGo;
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int gap = 20;

			var buttonSize = new Size (250, 40);

			var controlsZone = new Rectangle (0, 0, ClientSize.Width / 2, ClientSize.Height - gap - buttonSize.Height - gap);
			var imageZone = new Rectangle (controlsZone.Right, 0, ClientSize.Width - controlsZone.Right, controlsZone.Height);

			cancel.Bounds = new RectangleFromBounds
			{
				Right = ClientSize.Width - gap,
				Bottom = ClientSize.Height - gap,
				Width = buttonSize.Width,
				Height = buttonSize.Height
			}.ToRectangle();

			ok.Bounds = new RectangleFromBounds
			{
				Left = gap,
				Bottom = ClientSize.Height - gap,
				Width = buttonSize.Width,
				Height = buttonSize.Height
			}.ToRectangle();

			eulaConsentButton.Bounds = ok.Bounds;
			gdprConsentButton.Bounds = ok.Bounds;
			gdprConsentButton.BringToFront();
			eulaConsentButton.BringToFront();

			emergency.Bounds = new RectangleFromBounds
			{
				Left = (ClientSize.Width - buttonSize.Width) / 2,
				Bottom = ClientSize.Height - gap,
				Width = buttonSize.Width,
				Height = buttonSize.Height
			}.ToRectangle();
			emergency.Visible = showEmergencyButton && (createEmergencyGamePanel == null);

			int bottom = controlsZone.Bottom - gap;
			int top = controlsZone.Top;

			int y = top;
			int fieldHeight = 50;
			int leading = (bottom - top - (fields.Count * fieldHeight)) / (fields.Count - 1);
			foreach (var field in fields)
			{
				field.Bounds = new Rectangle (0, y, controlsZone.Width, fieldHeight);
				y = field.Bottom + leading;
			}

			image.Bounds = imageZone;

			eulaBox.Bounds = new Rectangle(0, top, controlsZone.Width, bottom - top);
			gdprBox.Bounds = new Rectangle(0, top, controlsZone.Width, bottom - top);

			if (createEmergencyGamePanel != null)
			{
				createEmergencyGamePanel.Bounds = imageZone;
			}

			if (emergencyCodesPanel != null)
			{
				emergencyCodesPanel.Bounds = new Rectangle (0, 0, ClientSize.Width, ClientSize.Height);
			}
		}

		void UpdateDetails ()
		{
			userDetails = new UserDetails (userName.Value.ToUpper(), password.Value, email.Value, givenName.Value, familyName.Value, masterTrainer.Value, companyName.Value);

			if (createEmergencyGamePanel != null)
			{
				gameDetails = createEmergencyGamePanel.GameDetails;
			}
		}

		public event EventHandler OkClicked;
		public event EventHandler CancelClicked;
		public event EventHandler ResponseCodeEntered;

		void ok_Click (object sender, EventArgs args)
		{
			UpdateDetails();

			if (ValidateUserName())
			{
				choiceButtonClicked = true;
				OnOkClicked();
			}
		}

		void OnOkClicked ()
		{
			OkClicked?.Invoke(this, EventArgs.Empty);
		}

		void OnCancelClicked ()
		{
			CancelClicked?.Invoke(this, EventArgs.Empty);
		}

		void OnResponseCodeEntered ()
		{
			ResponseCodeEntered?.Invoke(this, EventArgs.Empty);
		}

		bool ValidateUserName ()
		{
			if (! productLicensor.UserNamePrefices.Any(p => userDetails.UserName.StartsWith(p)))
			{
				MessageBox.Show(this, "Please check your TAC.", "Error");
				return false;
			}

			return true;
		}

		void cancel_Click (object sender, EventArgs args)
		{
			choiceButtonClicked = true;
			OnCancelClicked();
		}

		void emergency_Click (object sender, EventArgs args)
		{
			isInEmergencyMode = true;

			UpdateDetails();

			createEmergencyGamePanel = new CreateGamePanel(productLicensor, null, userDetails, CreateGamePanelMode.EmergencyActivationAndGameCreation);
			Controls.Add(createEmergencyGamePanel);
			createEmergencyGamePanel.BringToFront();
			DoSize();
		}

		public void ShowEmergencyCodesPanel (string instructions)
		{
			emergencyCodesPanel = new EmergencyActivationCodesPanel (instructions);
			emergencyCodesPanel.ResponseCodeCompleted += EmergencyResponseCodesPanelResponseCodeCompleted;
			emergencyCodesPanel.Cancelled += emergencyCodesPanel_Cancelled;
			Controls.Add(emergencyCodesPanel);
			emergencyCodesPanel.BringToFront();

			createEmergencyGamePanel.Hide();

			DoSize();
		}

		public void HideEmergencyCodesPanel ()
		{
			emergencyCodesPanel.Dispose();
			emergencyCodesPanel = null;

			createEmergencyGamePanel.Show();
		}

		void EmergencyResponseCodesPanelResponseCodeCompleted (object sender, EventArgs args)
		{
			OnResponseCodeEntered();
		}

		public event EventHandler ResponseCodeCancelled;

		void OnResponseCodeCancelled ()
		{
			ResponseCodeCancelled?.Invoke(this, EventArgs.Empty);
		}

		void emergencyCodesPanel_Cancelled (object sender, EventArgs args)
		{
			OnResponseCodeCancelled();
		}

		void eulaConsentButton_Click (object sender, EventArgs args)
		{
			eulaBox.Hide();
			eulaConsentButton.Hide();

			gdprBox.Show();
			gdprConsentButton.Show();
		}

		void gdprConsentButton_Click (object sender, EventArgs args)
		{
			gdprBox.Hide();
			gdprConsentButton.Hide();
		}

		public bool ChoiceMade => choiceButtonClicked;
	}
}