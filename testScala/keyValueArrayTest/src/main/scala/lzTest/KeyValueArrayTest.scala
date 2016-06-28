package lzTest

import java.io.File
import java.util.Date

import org.apache.spark.storage.StorageLevel
import org.apache.spark.streaming.dstream.DStream
import org.apache.spark.{SparkConf, SparkContext}
import org.apache.spark.streaming._
import org.apache.spark.streaming.StreamingContext._
import org.apache.spark.streaming.dstream._

import scala.collection.mutable.ArrayBuffer
import scala.reflect.io.Path

/**
  * Hello world!
  *
  */
object KeyValueArrayTest extends LogBase {

  //  def Log(message: String): Unit = {
  //    println(s"${TestUtil.NowMilli} ${this.getClass.getCanonicalName} $message")
  //  }

  def ToDuration(seconds: Double): Duration = {
    if (seconds < 1) {
      Milliseconds((1000 * seconds).toInt)
    } else {
      Seconds(seconds.toInt)
    }
  }

  def main(args: Array[String]): Unit = {
    // val jar = KeyValueArrayTest.getClass().getProtectionDomain().getCodeSource().getLocation().toURI() //.getPath()
    //val jar = new File(KeyValueArrayTest.getClass().getProtectionDomain().getCodeSource().getLocation().toURI().getPath())
    //val jarDir = jar.getParentFile.getPath
    ArgParser4J.parse(args)
    log(s"will connect ${Args4JOptions.host}:${Args4JOptions.port}, batchSeconds = ${Args4JOptions.batchSeconds} s, windowSeconds = ${Args4JOptions.windowSeconds} s. slideSeconds = ${Args4JOptions.slideSeconds} s, checkpointDirectory = ${Args4JOptions.checkPointDirectory}, is-array-test = ${Args4JOptions.isArrayValue}")
    if (Args4JOptions.deleteCheckDirectory) {
      TestUtil.tryDelete(new File(Args4JOptions.checkPointDirectory))
    }
    val prefix = KeyValueArrayTest.getClass.getCanonicalName + (if (Args4JOptions.isArrayValue) (if (Args4JOptions.isUnevenArray) "-uneven" else "-even") + "-array" else "-single") + "-"
    //    var prefix = KeyValueArrayTest.getClass.getCanonicalName
    //    if(Args4JOptions.isUnevenArray) {
    //       if(Args4JOptions.isUnevenArray) prefix += "-uneven-array"
    //       else prefix += "-array"
    //    } else  prefix += "-single"
    //    prefix += "-"
    val conf = new SparkConf().setAppName(prefix + "app")
    val sc = new SparkContext(conf)
    val beginTime = new Date()
    val countList = new ArrayBuffer[Long]()
    def testOneStreaming(testTime: Long) {
      val timesInfo = " test[" + testTime + "]-" + Args4JOptions.testTimes + " "
      log("============== begin of " + timesInfo + " =========================")
      val ssc = new StreamingContext(sc, Seconds(Args4JOptions.batchSeconds))
      ssc.checkpoint(Args4JOptions.checkPointDirectory)
      val lines = ssc.socketTextStream(Args4JOptions.host, Args4JOptions.port, StorageLevel.MEMORY_AND_DISK_SER)
      val sumCount = new SumCount
      StartOneTest(sc, lines, sumCount, prefix)
      countList += sumCount.lineCount
      ssc.start()
      val startTime = new Date()
      ssc.awaitTerminationOrTimeout(Args4JOptions.runningSeconds * 1000)
      var validationMessage = ""
      var isValidateOK = true
      if (Args4JOptions.validateCount > 0) {
        isValidateOK = Args4JOptions.validateCount == sumCount.lineCount
        validationMessage = if (isValidateOK) ". Validation OK "
        else s". Validation failed : expect ${Args4JOptions.validateCount} but line count = ${sumCount.lineCount}"
      }

      val stopBegin = new Date()
      ssc.stop() //ssc.stop(stopSparkContext = true, stopGracefully = true)
      log(s"stopped ${timesInfo} used time = ${(new Date().getTime - stopBegin.getTime) / 1000} s.")
      log(s"============= end of ${timesInfo}, start from ${TestUtil.MilliFormat.format(startTime)} , used " +
        s"${(new Date().getTime - startTime.getTime) / 1000.0} s. total cost ${(new Date().getTime - beginTime.getTime) / 1000.0} s."
        + s" sumCount = { ${sumCount} } ${validationMessage}")

      if (!isValidateOK) {
        Args4JOptions.print("Trace arg :")
        throw new Exception(validationMessage)
      }
    }

    for (times <- 0 until Args4JOptions.testTimes) {
      testOneStreaming(times + 1)
    }

    log(s"finished all test , total test times = ${Args4JOptions.testTimes} , used time = ${(new Date().getTime - beginTime.getTime) / 1000.0} s"
      + s", countList[${countList.length}] = ${if (countList.length < 9) countList.mkString(",") else countList.take(9).mkString(", ") + ", ... , " + countList.last}")
  }

