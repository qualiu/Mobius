using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testKeyValueStream
{
    [Serializable]
    class ReduceHelper : BaseUtil<ReduceHelper>
    {
        private readonly bool CheckArray;

        public ReduceHelper(bool isCheckingBeforeSum)
        {
            this.CheckArray = isCheckingBeforeSum;
        }

        public int[] Sum(int[] a, int[] b)
        {
            Log(string.Format("SumArray {0} + {1} : checkArray = {2}", TestUtils.ArrayToText("a", a), TestUtils.ArrayToText("b", b), this.CheckArray));

            if (this.CheckArray)
            {
                if (a == null || b == null)
                {
                    return a == null ? b : a;
                }

                if (a.Length == 0 || b.Length == 0)
                {
                    return a.Length == 0 ? b : a;
                }
            }

            var count = this.CheckArray ? Math.Min(a.Length, b.Length) : a.Length;
            var c = new int[count];
            for (var k = 0; k < c.Length; k++)
            {
                c[k] = a[k] + b[k];
            }

            return c;
        }

        public int[] InverseSum(int[] a, int[] b)
        {
            Log(string.Format("InverseSumArray {0} - {1}", TestUtils.ArrayToText("a", a), TestUtils.ArrayToText("b", b)));
            if (this.CheckArray)
            {
                if (a == null || b == null)
                {
                    return a == null ? b : a;
                }

                if (a.Length == 0 || b.Length == 0)
                {
                    return a.Length == 0 ? b : a;
                }
            }

            var count = this.CheckArray ? Math.Min(a.Length, b.Length) : a.Length;
            var c = new int[count];
            for (var k = 0; k < c.Length; k++)
            {
                c[k] = a[k] - b[k];
            }
            return c;
        }

    }
}
