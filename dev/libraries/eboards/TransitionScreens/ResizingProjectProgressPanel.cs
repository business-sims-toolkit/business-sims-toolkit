using System;
using System.Collections.Generic;
using System.Drawing;
using Algorithms;
using LibCore;
using Network;

namespace TransitionScreens
{
	public class ResizingProjectProgressPanel : ProjectProgressPanelBase
	{
		ProgressLozenge productLozenge;
		ProgressLozenge designLozenge;
		ProgressLozenge buildLozenge;
		ProgressLozenge testLozenge;
		ProgressLozenge handoverLozenge;
		ProgressLozenge readyLozenge;
		ProgressLozenge installLozenge;
		ProgressLozenge spendLozenge;
		ProgressLozenge budgetLozenge;
		List<ProgressLozenge> lozenges;

		public ResizingProjectProgressPanel (Node project)
			: base (project)
		{
			productLozenge = new ProgressLozenge ();
			designLozenge = new ProgressLozenge ();
			buildLozenge = new ProgressLozenge ();
			testLozenge = new ProgressLozenge ();
			handoverLozenge = new ProgressLozenge ();
			readyLozenge = new ProgressLozenge ();
			installLozenge = new ProgressLozenge ();
			spendLozenge = new ProgressLozenge ();
			budgetLozenge = new ProgressLozenge ();

			lozenges = new List<ProgressLozenge> (new [] { productLozenge, designLozenge, buildLozenge, testLozenge, handoverLozenge, readyLozenge, installLozenge, spendLozenge, budgetLozenge });

			foreach (var lozenge in lozenges)
			{
				Controls.Add(lozenge);
			}

			OnProjectStatusChanged();
		}

		protected override void OnProjectStatusChanged ()
		{
			base.OnProjectStatusChanged();

			foreach (var lozenge in lozenges)
			{
				lozenge.IsActive = (monitoredProject != null);
			}

			ProgressLozenge currentLozenge = null;
			bool started = true;
			bool completed = false;
			switch (stage)
			{
				case AttrName_Stage_DEFINITION:
					currentLozenge = productLozenge;
					started = false;
					break;

				case AttrName_Stage_DESIGN:
					currentLozenge = designLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Running;
					currentLozenge.SetTimer(wdays);
					break;

				case AttrName_Stage_BUILD:
					currentLozenge = buildLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Running;
					currentLozenge.SetTimer(wdays);
					break;

				case AttrName_Stage_TEST:
					currentLozenge = testLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Running;
					currentLozenge.SetTimer(wdays);
					break;

				case AttrName_Stage_HANDOVER:
					currentLozenge = handoverLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Completed;
					break;

				case AttrName_Stage_READY:
					currentLozenge = readyLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Completed;
					break;

				case AttrName_Stage_INSTALL_OK:
					currentLozenge = installLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Completed;

					completed = true;
					break;

				case AttrName_Stage_INSTALL_FAIL:
					currentLozenge = installLozenge;
					currentLozenge.Status = ProgressLozengeStatus.Failed;
					break;
			}

			if (currentLozenge != null)
			{
				for (int i = 0; i < lozenges.IndexOf(currentLozenge); i++)
				{
					lozenges[i].Status = ProgressLozengeStatus.Completed;

					if (lozenges[i] != productLozenge)
					{
						lozenges[i].SetText();
					}
				}

				for (int i = lozenges.IndexOf(currentLozenge) + 1; i < lozenges.IndexOf(spendLozenge); i++)
				{
					lozenges[i].Status = ProgressLozengeStatus.NotStarted;
				}

				if (completed)
				{
					spendLozenge.Status = ProgressLozengeStatus.Completed;
					budgetLozenge.Status = ProgressLozengeStatus.Completed;
				}
				else if (started)
				{
					spendLozenge.Status = ProgressLozengeStatus.Running;
					budgetLozenge.Status = ProgressLozengeStatus.Running;
				}
				else
				{
					spendLozenge.Status = ProgressLozengeStatus.NotStarted;
					budgetLozenge.Status = ProgressLozengeStatus.NotStarted;
				}
			}

			if (monitoredProject != null)
			{
				spendLozenge.SetText(CONVERT.FormatMoney(CurrentSpend, 0, CONVERT.MoneyFormatting.Thousands));

				productLozenge.Status = ProgressLozengeStatus.Completed;
				productLozenge.SetText(DisplayNameProduct, DisplayNamePlatform);

				if (lozenges.IndexOf(currentLozenge) >= lozenges.IndexOf(handoverLozenge))
				{
					handoverLozenge.SetText(CONVERT.Format("{0}%", Handover));
				}

				if (lozenges.IndexOf(currentLozenge) >= lozenges.IndexOf(designLozenge))
				{
					readyLozenge.SetText(GoLive);
				}

				if (monitoredProject != null)
				{
					budgetLozenge.SetText(CONVERT.FormatMoney(ActualCost, 0, CONVERT.MoneyFormatting.Thousands));
				}
			}
			else
			{
				productLozenge.Status = ProgressLozengeStatus.NotStarted;
			}

			switch (installLozenge.Status)
			{
				case ProgressLozengeStatus.NotStarted:
				case ProgressLozengeStatus.Completed:
					installLozenge.SetText(location.ToUpper());
					break;

				case ProgressLozengeStatus.Failed:
					installLozenge.SetText(reason);
					break;
			}
		}

		public void SetColumnSizes (Interval [] columns)
		{
			for (int i = 0; i < lozenges.Count; i++)
			{
				lozenges[i].Bounds = new Rectangle (columns[i].Min, 0, columns[i].Size, Height);
			}
		}
	}
}