package lzTest.CommonUtil

import java.text.SimpleDateFormat
import java.util.Date
import java.util.regex.Pattern
import java.io.{File}

import scala.reflect.runtime.universe._
import scala.reflect.ClassTag

//import scala.reflect.api.TypeTags.TypeTag
/**
  * Created by qualiu on 6/20/2016.
  */

trait LogBase extends Serializable {
  def log(message: String): Unit = {
    println(s"${TestUtil.NowMilli} : ${this.getClass.getName} $message")
  }
}

class TestUtil {
  //  implicit def StringToInt(x: String) : Int = x.toInt
  //  implicit def StringToDouble(x: String) : Double = x.toDouble
  private var index = -1
  val BOOL_VALUE_PATTERN = Pattern.compile("1|true", Pattern.CASE_INSENSITIVE)

  def getArgValue[ArgType: Manifest](args: Array[String], argName: String, defaultValue: ArgType, canOutOfArgs: Boolean = true): ArgType = {
    //def getArgValue[@specialized(Int, Double, Long, Float, Boolean, String) ArgType](args: Array[String], argName: String, defaultValue: ArgType, canOutOfArgs: Boolean = true): ArgType = {
    index += 1
    if (args.length > index) {
      println("args[" + index + "] : " + argName + " = " + args(index))
      var argValue = args(index)
      if (defaultValue.isInstanceOf[Boolean]) {
        argValue = BOOL_VALUE_PATTERN.matcher(argValue).find().asInstanceOf[ArgType].toString
      }
      //      println(s"return ${argValue.asInstanceOf[ArgType].getClass} = ${argValue.asInstanceOf[ArgType]} ")
      //      println(s"defaultValue type = ${defaultValue.getClass}")
      //      manifest[ArgType].erasure.cast(argValue).asInstanceOf[ArgType]
      //      argValue.asInstanceOf[ArgType]
      (defaultValue match {
        case defaultValue: Double => argValue.toDouble
        case defaultValue: Int => argValue.toInt
        case defaultValue: Float => argValue.toFloat
        case defaultValue: Long => argValue.toLong
        case defaultValue: Boolean => argValue.toBoolean
        case defaultValue: String => argValue
      }).asInstanceOf[ArgType]
    }
    else if (canOutOfArgs) {
      println("args[" + index + "] : " + argName + " = " + defaultValue)
      defaultValue
    }
    else {
      throw new IllegalArgumentException(f"must set $argName%s at arg[${index + 1}%d]")
    }
  }
}

object TestUtil {
  val TimeFomart = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss")
  val MilliFormat = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS")

  def NowMilli(): String = {
    MilliFormat.format(new Date())
  }

  def ArrayToText[T](arrayName: String, array: Array[T], takeMaxElementCount: Int = 9): String = {
    if (array == null) {
      s"${arrayName}[] = null"
    }
    else if (array.length == 0) {
      s"${arrayName}[0] = " + array
    }
    else if (array.length <= takeMaxElementCount) {
      s"${arrayName}[${array.length}] = " + array.mkString(", ")
    }
    else {
      s"${arrayName}[${array.length}] = " + array.take(takeMaxElementCount).mkString(", ") + ", ... , " + array.last
    }
  }

  def GetValueText[T](value: T, name: String = ""): String = {
    if (value == null) {
      null
    }
    else if (value.isInstanceOf[Int]) {
      value.toString
    }
    else if (value.isInstanceOf[Array[Int]]) {
      TestUtil.ArrayToText(name, value.asInstanceOf[Array[Int]])
    }
    else {
      value.toString
    }
  }

  def delete(path: File) {
    if (path.isDirectory)
      Option(path.listFiles).map(_.toList).getOrElse(Nil).foreach(delete(_))
    path.delete
  }

  def tryDelete(path: File): Unit = {
    try {
      TestUtil.delete(path)
      println(s"Deleted path : ${path}")
    } catch {
      case ex: Exception =>
        println(s"Failed to delete path : ${path}:")
        ex.printStackTrace(System.out)
    }
  }
}