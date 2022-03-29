using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CoreUtils;
using LibCore;

namespace Cloud.Application
{
	/// <summary>
	/// Summary description for DebugWindow.
	/// </summary>
	public class DebugWindow : Form
	{
		Button Race1_button;
		Button Race3_button;
		Container components = null;
		GroupBox groupBox5;
		ComboBox comboBox1;
		Button button1;
		Button importIncidentsButton;
		Button quickStartButton;
		Button button4;
		Button Race2_button;
		Button button3;
		Color normColour;
	
		/// <summary>
		/// The Main Window
		/// </summary>
		protected MainGameForm mainForm;

		/// <summary>
		/// Helper panel to play rounds
		/// </summary>
		/// <param name="mf"></param>
		public DebugWindow(MainGameForm mf)
		{
			mainForm = mf;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			//
;                             
			comboBox1.Items.Add("1");
			comboBox1.Items.Add("2");
			comboBox1.Items.Add("4");
			comboBox1.Items.Add("10");
			comboBox1.Items.Add("30");
			comboBox1.Items.Add("60");
			comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

			comboBox1.SelectedIndex = 0;

			Closing += RoundPlayForm_Closing;
		}



		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Race1_button = new System.Windows.Forms.Button();
			Race3_button = new System.Windows.Forms.Button();
			groupBox5 = new System.Windows.Forms.GroupBox();
			comboBox1 = new System.Windows.Forms.ComboBox();
			button1 = new System.Windows.Forms.Button();
			importIncidentsButton = new System.Windows.Forms.Button();
			quickStartButton = new System.Windows.Forms.Button();
			button4 = new System.Windows.Forms.Button();
			Race2_button = new System.Windows.Forms.Button();
			button3 = new System.Windows.Forms.Button();
			groupBox5.SuspendLayout();
			SuspendLayout();
			// 
			// Race1_button
			// 
			Race1_button.Location = new System.Drawing.Point(10, 12);
			Race1_button.Name = "Race1_button";
			Race1_button.Size = new System.Drawing.Size(57, 20);
			Race1_button.TabIndex = 0;
			Race1_button.Text = "Round 1";
			Race1_button.Click += new System.EventHandler(Race1_button_Click);
			// 
			// Race3_button
			// 
			Race3_button.Location = new System.Drawing.Point(139, 12);
			Race3_button.Name = "Race3_button";
			Race3_button.Size = new System.Drawing.Size(60, 20);
			Race3_button.TabIndex = 0;
			Race3_button.Text = "Round 3";
			Race3_button.Click += new System.EventHandler(Race3_button_Click);
			// 
			// groupBox5
			// 
			groupBox5.Controls.Add(comboBox1);
			groupBox5.Location = new System.Drawing.Point(292, 12);
			groupBox5.Name = "groupBox5";
			groupBox5.Size = new System.Drawing.Size(80, 60);
			groupBox5.TabIndex = 6;
			groupBox5.TabStop = false;
			groupBox5.Text = "Speed";
			// 
			// comboBox1
			// 
			comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			comboBox1.Location = new System.Drawing.Point(10, 20);
			comboBox1.Name = "comboBox1";
			comboBox1.Size = new System.Drawing.Size(60, 21);
			comboBox1.TabIndex = 0;
			// 
			// button1
			// 
			button1.Location = new System.Drawing.Point(10, 38);
			button1.Name = "button1";
			button1.Size = new System.Drawing.Size(80, 20);
			button1.TabIndex = 8;
			button1.Text = "Open Model";
			button1.Click += new System.EventHandler(button1_Click);
			// 
			// button2
			// 
			importIncidentsButton.Location = new System.Drawing.Point(10, 90);
			importIncidentsButton.Name = "button2";
			importIncidentsButton.Size = new System.Drawing.Size(100, 20);
			importIncidentsButton.TabIndex = 9;
			importIncidentsButton.Text = "Import Incidents";
			normColour = importIncidentsButton.ForeColor;
			importIncidentsButton.Enabled = true;
			importIncidentsButton.Click += new System.EventHandler(button2_Click);
			// 
			// quickStartButton
			// 
			quickStartButton.Location = new System.Drawing.Point(10, 64);
			quickStartButton.Name = "quickStartButton";
			quickStartButton.Size = new System.Drawing.Size(116, 20);
			quickStartButton.TabIndex = 10;
			quickStartButton.Text = "Quick Start Game";
			quickStartButton.UseVisualStyleBackColor = true;
			quickStartButton.Click += new System.EventHandler(quickStartButton_Click);
			// 
			// button4
			// 
			button4.Location = new System.Drawing.Point(116, 90);
			button4.Name = "button4";
			button4.Size = new System.Drawing.Size(100, 20);
			button4.TabIndex = 12;
			button4.Text = "Export Incidents";
			button4.UseVisualStyleBackColor = true;
			button4.Click += new System.EventHandler(button4_Click);
			// 
			// Race2_button
			// 
			Race2_button.Location = new System.Drawing.Point(73, 12);
			Race2_button.Name = "Race2_button";
			Race2_button.Size = new System.Drawing.Size(60, 20);
			Race2_button.TabIndex = 0;
			Race2_button.Text = "Round 2";
			Race2_button.Click += new System.EventHandler(Race2_button_Click);
			// 
			// button3
			// 
			button3.Location = new System.Drawing.Point(205, 12);
			button3.Name = "button3";
			button3.Size = new System.Drawing.Size(57, 20);
			button3.TabIndex = 13;
			button3.Text = "Round 4";
			button3.Click += new System.EventHandler(button3_Click);
			// 
			// DebugWindow
			// 
			ClientSize = new System.Drawing.Size(383, 121);
			Controls.Add(button3);
			Controls.Add(Race3_button);
			Controls.Add(Race1_button);
			Controls.Add(Race2_button);
			Controls.Add(button4);
			Controls.Add(quickStartButton);
			Controls.Add(importIncidentsButton);
			Controls.Add(button1);
			Controls.Add(groupBox5);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			Name = "DebugWindow";
			Text = "DebugWindow";
			groupBox5.ResumeLayout(false);
			ResumeLayout(false);

		}
		#endregion

