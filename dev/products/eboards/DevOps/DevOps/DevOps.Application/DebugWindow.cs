using System;
using System.ComponentModel;
using System.Windows.Forms;
using CoreUtils;
using LibCore;

namespace DevOps.Application
{
	/// <summary>
	/// Summary description for RoundPlayForm.
	/// </summary>
	public class DebugWindow : Form
	{
		Button Race1_button;
		GroupBox groupBox1;
		GroupBox groupBox2;
		Button Transition2_button;
		Button Race2_button;
		GroupBox groupBox3;
		Button Race3_button;
		Button Transition3_button;
		GroupBox groupBox4;
		Button Transition4_button;
		Button Race4_button;
		Container components = null;
		GroupBox groupBox5;
		ComboBox comboBox1;
		System.Windows.Forms.GroupBox groupBox6;
		System.Windows.Forms.Button Race5_button;
		System.Windows.Forms.Button Transition5_button;
		System.Windows.Forms.Button button1;
		System.Windows.Forms.Button button2;
		Button button3;
		Button button4;

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
			comboBox1.Items.Add("1");
			comboBox1.Items.Add("2");
			comboBox1.Items.Add("4");
			comboBox1.Items.Add("10");
			comboBox1.Items.Add("30");
			comboBox1.Items.Add("60");
			comboBox1.SelectedIndexChanged += new EventHandler(comboBox1_SelectedIndexChanged);

			comboBox1.SelectedIndex = 0;

			this.Closing += new CancelEventHandler(RoundPlayForm_Closing);
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
            this.Race1_button = new System.Windows.Forms.Button();
            this.Transition2_button = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Race2_button = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.Race3_button = new System.Windows.Forms.Button();
            this.Transition3_button = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.Race4_button = new System.Windows.Forms.Button();
            this.Transition4_button = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.Race5_button = new System.Windows.Forms.Button();
            this.Transition5_button = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.SuspendLayout();
            // 
            // Race1_button
            // 
            this.Race1_button.Location = new System.Drawing.Point(10, 20);
            this.Race1_button.Name = "Race1_button";
            this.Race1_button.Size = new System.Drawing.Size(50, 20);
            this.Race1_button.TabIndex = 0;
            this.Race1_button.Text = "Race 1";
            this.Race1_button.Click += new System.EventHandler(this.Race1_button_Click);
            // 
            // Transition2_button
            // 
            this.Transition2_button.Location = new System.Drawing.Point(10, 20);
            this.Transition2_button.Name = "Transition2_button";
            this.Transition2_button.Size = new System.Drawing.Size(70, 20);
            this.Transition2_button.TabIndex = 1;
            this.Transition2_button.Text = "Trans 2";
            this.Transition2_button.Click += new System.EventHandler(this.Transition2_button_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Race1_button);
            this.groupBox1.Location = new System.Drawing.Point(10, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(70, 50);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Round 1";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Race2_button);
            this.groupBox2.Controls.Add(this.Transition2_button);
            this.groupBox2.Location = new System.Drawing.Point(90, 10);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(150, 50);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Round 2";
            // 
            // Race2_button
            // 
            this.Race2_button.Location = new System.Drawing.Point(90, 20);
            this.Race2_button.Name = "Race2_button";
            this.Race2_button.Size = new System.Drawing.Size(50, 20);
            this.Race2_button.TabIndex = 0;
            this.Race2_button.Text = "Race 2";
            this.Race2_button.Click += new System.EventHandler(this.Race2_button_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.Race3_button);
            this.groupBox3.Controls.Add(this.Transition3_button);
            this.groupBox3.Location = new System.Drawing.Point(240, 10);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(150, 50);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Round 3";
            // 
            // Race3_button
            // 
            this.Race3_button.Location = new System.Drawing.Point(90, 20);
            this.Race3_button.Name = "Race3_button";
            this.Race3_button.Size = new System.Drawing.Size(50, 20);
            this.Race3_button.TabIndex = 0;
            this.Race3_button.Text = "Race 3";
            this.Race3_button.Click += new System.EventHandler(this.Race3_button_Click);
            // 
            // Transition3_button
            // 
            this.Transition3_button.Location = new System.Drawing.Point(10, 20);
            this.Transition3_button.Name = "Transition3_button";
            this.Transition3_button.Size = new System.Drawing.Size(70, 20);
            this.Transition3_button.TabIndex = 1;
            this.Transition3_button.Text = "Trans 3";
            this.Transition3_button.Click += new System.EventHandler(this.Transition3_button_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.Race4_button);
            this.groupBox4.Controls.Add(this.Transition4_button);
            this.groupBox4.Location = new System.Drawing.Point(400, 10);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(150, 50);
            this.groupBox4.TabIndex = 5;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Round 4";
            // 
            // Race4_button
            // 
            this.Race4_button.Location = new System.Drawing.Point(90, 20);
            this.Race4_button.Name = "Race4_button";
            this.Race4_button.Size = new System.Drawing.Size(50, 20);
            this.Race4_button.TabIndex = 0;
            this.Race4_button.Text = "Race 4";
            this.Race4_button.Click += new System.EventHandler(this.Race4_button_Click);
            // 
            // Transition4_button
            // 
            this.Transition4_button.Location = new System.Drawing.Point(10, 20);
            this.Transition4_button.Name = "Transition4_button";
            this.Transition4_button.Size = new System.Drawing.Size(70, 20);
            this.Transition4_button.TabIndex = 1;
            this.Transition4_button.Text = "Trans 4";
            this.Transition4_button.Click += new System.EventHandler(this.Transition4_button_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.comboBox1);
            this.groupBox5.Location = new System.Drawing.Point(710, 10);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(80, 60);
            this.groupBox5.TabIndex = 6;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Speed";
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.Location = new System.Drawing.Point(10, 20);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(60, 21);
            this.comboBox1.TabIndex = 0;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.Race5_button);
            this.groupBox6.Controls.Add(this.Transition5_button);
            this.groupBox6.Location = new System.Drawing.Point(560, 10);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(140, 50);
            this.groupBox6.TabIndex = 7;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Round 5";
            // 
            // Race5_button
            // 
            this.Race5_button.Location = new System.Drawing.Point(80, 20);
            this.Race5_button.Name = "Race5_button";
            this.Race5_button.Size = new System.Drawing.Size(50, 20);
            this.Race5_button.TabIndex = 0;
            this.Race5_button.Text = "Race 5";
            this.Race5_button.Click += new System.EventHandler(this.Race5_button_Click);
            // 
            // Transition5_button
            // 
            this.Transition5_button.Location = new System.Drawing.Point(10, 20);
            this.Transition5_button.Name = "Transition5_button";
            this.Transition5_button.Size = new System.Drawing.Size(62, 20);
            this.Transition5_button.TabIndex = 1;
            this.Transition5_button.Text = "Trans 5";
            this.Transition5_button.Click += new System.EventHandler(this.Transition5_button_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(10, 70);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(80, 20);
            this.button1.TabIndex = 8;
            this.button1.Text = "Open Model";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(100, 70);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 20);
            this.button2.TabIndex = 9;
            this.button2.Text = "Import Incidents";
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(661, 70);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(129, 20);
            this.button3.TabIndex = 10;
            this.button3.Text = "Quick Start Game";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(206, 69);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(114, 23);
            this.button4.TabIndex = 11;
            this.button4.Text = "Export Incidents";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // RoundPlayForm
            // 
            this.ClientSize = new System.Drawing.Size(794, 95);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "RoundPlayForm";
            this.Text = "RoundPlayForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		void Race1_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(1,true);
		}

