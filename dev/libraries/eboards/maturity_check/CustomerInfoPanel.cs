using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Runtime.InteropServices;

namespace maturity_check
{
	public interface ICustomerInfo
	{
		void Cancelled();
		void Accepted();
		void SaveCustomerDetails ();
	};
	/// <summary>
	/// Summary description for CustomerInfoPanel.
	/// </summary>
	public class CustomerInfoPanel : Panel
	{
		[DllImport("user32")]
		static extern bool HideCaret (IntPtr hWnd);
		[DllImport("user32")]
		static extern bool ShowCaret (IntPtr hWnd);

		System.Windows.Forms.GroupBox groupBox1;
		System.Windows.Forms.TextBox customerBox;
		System.Windows.Forms.GroupBox groupBox2;
		System.Windows.Forms.TextBox addressBox;
		System.Windows.Forms.GroupBox groupBox3;
		System.Windows.Forms.ComboBox regionBox;
		System.Windows.Forms.ComboBox countryBox;
		TextBox regionTextBox;
		TextBox countryTextBox;

		System.Windows.Forms.Label label1;
		System.Windows.Forms.Label label2;
		System.Windows.Forms.GroupBox groupBox4;
		System.Windows.Forms.TextBox emailBox;
		System.Windows.Forms.GroupBox groupBox5;
		System.Windows.Forms.TextBox email2Box;
		System.Windows.Forms.GroupBox groupBox6;
		System.Windows.Forms.Label label3;
		System.Windows.Forms.DateTimePicker datePicker;
		TextBox dateTextBox;
		System.Windows.Forms.GroupBox groupBox7;
		System.Windows.Forms.TextBox phoneBox;
		System.Windows.Forms.Label label4;
		System.Windows.Forms.TextBox purposeBox;
		System.Windows.Forms.Label label5;
		System.Windows.Forms.TextBox notesBox;

		Button ok;
		Button cancel;

		bool saveChangesImmediately;

		Hashtable timerToFlashData;

		string dateFormat = "yyyy_MM_dd_HH_mm_ss";

		class FlashData
		{
			public Control Control;
			public Color BackColor;
			public Color FlashColor;
			public int FlashesLeft;

			public FlashData (Control control, Color flashColor)
			{
				Control = control;
				BackColor = control.BackColor;
				FlashColor = flashColor;
				FlashesLeft = 0;
			}
		}

		protected ICustomerInfo _ci;

		public CustomerInfoPanel(ICustomerInfo ci)
		{
			_ci = ci;
			timerToFlashData = new Hashtable ();

			InitializeComponent();
			LoadRegions();
			this.Size = new System.Drawing.Size(600,550);
		}

		public void HideButtons()
		{
			ok.Visible = false;
			cancel.Visible = false;
		}

