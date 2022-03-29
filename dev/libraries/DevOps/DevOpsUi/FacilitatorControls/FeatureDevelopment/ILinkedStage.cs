using System;
using System.Drawing;

namespace DevOpsUi.FeatureDevelopment
{
	public interface ILinkedStage
	{
		bool HasCompletedStage { get; }

		bool HasFailedStage { get; }

		bool IsStageIncomplete { get; }

		bool IsStageInProgress { get; }
		ILinkedStage PrecedingStage { get; }

		event EventHandler StageStatusChanged;

		ILinkedStage FinalStage { get; }

		int NumberOfRows { get; }

		Rectangle Bounds { get; set; }

		int RowPadding { set; }
		int RowHeight { set; }
	}
}
