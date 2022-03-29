using System;
using System.Collections.Generic;
using Events;
using ReportBuilder;

namespace DevOps.ReportsScreen.Interfaces
{
    public interface IRoundScoresUpdater<T> where T : RoundScores
    {
        event EventHandler<EventArgs<List<T>>> RoundScoresChanged;
    }
}