		void InitializeComponent()
		{
			this.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("maturity_editor_background_colour");

			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.customerBox = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.addressBox = new System.Windows.Forms.TextBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.regionBox = new System.Windows.Forms.ComboBox();
			this.countryBox = new System.Windows.Forms.ComboBox();
			regionTextBox = new TextBox ();
			countryTextBox = new TextBox ();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.emailBox = new System.Windows.Forms.TextBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.email2Box = new System.Windows.Forms.TextBox();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.datePicker = new System.Windows.Forms.DateTimePicker();
			dateTextBox = new TextBox();
			this.groupBox7 = new System.Windows.Forms.GroupBox();
			this.phoneBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.purposeBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.notesBox = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.groupBox7.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.customerBox);
			this.groupBox1.Location = new System.Drawing.Point(10, 10);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(250, 50);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Customer (Company)";
			// 
			// customerBox
			// 
			this.customerBox.Location = new System.Drawing.Point(10, 20);
			this.customerBox.Name = "customerBox";
			this.customerBox.Size = new System.Drawing.Size(230, 20);
			this.customerBox.TabIndex = 0;
			this.customerBox.Text = "";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.addressBox);
			this.groupBox2.Location = new System.Drawing.Point(10, 180);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(250, 120);
			this.groupBox2.TabIndex = 2;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Customer Address";
			// 
			// addressBox
			// 
			this.addressBox.Location = new System.Drawing.Point(10, 20);
			this.addressBox.Multiline = true;
			this.addressBox.Name = "addressBox";
			this.addressBox.Size = new System.Drawing.Size(230, 90);
			this.addressBox.TabIndex = 0;
			this.addressBox.Text = "";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label2);
			this.groupBox3.Controls.Add(this.label1);
			this.groupBox3.Controls.Add(this.countryBox);
			this.groupBox3.Controls.Add(this.regionBox);
			groupBox3.Controls.Add(countryTextBox);
			groupBox3.Controls.Add(regionTextBox);
			this.groupBox3.Location = new System.Drawing.Point(10, 70);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(250, 100);
			this.groupBox3.TabIndex = 3;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Customer Region/Country";
			// 
			// regionBox
			// 
			this.regionBox.Location = new System.Drawing.Point(90, 20);
			this.regionBox.Name = "regionBox";
			this.regionBox.Size = new System.Drawing.Size(150, 21);
			this.regionBox.DropDownStyle = ComboBoxStyle.DropDownList;
			this.regionBox.TabIndex = 0;
			this.regionBox.Text = "";
			regionTextBox.Bounds = regionBox.Bounds;
			// 
			// countryBox
			// 
			this.countryBox.Location = new System.Drawing.Point(90, 60);
			this.countryBox.Name = "countryBox";
			this.countryBox.Size = new System.Drawing.Size(150, 21);
			this.countryBox.DropDownStyle = ComboBoxStyle.DropDownList;
			this.countryBox.TabIndex = 1;
			this.countryBox.Text = "";
			countryTextBox.Bounds = countryBox.Bounds;
			// 
			// label1
			// 
			this.label1.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label1.Location = new System.Drawing.Point(10, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(70, 20);
			this.label1.TabIndex = 2;
			this.label1.Text = "Region";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(10, 60);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(70, 20);
			this.label2.TabIndex = 3;
			this.label2.Text = "Country";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.emailBox);
			this.groupBox4.Location = new System.Drawing.Point(10, 310);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(250, 50);
			this.groupBox4.TabIndex = 4;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Customer Email";
			// 
			// emailBox
			// 
			this.emailBox.Location = new System.Drawing.Point(10, 20);
			this.emailBox.Name = "emailBox";
			this.emailBox.Size = new System.Drawing.Size(230, 20);
			this.emailBox.TabIndex = 0;
			this.emailBox.Text = "";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.email2Box);
			this.groupBox5.Location = new System.Drawing.Point(10, 370);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(250, 50);
			this.groupBox5.TabIndex = 5;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Customer Email 2";
			// 
			// email2Box
			// 
			this.email2Box.Location = new System.Drawing.Point(10, 20);
			this.email2Box.Name = "email2Box";
			this.email2Box.Size = new System.Drawing.Size(230, 20);
			this.email2Box.TabIndex = 0;
			this.email2Box.Text = "";
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.notesBox);
			this.groupBox6.Controls.Add(this.label5);
			this.groupBox6.Controls.Add(this.purposeBox);
			this.groupBox6.Controls.Add(this.label4);
			this.groupBox6.Controls.Add(this.datePicker);
			this.groupBox6.Controls.Add(this.dateTextBox);
			this.groupBox6.Controls.Add(this.label3);
			this.groupBox6.Location = new System.Drawing.Point(270, 10);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(310, 470);
			this.groupBox6.TabIndex = 6;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Assessment Information";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(100, 30);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(200, 20);
			this.label3.TabIndex = 0;
			this.label3.Text = "Date of Assessment";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// datePicker
			// 
			this.datePicker.Location = new System.Drawing.Point(100, 60);
			this.datePicker.Name = "datePicker";
			this.datePicker.TabIndex = 1;
			this.dateTextBox.Bounds = this.datePicker.Bounds;
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.phoneBox);
			this.groupBox7.Location = new System.Drawing.Point(10, 430);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(250, 50);
			this.groupBox7.TabIndex = 7;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Customer Phone Number";
			// 
			// phoneBox
			// 
			this.phoneBox.Location = new System.Drawing.Point(10, 20);
			this.phoneBox.Name = "phoneBox";
			this.phoneBox.Size = new System.Drawing.Size(230, 20);
			this.phoneBox.TabIndex = 0;
			this.phoneBox.Text = "";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(100, 100);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(200, 20);
			this.label4.TabIndex = 2;
			this.label4.Text = "Purpose of Assessment";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// purposeBox
			// 
			this.purposeBox.Location = new System.Drawing.Point(10, 130);
			this.purposeBox.Multiline = true;
			this.purposeBox.Name = "purposeBox";
			this.purposeBox.Size = new System.Drawing.Size(290, 100);
			this.purposeBox.TabIndex = 3;
			this.purposeBox.Text = "";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(100, 240);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(200, 20);
			this.label5.TabIndex = 4;
			this.label5.Text = "Notes";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// notesBox
			// 
			this.notesBox.Location = new System.Drawing.Point(10, 270);
			this.notesBox.Multiline = true;
			this.notesBox.Name = "notesBox";
			this.notesBox.Size = new System.Drawing.Size(290, 190);
			this.notesBox.TabIndex = 5;
			this.notesBox.Text = "";
			//
			ok = new Button();
			ok.Text = "OK";
			ok.Size = new System.Drawing.Size(100,20);
			ok.Location = new System.Drawing.Point(15,510);
			ok.BackColor = Color.LightGray;
			ok.Click += ok_Click;
			this.Controls.Add(ok);
			//
			cancel = new Button();
			cancel.Text = "Cancel";
			cancel.Size = new System.Drawing.Size(100,20);
			cancel.Location = new System.Drawing.Point(465,510);
			cancel.BackColor = Color.LightGray;
			cancel.Click += cancel_Click;
			this.Controls.Add(cancel);
			// 
			// Form1
			// 
			this.ClientSize = new System.Drawing.Size(612, 521);
			this.Controls.Add(this.groupBox7);
			this.Controls.Add(this.groupBox6);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox4.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox6.ResumeLayout(false);
			this.groupBox7.ResumeLayout(false);
			this.ResumeLayout(false);

			customerBox.GotFocus += textBox_GotFocus;
			addressBox.GotFocus += textBox_GotFocus;
			countryTextBox.GotFocus += textBox_GotFocus;
			regionTextBox.GotFocus += textBox_GotFocus;
			emailBox.GotFocus += textBox_GotFocus;
			email2Box.GotFocus += textBox_GotFocus;
			phoneBox.GotFocus += textBox_GotFocus;
			purposeBox.GotFocus += textBox_GotFocus;
			notesBox.GotFocus += textBox_GotFocus;
			dateTextBox.GotFocus += textBox_GotFocus;

			// Because the notes and purpose boxes remain editable at all times,
			// we might need to save whenever they're changed.
			notesBox.TextChanged += notesBox_TextChanged;
			purposeBox.TextChanged += purposeBox_TextChanged;
		}

		void SaveChangesIfAppropriate ()
		{
			// If we're not generally editable, then we won't get an OK click
			// when the user is done -- so save the changes now.
			if (saveChangesImmediately)
			{
				_ci.SaveCustomerDetails();
			}
		}

		void purposeBox_TextChanged (object sender, EventArgs e)
		{
			SaveChangesIfAppropriate();
		}

		void notesBox_TextChanged (object sender, EventArgs e)
		{
			SaveChangesIfAppropriate();
		}

		void textBox_GotFocus (object sender, EventArgs e)
		{
			TextBox textBox = sender as TextBox;

			if (textBox.ReadOnly)
			{
				HideCaret(textBox.Handle);
			}
			else
			{
				ShowCaret(textBox.Handle);
			}
		}

		void ok_Click(object sender, EventArgs e)
		{
			if (ValidateInput())
			{
				_ci.Accepted();
			}
		}

		void cancel_Click(object sender, EventArgs e)
		{
			_ci.Cancelled();
		}

		Timer GetTimerByControl (Control control)
		{
			foreach (Timer timer in timerToFlashData.Keys)
			{
				if (((FlashData) timerToFlashData[timer]).Control == control)
				{
					return timer;
				}
			}

			return null;
		}

		void FlashControl (Control control)
		{
			Timer timer = GetTimerByControl(control);

			if (timer == null)
			{
				timer = new Timer ();
				timer.Interval = 250;
				timer.Tick += timer_Tick;
				timerToFlashData.Add(timer, new FlashData (control, Color.Red));
			}

			(timerToFlashData[timer] as FlashData).FlashesLeft = 4;

			timer.Start();
		}

		void timer_Tick (object sender, EventArgs e)
		{
			Timer timer = sender as Timer;
			FlashData flashData = timerToFlashData[timer] as FlashData;

			if (flashData.Control.BackColor == flashData.FlashColor)
			{
				flashData.Control.BackColor = flashData.BackColor;
			}
			else
			{
				flashData.Control.BackColor = flashData.FlashColor;
			}

			flashData.FlashesLeft--;

			if (flashData.FlashesLeft <= 0)
			{
				timer.Stop();
				timerToFlashData.Remove(timer);
			}
		}

		void CheckNonEmpty (Control control, string errorText, ref string error)
		{
			if (control.Text.Trim() == "")
			{
				FlashControl(control);

				if (error != "")
				{
					error += "\n";
				}
				error += errorText;
			}
		}
		
		bool ValidateInput ()
		{
			string error = "";

			CheckNonEmpty(customerBox, "Please specify a customer name.", ref error);
			CheckNonEmpty(regionBox, "Please specify a customer region.", ref error);
			CheckNonEmpty(countryBox, "Please specify a customer country.", ref error);
			CheckNonEmpty(addressBox, "Please specify a customer address.", ref error);
			CheckNonEmpty(emailBox, "Please specify a customer email address.", ref error);
			CheckNonEmpty(phoneBox, "Please specify a customer phone number.", ref error);
			CheckNonEmpty(purposeBox, "Please specify the purpose of the assessment.", ref error);

			if (error != "")
			{
				MessageBox.Show(this, error, "Incomplete information");
				return false;
			}

			return true;
		}

		void LoadRegions ()
		{
			using (StreamReader stream = new StreamReader (LibCore.AppInfo.TheInstance.Location + "data/countries.xml"))
			{
				LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create(stream.ReadToEnd());

				XmlNode root = doc.DocumentElement;
				foreach (XmlNode region in root.ChildNodes)
				{
					if (region.Name == "region")
					{
						string regionName = LibCore.BasicXmlDocument.GetStringAttribute(region, "name");
						if (! regionBox.Items.Contains(regionName))
						{
							regionBox.Items.Add(regionName);
						}

						foreach (XmlNode country in region.ChildNodes)
						{
							if (country.Name == "country")
							{
								string countryName = LibCore.BasicXmlDocument.GetStringAttribute(country, "name");
								if (! countryBox.Items.Contains(countryName))
								{
									countryBox.Items.Add(countryName);
								}
							}
						}
					}
				}

				ArrayList sortedRegions = new ArrayList (regionBox.Items);
				sortedRegions.Sort();
				regionBox.Items.Clear();
				regionBox.Items.AddRange(sortedRegions.ToArray());

				ArrayList sortedCountries = new ArrayList (countryBox.Items);
				sortedCountries.Sort();
				countryBox.Items.Clear();
				countryBox.Items.AddRange(sortedCountries.ToArray());
			}
		}

		public void SetEditable (bool editable)
		{
			if (editable)
			{
				customerBox.ReadOnly = false;
				addressBox.ReadOnly = false;
				emailBox.ReadOnly = false;
				email2Box.ReadOnly = false;
				phoneBox.ReadOnly = false;
				purposeBox.ReadOnly = false;
				notesBox.ReadOnly = false;

				datePicker.Show();
				dateTextBox.Hide();

				countryBox.Show();
				countryTextBox.Hide();

				regionBox.Show();
				regionTextBox.Hide();

				ok.Show();
				cancel.Show();
			}
			else
			{
				customerBox.ReadOnly = true;
				addressBox.ReadOnly = true;
				emailBox.ReadOnly = true;
				email2Box.ReadOnly = true;
				phoneBox.ReadOnly = true;

				purposeBox.ReadOnly = false;
				notesBox.ReadOnly = false;

				datePicker.Hide();
				dateTextBox.Show();
				dateTextBox.ReadOnly = true;

				countryBox.Hide();
				countryTextBox.Show();
				countryTextBox.ReadOnly = true;

				regionBox.Hide();
				regionTextBox.Show();
				regionTextBox.ReadOnly = true;

				ok.Hide();
				cancel.Hide();
			}

			saveChangesImmediately = ! editable;
		}

		void AppendElement (XmlElement parent, string nodeName, string nodeContent)
		{
			XmlElement element = parent.OwnerDocument.CreateElement(nodeName);
			element.InnerText = nodeContent;
			parent.AppendChild(element);
		}

		public void SaveToXml (string filename)
		{
			LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create ();

			XmlElement root = doc.CreateElement("customer_details");
			doc.AppendChild(root);

			AppendElement(root, "customer_name", customerBox.Text);
			AppendElement(root, "address", addressBox.Text);
			AppendElement(root, "region", regionBox.Text);
			AppendElement(root, "country", countryBox.Text);
			AppendElement(root, "email_1", emailBox.Text);
			AppendElement(root, "email_2", email2Box.Text);
			AppendElement(root, "date", datePicker.Value.ToString(dateFormat));
			AppendElement(root, "phone", phoneBox.Text);
			AppendElement(root, "purpose", purposeBox.Text);
			AppendElement(root, "notes", notesBox.Text);

			doc.Save(filename);
		}

		public void LoadXml (string filename)
		{
			saveChangesImmediately = false;
			if (File.Exists(filename))
			{
				using (StreamReader reader = new StreamReader(filename))
				{
					LibCore.BasicXmlDocument doc = LibCore.BasicXmlDocument.Create(reader.ReadToEnd());

					customerBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "customer_name").InnerText;
					addressBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "address").InnerText;

					regionTextBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "region").InnerText;
					regionBox.Text = regionTextBox.Text;
					countryTextBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "country").InnerText;
					countryBox.Text = countryTextBox.Text;

					emailBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "email_1").InnerText;
					email2Box.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "email_2").InnerText;

					datePicker.Value = DateTime.ParseExact(LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "date").InnerText, dateFormat, CultureInfo.InvariantCulture);
					dateTextBox.Text = datePicker.Value.ToString("dd MMMM yyyy").Replace(" ", "   ");

					phoneBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "phone").InnerText;
					purposeBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "purpose").InnerText;
					notesBox.Text = LibCore.BasicXmlDocument.GetNamedChild(doc.DocumentElement, "notes").InnerText;
				}
			}
		}

		public string ExportCSV ()
		{
			string csv = "";

			csv += MaturityCard.EscapeToCSVFormat(customerBox.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(datePicker.Value.ToString("dd MMMM yyyy")) + ",";
			csv += MaturityCard.EscapeToCSVFormat(regionTextBox.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(countryTextBox.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(emailBox.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(email2Box.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(phoneBox.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(purposeBox.Text) + ",";
			csv += MaturityCard.EscapeToCSVFormat(addressBox.Text);

			csv += "\n";

			return csv;
		}

		public string CustomerText
		{
			get
			{
				return customerBox.Text;
			}
		}

		public string DateText
		{
			get
			{
				return datePicker.Value.ToString("dd MMMM yyyy");
			}
		}

		public string CountryText
		{
			get
			{
				return countryBox.Text;
			}
		}

		public string RegionText
		{
			get
			{
				return regionBox.Text;
			}
		}

		public string AddressText
		{
			get
			{
				return addressBox.Text;
			}
		}

		public string EmailText
		{
			get
			{
				return emailBox.Text;
			}
		}

		public string Email2Text
		{
			get
			{
				return email2Box.Text;
			}
		}

		public string PhoneText
		{
			get
			{
				return phoneBox.Text;
			}
		}

		public string PurposeText
		{
			get
			{
				return purposeBox.Text;
			}
		}
	}
}