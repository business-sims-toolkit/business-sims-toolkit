using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using DevOpsEngine.RequestsManagers;
using LibCore;
using Network;
using ResizingUi.Button;
using ResizingUi.Interfaces;

namespace DevOpsUi.FacilitatorControls.FeatureDevelopment.Agile
{
	public class UpgradesPanel : FlickerFreePanel
	{
		public UpgradesPanel(AgileRequestsManager requestsManager, string bizServiceName, IDialogOpener dialogController, string preselectedFeatureId = null)
		{
			this.dialogController = dialogController;

			okButton = new StyledDynamicButton("standard", "OK", true)
			{
				Size = new Size(80, 30),
				Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font"),
				Enabled = false,
				Visible = false
			};
			Controls.Add(okButton);
			okButton.Click += okButton_Click;

			cancelButton = new StyledImageButton("progression_panel", 0, false)
			{
				BackColor = Color.Transparent,
				UseCircularBackground = true,
				Margin = new Padding(4),
				Size = new Size(20, 20)
			};
			cancelButton.SetVariants(@"\images\buttons\cross.png");
			Controls.Add(cancelButton);
			cancelButton.Click += cancelButton_Click;

			this.requestsManager = requestsManager;

			this.bizServiceName = bizServiceName;
			this.preselectedFeatureId = preselectedFeatureId;

			isRedevelopment = !string.IsNullOrEmpty(preselectedFeatureId);

			ShowFeaturesPanel();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{

			var bounds = new Rectangle(0, 0, Width, Height);

			const int minButtonHeight = 20;
			const int maxButtonHeight = 40;

			const int minButtonWidth = 40;
			const int maxButtonWidth = 60;


			const int minPadding = 5;
			const int maxPadding = 10;

			var padding = Maths.Clamp((int)(Width * 0.1f), minPadding, maxPadding);

			var buttonWidth = Maths.Clamp((int)(Width * 0.3f), minButtonWidth, maxButtonWidth);

			var buttonHeight = Maths.Clamp((int)(Height * 0.1f), minButtonHeight, maxButtonHeight);

			cancelButton.Bounds = bounds.AlignRectangle(cancelButton.Size, StringAlignment.Far, StringAlignment.Near, -padding, padding);

			okButton.Bounds = new RectangleFromBounds
			{
				Right = cancelButton.Left - padding,
				Top = cancelButton.Top,
				Width = buttonWidth,
				Height = buttonHeight
			}.ToRectangle();

			featuresPanel.Bounds = new Rectangle(padding, padding, Width - 2 * padding, (Height - 3 * padding) / 4);

			if (productsPanel != null)
			{
				productsPanel.Bounds = new RectangleFromBounds
				{
					Left = padding,
					Right = Width - padding,
					Top = featuresPanel.Bottom + padding,
					Bottom = Bottom - padding
				}.ToRectangle();
			}

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (productsPanel?.Visible ?? false)
			{
				var lineY = featuresPanel.Bottom + (productsPanel.Top - featuresPanel.Bottom) / 2f;

				using (var pen = new Pen(CONVERT.ParseHtmlColor("#c1c1c1"), 1))
				{
					e.Graphics.DrawLine(pen, 20, lineY, Width - 20, lineY);
				}
			}
		}

		void ShowFeaturesPanel()
		{
			RemoveFeaturesPanel();

			List<StyledDynamicButton> CreateFeatureButtons()
			{
				var featureAvailabilitiesForService = requestsManager.GetFeaturesByServiceName(bizServiceName);

				if (preselectedFeatureId != null)
				{
					featureAvailabilitiesForService = featureAvailabilitiesForService
						.Where(f => f.FeatureId == preselectedFeatureId).ToList();
					selectedFeatureId = preselectedFeatureId;
				}

				return featureAvailabilitiesForService.Select(f =>
					FeatureSelectionButtonFactory.CreateButton(GetFeatureButtonLegend(f.FeatureNode), f.FeatureId, false,
						f.FeatureId == selectedFeatureId, f.IsAvailable, 40, 30, null, "standard", true)).ToList();
			}

			if (featuresPanel == null)
			{
				featuresPanel = new FeatureSelectionButtonsPanel(CreateFeatureButtons, new Size(30, 20), 60, 30, "Features", false)
				{
					MaximumColumns = 3
				};

				featuresPanel.ButtonClicked += featuresPanel_ButtonClicked;

				Controls.Add(featuresPanel);
			}

			featuresPanel.UpdateButtons();
			featuresPanel.PreSelectedId = preselectedFeatureId;
			featuresPanel.Enabled = string.IsNullOrEmpty(preselectedFeatureId);

			DoSize();
		}

		static string GetProductButtonLegend(Node product)
		{
			var isPrototype = product.GetBooleanAttribute("is_prototype", false);
			var platform = product.GetAttribute("platform");

			return (isPrototype ? "MVP" : platform);
		}

		void ShowProductsPanel()
		{
			RemoveProductsPanel();

			List<StyledDynamicButton> CreateProductButtons()
			{
				var featureProducts = requestsManager.GetProductsForFeature(selectedFeatureId);

				return featureProducts.Select(
					p => FeatureSelectionButtonFactory.CreateButton(GetProductButtonLegend(p.ProductNode), p.ProductId,
						p.IsOptimal, p.ProductId == selectedProductId, true, 60, 30,
						!requestsManager.IsPlatformSupportedForProduct(p.ProductId) ? Color.Red : (Color?)null,
					"standard", true)).ToList();
			}

			productsPanel = new FeatureSelectionButtonsPanel(CreateProductButtons, new Size(40, 30), 65, 40, "Products", false)
				{
					MaximumColumns = 3
				};

			productsPanel.ButtonClicked += productsPanel_ButtonClicked;
			productsPanel.ButtonDoubleClicked += productsPanel_ButtonDoubleClicked;

			Controls.Add(productsPanel);

			DoSize();

		}

		static string GetFeatureButtonLegend(Node feature)
		{
			var bestService = feature.GetChildrenAsList().Where(p => !p.GetBooleanAttribute("is_prototype", false)).Aggregate((bestSoFar, ns) =>
			{
				var bestSoFarPerformance = (bestSoFar == null) ? null : (int?)AgileRequestsManager.GetProductPerformance(bestSoFar);
				var thisServicePerformance = AgileRequestsManager.GetProductPerformance(ns);
				return (bestSoFarPerformance.HasValue && (bestSoFarPerformance.Value < thisServicePerformance)) ? ns : bestSoFar;
			}
			);

			var performance = AgileRequestsManager.GetProductPerformance(bestService);

			return feature.GetAttribute("service_id") + ((performance > 0) ? " +" : ((performance < 0) ? " -" : " ="));
		}

		void SetOkButtonEnabled()
		{
			if (!string.IsNullOrEmpty(selectedFeatureId) && !string.IsNullOrEmpty(selectedProductId))
			{
				okButton.Enabled = isRedevelopment || !requestsManager.HasNewServiceDevelopmentAlreadyStarted(selectedFeatureId);
			}
		}

		void RemoveFeaturesPanel()
		{
			RemoveProductsPanel();
		}

		void RemoveProductsPanel()
		{
			productsPanel?.Dispose();
			productsPanel = null;
		}

		void ClosePanel()
		{
			dialogController.CloseDialog();
		}

		void featuresPanel_ButtonClicked(object sender, EventArgs e)
		{
			selectedFeatureId = featuresPanel.SelectedId;
			selectedProductId = "";

			SetOkButtonEnabled();

			ShowProductsPanel();
		}

		void productsPanel_ButtonClicked(object sender, EventArgs e)
		{
			selectedProductId = productsPanel.SelectedId;

			okButton.PressButton();
		}

		void productsPanel_ButtonDoubleClicked(object sender, EventArgs e)
		{
			selectedProductId = productsPanel.SelectedId;

			okButton.PressButton();
		}

		void okButton_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(selectedFeatureId) && !string.IsNullOrEmpty(selectedProductId))
			{
				if (isRedevelopment || !requestsManager.HasNewServiceDevelopmentAlreadyStarted(selectedFeatureId))
				{
					requestsManager.StartServiceDevelopment(selectedFeatureId, selectedProductId);
				}
			}
			else
			{
				okButton.Enabled = false;
			}

			ClosePanel();
		}

		void cancelButton_Click(object sender, EventArgs e)
		{
			ClosePanel();
		}

		string selectedFeatureId;
		string selectedProductId;

		readonly string preselectedFeatureId;


		readonly IDialogOpener dialogController;

		FeatureSelectionButtonsPanel featuresPanel;
		FeatureSelectionButtonsPanel productsPanel;

		readonly StyledDynamicButton okButton;
		readonly StyledImageButton cancelButton;

		readonly AgileRequestsManager requestsManager;
		readonly string bizServiceName;

		readonly bool isRedevelopment;
	}
}
