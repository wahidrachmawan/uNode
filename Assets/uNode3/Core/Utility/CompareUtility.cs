using System.Collections.Generic;


namespace MaxyGames.UNode {
	public static class CompareUtility {
		public static IEqualityComparer<T> ReferenceComparer<T>() where T : class {
			return OdinSerializer.Utilities.ReferenceEqualityComparer<T>.Default;
		}

		public static int Compare(int intA, int intB) {
			if (intA < intB) {
				return -1;
			}
			if (intB < intA) {
				return 1;
			}
			return 0;
		}

		public static int Compare(string strA, int intA, string strB, int intB) {
			if (intA == intB) {
				return string.Compare(strA, strB, System.StringComparison.Ordinal);
			}
			if (intA < intB) {
				return -1;
			}
			if (intB < intA) {
				return 1;
			}
			return string.Compare(strA, strB, System.StringComparison.Ordinal);
		}

		public static int Compare(string strA1, string strB1, string strA2, string strB2) {
			int val = string.CompareOrdinal(strA1, strB1);
			if(val == 0) {
				return string.Compare(strA2, strB2, System.StringComparison.Ordinal);
			}
			return val;
		}
	}
}