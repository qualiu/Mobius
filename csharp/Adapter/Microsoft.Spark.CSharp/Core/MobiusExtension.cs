using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Spark.CSharp.Core
{
    internal static class MobiusExtension
    {
        public static IntPtr GetAddress(this object obj)
        {
            var handle = GCHandle.Alloc(obj, GCHandleType.WeakTrackResurrection);
            return GCHandle.ToIntPtr(handle);
        }
    }
}
