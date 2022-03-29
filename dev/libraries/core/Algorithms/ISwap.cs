using System.Collections;

namespace Algorithms
{
	public interface ISwap
	{
		void Swap(IList array, int left, int right);
		void Set(IList array, int left, int right);
		void Set(IList array, int left, object obj);
	}
}
