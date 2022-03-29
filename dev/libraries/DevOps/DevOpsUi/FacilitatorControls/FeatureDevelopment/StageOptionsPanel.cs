using System;
using System.Collections;
using System.Collections.Generic;

using CommonGUI;
using Network;

namespace DevOpsUi.FeatureDevelopment
{
	public abstract class StageOptionsPanel : FlickerFreePanel
	{
		protected StageOptionsPanel(StageGroupProperties properties, Func<bool> hasPassedStage, Node serviceNode, Node commandQueueNode)
		{
			getCorrectOption = properties.GetCorrectOption;
			getCurrentSelection = properties.GetCurrentSelection;
			getOptions = properties.GetOptions;
			commandTypes = properties.CommandTypes;

			this.hasPassedStage = hasPassedStage;
			this.serviceNode = serviceNode;

			serviceNode.AttributesChanged += serviceNode_AttributesChanged;

			this.commandQueueNode = commandQueueNode;
		}

		public string SelectedOption
		{
			get => selectedOption;

			set
			{
				selectedOption = value;

				if (!string.IsNullOrEmpty(selectedOption))
				{
					AddSelectionCommand();
					OnOptionSelected();
				}
			}
		}

		public abstract void UpdateOptions();
		public abstract void SelectCorrectOption();

		public event EventHandler OptionSelected;

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		protected abstract void DoSize();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				serviceNode.AttributesChanged -= serviceNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		void OnOptionSelected()
		{
			OptionSelected?.Invoke(this, EventArgs.Empty);
		}

		public void AddSelectionCommand()
		{
			foreach (var command in commandTypes)
			{
				commandQueueNode.CreateChild(command, "", new List<AttributeValuePair>
				{
					new AttributeValuePair("selection", SelectedOption),
					new AttributeValuePair("service_name", serviceNode.GetAttribute("name"))
				});
			}

			UpdateOptions();
		}

		void serviceNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateOptions();
		}

		readonly Node commandQueueNode;
		protected readonly Node serviceNode;
		readonly List<string> commandTypes;

		protected readonly Func<Node, string> getCorrectOption;
		protected readonly Func<Node, string> getCurrentSelection;
		protected readonly Func<List<ButtonTextTags>> getOptions;
		protected readonly Func<bool> hasPassedStage;

		protected string selectedOption;
	}
}
