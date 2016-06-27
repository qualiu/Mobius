using Microsoft.VisualStudio.TestTools.UnitTesting;
using testKeyValueStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Spark.CSharp.Streaming;

namespace testKeyValueStream.Tests
{
    [TestClass()]
    public class testKeyValueStreamTests
    {
        //[TestMethod()]
        //public void MainTest()
        //{
        //    Assert.Fail();
        //}

        [TestMethod()]
        public void ForEachRDDTest()
        {
            var sumCount = new SumCount();
            Assert.Fail();
        }
    }
}