		void Race1_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			importIncidentsButton.ForeColor = normColour;
			mainForm.RunRace(1,true);
		}

		void Race2_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(2,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void Race3_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(3,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void Race4_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(4,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void Transition2_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(2,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void Transition3_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(3,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void Transition4_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(4,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			string val = (string)comboBox1.Items[ comboBox1.SelectedIndex ];
			int speed = CONVERT.ParseInt(val);
			TimeManager.TheInstance.FastForward(speed);
		}

		void RoundPlayForm_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
		}

		void Transition5_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(5,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void Race5_button_Click(object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(5,true);
			importIncidentsButton.ForeColor = normColour;
		}

		void button1_Click(object sender, EventArgs e)
		{
			NodeTreeViewer.MainForm bmf = new NodeTreeViewer.MainForm(mainForm.MainPanel.TheGameFile.NetworkModel);
			bmf.Show();
		}

		public void eventMon_NewRoundInstance(object sender, EventArgs e)
		{

		}

		void button2_Click(object sender, EventArgs e)
		{
			importIncidentsButton.ForeColor = Color.Red;
			//mainForm.mainPanel.IncidentsImported = true;
			//int currentRound = mainForm.mainPanel.GetCurrentRound();
			
			OpenFileDialog fdlg = new OpenFileDialog(); 
			fdlg.Title = "Import Incident File" ; 
			fdlg.InitialDirectory = @"c:\" ; 
			fdlg.Filter = "Game Phase Incident File (*.xml)|*.xml";
			fdlg.FilterIndex = 1 ; 
			fdlg.RestoreDirectory = true ;
			if (fdlg.ShowDialog() == DialogResult.OK)
			{
				mainForm.ImportIncidents(fdlg.FileName);
			}
			else
			{
				importIncidentsButton.ForeColor = normColour;
				mainForm.MainPanel.IncidentsImported = false;
			}
		}

		void quickStartButton_Click (object sender, EventArgs e)
		{
			mainForm.QuickStartGame();
		}

		void button4_Click (object sender, EventArgs e)
		{
			mainForm.ExportIncidents();
		}

		void button3_Click (object sender, EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(4, true);
		}

		public bool SetImportIncidentsLabel(bool status)
		{
			if (status)
			{
				importIncidentsButton.ForeColor = Color.Red;
			}
			else
			{
				importIncidentsButton.ForeColor = Color.Black;
			}

			return true;
		}
	}
}
