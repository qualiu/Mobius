using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Streaming;

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
        protected SumCount sumCount { get { return GetSumCount(); } }

        public abstract SumCount GetSumCount(); // { throw new Exception("should implement."); }

        public void ForeachRDD<V>(double time, RDD<dynamic> rdd)
        {
            sumCount.RddCount += 1;
            var taken = rdd.Collect();
            Console.WriteLine("{0} taken.length = {1} , taken = {2}", TestUtils.NowMilli, taken.Length, taken);

            foreach (object record in taken)
            {
                sumCount.RecordCount += 1;
                KeyValuePair<string, V> kv = (KeyValuePair<string, V>)record;
                Console.WriteLine("{0} record: key = {2}, value = {3}, temp sumCount = {4}", TestUtils.NowMilli, record, kv.Key, TestUtils.GetValueText(kv.Value), sumCount);
                sumCount.LineCount += TestUtils.GetFirstElementValue(kv.Value);
            }

            Log(string.Format("Execute sumCount : {0}", sumCount));
        }

        public void Reduce<V>(double time, RDD<dynamic> rdd)
        {
            sumCount.RddCount += 1;
            var taken = rdd.Collect();
            Console.WriteLine("{0} taken.length = {1} , taken = {2}", TestUtils.NowMilli, taken.Length, taken);

            foreach (object record in taken)
            {
                sumCount.RecordCount += 1;
                KeyValuePair<string, V> kv = (KeyValuePair<string, V>)record;
                Console.WriteLine("{0} record: key = {2}, value = {3}, temp sumCount = {4}", TestUtils.NowMilli, record, kv.Key, TestUtils.GetValueText(kv.Value), sumCount);
                sumCount.LineCount += TestUtils.GetFirstElementValue(kv.Value);
            }

            Log(string.Format("Execute sumCount : {0}", sumCount));
        }

        protected void Log(string message)
        {
            Console.WriteLine("{0} {1} : {2}", TestUtils.NowMilli, this.GetType().Name, message);
        }
    }

    [Serializable]
    public class SumCountHelper : SumCountBase
    {
        //private new SumCount sumCount { get; set; }
        private new SumCount sumCount { get; set; }

        public override SumCount GetSumCount() { return this.sumCount; }

        public SumCountHelper(SumCount sum)
        {
            this.sumCount = sum;
        }

        public SumCountHelper()
        {
            this.sumCount = new SumCount(typeof(SumCountHelper).Name);
        }
    }

    [Serializable]
    public class SumCountStaticHelper : SumCountBase
    {
        private static SumCount _SumCount = new SumCount("StaticSum");
        private new SumCount sumCount { get { return _SumCount; } }

        public override SumCount GetSumCount() { return this.sumCount; }

        public SumCountStaticHelper() { }
    }


    [Serializable]
    public class SumCount
    {
        private long _lineCount = 0;
        private long _rddCount = 0;
        private long _recordCount = 0;
        private readonly string name;

        public SumCount(string nameForDebug = "")
        {
            this.name = nameForDebug;
        }
        
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
            return string.Format("LineCount = {0}, RddCount = {1}, RecordCount = {2}, name = {3}", LineCount, RddCount, RecordCount, name);
        }
    }
}
