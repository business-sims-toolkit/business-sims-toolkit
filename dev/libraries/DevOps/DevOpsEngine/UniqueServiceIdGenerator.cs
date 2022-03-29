using System;

namespace DevOpsEngine
{
	public class UniqueServiceIdGenerator
	{
		public int GetNextId()
		{
			return idCounter++;
		}

		int idCounter = 0;

		public void UpdateId (int id)
		{
			idCounter = Math.Max(idCounter, id + 1);
		}
	}
}