  def StartOneTest(sc: SparkContext, lines: DStream[String], sumCount: SumCount, prefix: String, suffix: String = ".txt"): Unit = {
    val isByKey = Args4JOptions.methodName.compareToIgnoreCase("reduceByKey") == 0
    if (!Args4JOptions.isArrayValue) {
      val pairs = lines.map(line => new ParseKeyValue(0).parse(line))
      val reducedStream = if (isByKey) pairs.reduceByKey((a, b) => Sum(a, b))
      else pairs.reduceByKeyAndWindow((a, b) => Sum(a, b), (a, b) => InverseSum(a, b), Seconds(Args4JOptions.windowSeconds), Seconds(Args4JOptions.slideSeconds))
      ForEachRDD("KeyValue", reducedStream, sumCount, prefix, suffix)
    }
    else {
      val pairs = if (Args4JOptions.isUnevenArray) lines.map(line => new ParseKeyValueUnevenArray(Args4JOptions.elementCount).parse(line))
      else lines.map(line => new ParseKeyValueArray(Args4JOptions.elementCount).parse(line)) //{ val kv = new ParseKeyValueArray(elementCount).parse(line) ; new Tuple2[String, Array[Int]]("mykey", kv._2) })
      val reducedStream = if (isByKey) pairs.reduceByKey((a, b) => new SumReduceHelper(Args4JOptions.checkArray) SumArray(a, b))
        else pairs.reduceByKeyAndWindow((a, b) => new SumReduceHelper(Args4JOptions.checkArray).SumArray(a, b),
          (a, b) => new SumReduceHelper(Args4JOptions.checkArray).InverseSumArray(a, b), Seconds(Args4JOptions.windowSeconds), Seconds(Args4JOptions.slideSeconds))
      val title = if (Args4JOptions.isUnevenArray) "KeyValueUnevenArray" else "KeyValueArray"
      ForEachRDD(title, reducedStream, sumCount, prefix, suffix)
    }
  }

  def ForEachRDD[V](title: String, reducedStream: DStream[Tuple2[String, V]], sumCount: SumCount, prefix: String, suffix: String = ".txt"): Unit = {
    log("ForEachRDD " + title)
    //    val arr = new ArrayBuffer[Int]
    //    reducedStream.foreachRDD((rdd, time) => {
    //      val nv = rdd.collect().map(_._2 match {
    //        case arr: Array[Int] => arr(0)
    //        case ele: Int => ele
    //        case _ => throw new IllegalArgumentException(s"illegal value type in reduce")
    //      })
    //      arr ++= nv
    //    })
    //
    //    val arrSum = arr.sum[Int]
    //    Log(s"arrSum = ${arrSum}, arr.length = ${arr.length}")

    //    reducedStream.foreachRDD((rdd, time) => {
    //      val sum = new SumReduceHelper(Args4JOptions.checkArray).forechRDD(rdd, time)
    //      sumCount.setCount(sum)
    //    })


    reducedStream.foreachRDD((rdd, time) => {
      sumCount.add(0, 1, 0)
      var taken = rdd.collect()
      for (record <- taken) {
        val kv = record.asInstanceOf[Tuple2[String, Array[Int]]]
        sumCount.add(kv._2(0), 0, 1)
        log(s"record key = ${kv._1} , ${TestUtil.GetValueText(kv._2, "value")}, sum : ${sumCount.toString()}")
      }
    })

    //    log(s"${title} lineCount = ${lineCount}, rddCount = ${rddCount}, recordCount = ${recordCount}, sumCount = ${sumCount}")
    if (Args4JOptions.saveTxtDirectory.length > 0) {
      val header = new File(Args4JOptions.saveTxtDirectory, prefix).toString
      reducedStream.saveAsTextFiles(header, suffix)
    }
  }

  def Sum(a: Int, b: Int): Int = {
    log(s"Sum: ${a} + ${b} = ${a + b}")
    a + b
  }

  def InverseSum(a: Int, b: Int): Int = {
    log(s"InverseSum : ${a} - ${b} = ${a - b}")
    a - b
  }

  def SumArray(a: Array[Int], b: Array[Int]): Array[Int] = {
    val checkArrayBeforeSum = Args4JOptions.checkArray
    log(s"SumArray() ${TestUtil.ArrayToText("a", a)} + ${TestUtil.ArrayToText("b", b)} , checkArrayBeforeSum = ${checkArrayBeforeSum}")
    if (checkArrayBeforeSum) {
      if (a == null || b == null) {
        return if (a == null) b else a
      }
      else if (a.length == 0 || b.length == 0) {
        return if (a.length == 0) b else a
      }
    }
    var count = if (checkArrayBeforeSum) math.min(a.length, b.length) else a.length
    var c = new Array[Int](count)
    for (k <- 0 until c.length) {
      c(k) = a(k) + b(k)
    }
    log(s"SumArray() ${TestUtil.ArrayToText("a", a)} + ${TestUtil.ArrayToText("b", b)} = ${TestUtil.ArrayToText("c", c)}")
    c
  }

  def InverseSumArray(a: Array[Int], b: Array[Int]): Array[Int] = {
    val checkArrayBeforeSum = Args4JOptions.checkArray
    log(s"InverseSumArray ${TestUtil.ArrayToText("a", a)} - ${TestUtil.ArrayToText("b", b)}, checkArrayBeforeSum = ${checkArrayBeforeSum}")
    if (checkArrayBeforeSum) {
      if (a == null || b == null) {
        return if (a == null) b else a
      }
      else if (a.length == 0 || b.length == 0) {
        return if (a.length == 0) b else a
      }
    }
    var count = if (checkArrayBeforeSum) math.min(a.length, b.length) else a.length
    var c = new Array[Int](count)
    for (k <- 0 until c.length) {
      c(k) = a(k) - b(k)
    }
    log(s"InverseSumArray() ${TestUtil.ArrayToText("a", a)} - ${TestUtil.ArrayToText("b", b)} = ${TestUtil.ArrayToText("c", c)}")
    c
  }
}