		void Race2_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(2,true);
		}

		void Race3_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(3,true);
		}

		void Race4_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(4,true);
		}

		void Transition2_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(2,true);
		}

		void Transition3_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(3,true);
		}

		void Transition4_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(4,true);
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

		void Transition5_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunTransition(5,true);		
		}

		void Race5_button_Click(object sender, System.EventArgs e)
		{
			TimeManager.TheInstance.Stop();
			mainForm.RunRace(5,true);		
		}

		void button1_Click(object sender, System.EventArgs e)
		{
			NodeTreeViewer.MainForm bmf = new NodeTreeViewer.MainForm(mainForm.MainPanel.TheGameFile.NetworkModel);
			bmf.Show();
		}

		void button2_Click(object sender, System.EventArgs e)
		{
			// Load an incident definition file for this phase...
			OpenFileDialog fdlg = new OpenFileDialog(); 
			fdlg.Title = "Import Incident File" ; 
			fdlg.InitialDirectory = @"c:\" ; 
			fdlg.Filter = "Game Phase Incident File (*.xml)|*.xml";
			fdlg.FilterIndex = 1 ; 
			fdlg.RestoreDirectory = true ; 
			if(fdlg.ShowDialog(TopLevelControl) == DialogResult.OK) 
			{
				mainForm.ImportIncidents(fdlg.FileName);
			} 
		}

		void button3_Click (object sender, EventArgs e)
		{
			mainForm.QuickStartGame();
		}

		void button4_Click(object sender, EventArgs e)
        {
            mainForm.ExportIncidents();
        }
	}
}
