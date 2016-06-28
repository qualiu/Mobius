using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Spark.CSharp.Core;

namespace testKeyValueStream
{
    public interface ISumCount
    {
        SumCount GetSumCount();
    }

    [Serializable]
    public abstract class SumCountBase : ISumCount
    {
        //protected abstract SumCount sumCount { get; set; } = null;
        //protected SumCount sumCount { get { return GetSumCount(); } }

        public abstract SumCount GetSumCount(); // { throw new Exception("should implement."); }

        public void ForeachRDD<V>(double time, RDD<dynamic> rdd)
        {
            var sumCount = GetSumCount();
            sumCount.RddCount += 1;
            var taken = rdd.Collect();
            Console.WriteLine("{0} taken.length = {1} , taken = {2}", TestUtils.NowMilli, taken.Length, taken);

            foreach (object record in taken)
            {
                sumCount.RecordCount += 1;
                KeyValuePair<string, V> kv = (KeyValuePair<string, V>)record;
                Console.WriteLine("{0} record: key = {1}, {2}, temp sumCount = {3}", TestUtils.NowMilli, kv.Key, TestUtils.GetValueText(kv.Value, "value"), sumCount);
                sumCount.LineCount += TestUtils.GetFirstElementValue(kv.Value);
            }

            Log(string.Format("Execute sumCount : {0}", sumCount));
        }

        public void Reduce<V>(double time, RDD<dynamic> rdd)
        {
            var sumCount = GetSumCount();
            sumCount.RddCount += 1;
            var taken = rdd.Collect();
            Console.WriteLine("{0} taken.length = {1} , taken = {2}", TestUtils.NowMilli, taken.Length, taken);

            foreach (object record in taken)
            {
                sumCount.RecordCount += 1;
                KeyValuePair<string, V> kv = (KeyValuePair<string, V>)record;
                Console.WriteLine("{0} record: key = {1}, {2}, temp sumCount = {3}", TestUtils.NowMilli, kv.Key, TestUtils.GetValueText(kv.Value, "value"), sumCount);
                sumCount.LineCount += TestUtils.GetFirstElementValue(kv.Value);
            }

            Log(string.Format("Execute sumCount : {0}", sumCount));
        }

        protected void Log(string message)
        {
            Console.WriteLine("{0} {1} : {2}", TestUtils.NowMilli, this.GetType().Name, message);
        }
    }

    //[Serializable]
    //public class SumCountHelper : SumCountBase
    //{
    //    //private new SumCount sumCount { get; set; }
    //    private new SumCount sumCount { get; set; }

    //    public override SumCount GetSumCount() { return this.sumCount; }

    //    public SumCountHelper(SumCount sum)
    //    {
    //        this.sumCount = sum;
    //    }

    //    public SumCountHelper()
    //    {
    //        this.sumCount = new SumCount();
    //    }
    //}

    [Serializable]
    public class SumCountStaticHelper : SumCountBase
    {
        private static SumCount _SumCount = new SumCount();
        //private new SumCount sumCount { get { return _SumCount; } }
        public SumCountStaticHelper() { }

        public override SumCount GetSumCount() { return _SumCount; }

        public static SumCount GetStaticSumCount() { return _SumCount; }
    }


    [Serializable]
    public class SumCount
    {
        private long _lineCount = 0;
        private long _rddCount = 0;
        private long _recordCount = 0;

        public SumCount(long lineCount = 0, long rddCount = 0, long recordCount = 0)
        {
            _lineCount = lineCount;
            _rddCount = rddCount;
            _recordCount = recordCount;
        }

        public SumCount(SumCount sum) : this(sum.LineCount, sum.RddCount, sum.RecordCount) { }

        public long LineCount
        {
            get { return Interlocked.Read(ref _lineCount); }
            set { Interlocked.Exchange(ref _lineCount, value); }
        }

        public long RddCount
        {
            get { return Interlocked.Read(ref _rddCount); }
            set { Interlocked.Exchange(ref _rddCount, value); }
        }

        public long RecordCount
        {
            get { return Interlocked.Read(ref _recordCount); }
            set { Interlocked.Exchange(ref _recordCount, value); }
        }

        public void Set(long lineCount = 0, long rddCount = 0, long recordCount = 0)
        {
            this.LineCount = lineCount;
            this.RddCount = rddCount;
            this.RecordCount = recordCount;
        }

        public override string ToString()
        {
            return string.Format("LineCount = {0}, RddCount = {1}, RecordCount = {2}", LineCount, RddCount, RecordCount);
        }

        public static SumCount operator -(SumCount s1, SumCount s2) =>
            new SumCount(s1.LineCount - s2.LineCount, s1.RddCount - s2.RddCount, s1.RecordCount - s2.RecordCount);

        public static SumCount operator +(SumCount s1, SumCount s2) =>
            new SumCount(s1.LineCount + s2.LineCount, s1.RddCount + s2.RddCount, s1.RecordCount + s2.RecordCount);

        //public static bool operator ==(SumCount s1, SumCount s2)
        //{
        //    return s1.LineCount == s2.LineCount && s1.RddCount == s2.RddCount && s1.RecordCount == s2.RecordCount;
        //}
        //public static bool operator !=(SumCount s1, SumCount s2)
        //{
        //    return !(s1 == s2);
        //}

        //public override bool Equals(object obj)
        //{
        //    var sum = obj as SumCount;
        //    if ((object)sum == null)
        //    {
        //        return false;
        //    }

        //    return base.Equals(obj) && this == sum;
        //}

        //public override int GetHashCode()
        //{
        //    return base.GetHashCode() ^ (int)(_lineCount + _rddCount + _recordCount);
        //}
    }
}
