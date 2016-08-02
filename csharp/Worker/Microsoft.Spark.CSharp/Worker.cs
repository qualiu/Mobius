﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Interop.Ipc;
using Microsoft.Spark.CSharp.Network;
using Microsoft.Spark.CSharp.Services;
using Microsoft.Spark.CSharp.Sql;
using Razorvine.Pickle;
using System.Configuration;

namespace Microsoft.Spark.CSharp
{
    internal class SparkCLRAssemblyHandler
    {
        private readonly ConcurrentDictionary<string, Assembly> assemblyDict = new ConcurrentDictionary<string, Assembly>();
        private readonly ConcurrentDictionary<string, bool> loadedFiles = new ConcurrentDictionary<string, bool>();

        public void LoadAssemblies(string[] files)
        {
            foreach (var assembly in from f in files.Where(f => new FileInfo(f).Length > 0).Select(Path.GetFullPath) where loadedFiles.TryAdd(f, true) select Assembly.Load(File.ReadAllBytes(f)))
            {
                if (!assemblyDict.ContainsKey(assembly.FullName))
                {
                    assemblyDict[assembly.FullName] = assembly;
                }
                else
                {
                    Console.Error.WriteLine("Already loaded assebmly " + assembly.FullName);
                }
            }
        }

        public Assembly Handle(object source, ResolveEventArgs e)
        {
            if (assemblyDict.ContainsKey(e.Name))
            {
                return assemblyDict[e.Name];
            }

            return null;
        }
    }

    /// <summary>
    /// Worker implementation for SparkCLR. The implementation is identical to the 
    /// worker used in PySpark. The RDD implementation to fork an external process
    /// and pipe data in and out between JVM & the other runtime is already implemented in PySpark.
    /// SparkCLR uses the same design and implementation of PythonRDD (CSharpRDD extends PythonRDD).
    /// So the worker behavior is also the identical between PySpark and SparkCLR.
    /// </summary>
    public class Worker
    {
        private static readonly DateTime UnixTimeEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static ILoggerService logger = null;

        private static SparkCLRAssemblyHandler assemblyHandler = null;

        public static void Main(string[] args)
        {
            assemblyHandler = new SparkCLRAssemblyHandler();
            AppDomain.CurrentDomain.AssemblyResolve += assemblyHandler.Handle;

            // can't initialize logger early because in MultiThreadWorker mode, JVM will read C#'s stdout via
            // pipe. When initialize logger, some unwanted info will be flushed to stdout. But we can still
            // use stderr
            Console.Error.WriteLine("input args: [{0}] SocketWrapper: [{1}]",
                string.Join(" ", args), SocketFactory.SocketWrapperType);

            if (args.Length != 2)
            {
                Console.Error.WriteLine("Wrong number of args: {0}, will exit", args.Count());
                Environment.Exit(-1);
            }

            if ("pyspark.daemon".Equals(args[1]))
            {
                if (SocketFactory.SocketWrapperType == SocketWrapperType.Rio)
                {
                    // In daemon mode, the socket will be used as server.
                    // Use ThreadPool to retrieve RIO socket results has good performance
                    // than a single thread.
                    RioNative.SetUseThreadPool(true);
                }

                var multiThreadWorker = new MultiThreadWorker();
                multiThreadWorker.Run();
            }
            else
            {
                RunSimpleWorker();
            }
        }

        /// <summary>
        /// The C# worker process is used to execute only one JVM Task. It will exit after the task is finished.
        /// </summary>
        private static void RunSimpleWorker()
        {
            try
            {
                InitializeLogger();
                logger.LogInfo("RunSimpleWorker ...");
                PrintFiles();

                int javaPort = int.Parse(Console.ReadLine()); //reading port number written from JVM
                logger.LogDebug("Port number used to pipe in/out data between JVM and CLR {0}", javaPort);
                var socket = InitializeSocket(javaPort);
                TaskRunner taskRunner = new TaskRunner(0, socket, false);
                taskRunner.Run();
            }
            catch (Exception e)
            {
                logger.LogError("RunSimpleWorker failed with exception, will Exit");
                logger.LogException(e);
                Environment.Exit(-1);
            }

            logger.LogInfo("RunSimpleWorker finished successfully");
        }

        public static void InitializeLogger()
        {
            try
            {
                // if there exists exe.config file, then use log4net
                if (File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
                {
                    LoggerServiceFactory.SetLoggerService(Log4NetLoggerService.Instance);
                }
                logger = LoggerServiceFactory.GetLogger(typeof(Worker));
            }
            catch (Exception e)
            {
                Console.WriteLine("InitializeLogger exception {0}, will exit", e);
                Environment.Exit(-1);
            }
        }

        private static ISocketWrapper InitializeSocket(int javaPort)
        {
            var socket = SocketFactory.CreateSocket();
            socket.Connect(IPAddress.Loopback, javaPort);
            return socket;
        }

        public static bool ProcessStream(Stream networkStream, int splitIndex)
        {
            logger.LogInfo(string.Format("Start of stream processing, splitIndex: {0}", splitIndex));
            bool readComplete = true;   // Whether all input data from the socket is read though completely

            try
            {
                DateTime bootTime = DateTime.UtcNow;

                string ver = SerDe.ReadString(networkStream);
                logger.LogDebug("version: " + ver);

                //// initialize global state
                //shuffle.MemoryBytesSpilled = 0
                //shuffle.DiskBytesSpilled = 0

                // fetch name of workdir
                string sparkFilesDir = SerDe.ReadString(networkStream);
                logger.LogDebug("spark_files_dir: " + sparkFilesDir);
                //SparkFiles._root_directory = sparkFilesDir
                //SparkFiles._is_running_on_worker = True

                ProcessIncludesItems(networkStream);

                ProcessBroadcastVariables(networkStream);

                Accumulator.threadLocalAccumulatorRegistry = new Dictionary<int, Accumulator>();

                var formatter = ProcessCommand(networkStream, splitIndex, bootTime);

                // Mark the beginning of the accumulators section of the output
                SerDe.Write(networkStream, (int)SpecialLengths.END_OF_DATA_SECTION);

                WriteAccumulatorValues(networkStream, formatter);

                int end = SerDe.ReadInt(networkStream);

                // check end of stream
                if (end == (int)SpecialLengths.END_OF_STREAM)
                {
                    SerDe.Write(networkStream, (int)SpecialLengths.END_OF_STREAM);
                    logger.LogDebug("END_OF_STREAM: " + (int)SpecialLengths.END_OF_STREAM);
                }
                else
                {
                    // This may happen when the input data is not read completely, e.g., when take() operation is performed
                    logger.LogWarn(string.Format("**** unexpected read: {0}, not all data is read", end));
                    // write a different value to tell JVM to not reuse this worker
                    SerDe.Write(networkStream, (int)SpecialLengths.END_OF_DATA_SECTION);
                    readComplete = false;
                }

                networkStream.Flush();

                // log bytes read and write
                logger.LogDebug(string.Format("total read bytes: {0}", SerDe.totalReadNum));
                logger.LogDebug(string.Format("total write bytes: {0}", SerDe.totalWriteNum));

                logger.LogDebug("Stream processing completed successfully");
            }
            catch (Exception e)
            {
                logger.LogError("ProcessStream failed with exception:");
                logger.LogError(e.ToString());
                try
                {
                    logger.LogError("Trying to write error to stream");
                    SerDe.Write(networkStream, e.ToString());
                }
                catch (IOException)
                {
                    // JVM close the socket
                }
                catch (Exception ex)
                {
                    logger.LogError("Writing exception to stream failed with exception:");
                    logger.LogException(ex);
                }
                throw e;
            }

            logger.LogInfo(string.Format("Stop of stream processing, splitIndex: {0}, readComplete: {1}", splitIndex, readComplete));
            return readComplete;
        }

        private static void ProcessIncludesItems(Stream networkStream)
        {
            // fetch names of includes - not used //TODO - complete the impl
            int numberOfIncludesItems = SerDe.ReadInt(networkStream);
            logger.LogDebug("num_includes: " + numberOfIncludesItems);

            if (numberOfIncludesItems > 0)
            {
                for (int i = 0; i < numberOfIncludesItems; i++)
                {
                    string filename = SerDe.ReadString(networkStream);
                }
            }
        }

        private static void ProcessBroadcastVariables(Stream networkStream)
        {
            // fetch names and values of broadcast variables
            int numBroadcastVariables = SerDe.ReadInt(networkStream);
            logger.LogDebug("num_broadcast_variables: " + numBroadcastVariables);

            if (numBroadcastVariables > 0)
            {
                for (int i = 0; i < numBroadcastVariables; i++)
                {
                    long bid = SerDe.ReadLong(networkStream);
                    if (bid >= 0)
                    {
                        string path = SerDe.ReadString(networkStream);
                        Broadcast.broadcastRegistry[bid] = new Broadcast(path);
                    }
                    else
                    {
                        bid = -bid - 1;
                        Broadcast bc;
                        Broadcast.broadcastRegistry.TryRemove(bid, out bc);
                    }
                }
            }
        }

        private static IFormatter ProcessCommand(Stream networkStream, int splitIndex, DateTime bootTime)
        {
            int lengthOfCommandByteArray = SerDe.ReadInt(networkStream);
            logger.LogDebug("command length: " + lengthOfCommandByteArray);

            IFormatter formatter = new BinaryFormatter();

            if (lengthOfCommandByteArray > 0)
            {
                var commandProcessWatch = new Stopwatch();
                commandProcessWatch.Start();

                int stageId = ReadDiagnosticsInfo(networkStream);

                string deserializerMode = SerDe.ReadString(networkStream);
                logger.LogDebug("Deserializer mode: " + deserializerMode);

                string serializerMode = SerDe.ReadString(networkStream);
                logger.LogDebug("Serializer mode: " + serializerMode);

                string runMode = SerDe.ReadString(networkStream);
                if ("R".Equals(runMode, StringComparison.InvariantCultureIgnoreCase))
                {
                    var compilationDumpDir = SerDe.ReadString(networkStream);
                    if (Directory.Exists(compilationDumpDir))
                    {
                        assemblyHandler.LoadAssemblies(Directory.GetFiles(compilationDumpDir, "ReplCompilation.*",
                            SearchOption.TopDirectoryOnly));
                    }
                    else
                    {
                        logger.LogError("Directory " + compilationDumpDir + " dose not exist.");
                    }
                }

                byte[] command = SerDe.ReadBytes(networkStream);

                logger.LogDebug("command bytes read: " + command.Length);
                var stream = new MemoryStream(command);

                var workerFunc = (CSharpWorkerFunc)formatter.Deserialize(stream);
                var func = workerFunc.Func;
                logger.LogDebug(
                    "------------------------ Printing stack trace of workerFunc for ** debugging ** ------------------------------");
                logger.LogDebug(workerFunc.StackTrace);
                logger.LogDebug(
                    "--------------------------------------------------------------------------------------------------------------");
                DateTime initTime = DateTime.UtcNow;

                int count = 0;
                int nullMessageCount = 0;

                var mobiusConfig = (System.Collections.IDictionary)ConfigurationManager.GetSection("mobius");
                var bufferConfig = mobiusConfig == null ? null : mobiusConfig["spark.mobius.network.buffersize"];
                var bufferSize = bufferConfig == null ? 1024 * 8 : int.Parse(bufferConfig.ToString());

                var bufferStream = new BufferedStream(networkStream, bufferSize); // 8k buffer
                var funcProcessWatch = Stopwatch.StartNew();
                foreach (var message in func(splitIndex, GetIterator(networkStream, deserializerMode)))
                {
                    funcProcessWatch.Stop();

                    if (object.ReferenceEquals(null, message))
                    {
                        nullMessageCount++;
                        continue;
                    }

                    WriteOutput(bufferStream, serializerMode, message, formatter);
                    count++;
                    funcProcessWatch.Start();
                }

                logger.LogDebug("Output entries count: " + count);
                logger.LogDebug("Null messages count: " + nullMessageCount);

                //if profiler:
                //    profiler.profile(process)
                //else:
                //    process()

                WriteDiagnosticsInfo(bufferStream, bootTime, initTime);

                bufferStream.Flush();
                commandProcessWatch.Stop();

                // log statistics
                logger.LogInfo("func process time: {0}, spark.mobius.network.buffersize = {1}", funcProcessWatch.ElapsedMilliseconds, bufferSize);
                logger.LogInfo("stage {0}, command process time: {1}", stageId, commandProcessWatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarn("lengthOfCommandByteArray = 0. Nothing to execute :-(");
            }

            return formatter;
        }

        private static void WriteOutput(Stream networkStream, string serializerMode, dynamic message, IFormatter formatter)
        {
            var buffer = GetSerializedMessage(serializerMode, message, formatter);
            if (buffer == null)
            {
                logger.LogError("Buffer is null");
            }

            if (buffer.Length <= 0)
            {
                logger.LogError("Buffer length {0} cannot be <= 0", buffer.Length);
            }

            //Debug.Assert(buffer != null);
            //Debug.Assert(buffer.Length > 0);
            SerDe.Write(networkStream, buffer.Length);
            SerDe.Write(networkStream, buffer);
        }

        private static byte[] GetSerializedMessage(string serializerMode, dynamic message, IFormatter formatter)
        {
            byte[] buffer;

            switch ((SerializedMode)Enum.Parse(typeof(SerializedMode), serializerMode))
            {
                case SerializedMode.None:
                    buffer = message as byte[];
                    break;

                case SerializedMode.String:
                    buffer = SerDe.ToBytes(message as string);
                    break;

                case SerializedMode.Row:
                    var pickler = new Pickler();
                    buffer = pickler.dumps(new ArrayList { message });
                    break;

                default:
                    try
                    {
                        var ms = new MemoryStream();
                        formatter.Serialize(ms, message);
                        buffer = ms.ToArray();
                    }
                    catch (Exception)
                    {
                        logger.LogError("Exception serializing output");
                        logger.LogError("{0} : {1}", message.GetType().Name, message.GetType().FullName);
                        throw;
                    }
                    break;
            }

            return buffer;
        }


        private static int ReadDiagnosticsInfo(Stream networkStream)
        {
            int rddId = SerDe.ReadInt(networkStream);
            int stageId = SerDe.ReadInt(networkStream);
            int partitionId = SerDe.ReadInt(networkStream);
            logger.LogInfo(string.Format("rddInfo: rddId {0}, stageId {1}, partitionId {2}", rddId, stageId, partitionId));
            return stageId;
        }

        private static void WriteDiagnosticsInfo(Stream networkStream, DateTime bootTime, DateTime initTime)
        {
            DateTime finishTime = DateTime.UtcNow;
            const string format = "MM/dd/yyyy hh:mm:ss.fff tt";
            logger.LogDebug(string.Format("bootTime: {0}, initTime: {1}, finish_time: {2}",
                bootTime.ToString(format), initTime.ToString(format), finishTime.ToString(format)));
            SerDe.Write(networkStream, (int)SpecialLengths.TIMING_DATA);
            SerDe.Write(networkStream, ToUnixTime(bootTime));
            SerDe.Write(networkStream, ToUnixTime(initTime));
            SerDe.Write(networkStream, ToUnixTime(finishTime));

            SerDe.Write(networkStream, 0L); //shuffle.MemoryBytesSpilled  
            SerDe.Write(networkStream, 0L); //shuffle.DiskBytesSpilled
        }

        private static void WriteAccumulatorValues(Stream networkStream, IFormatter formatter)
        {
            SerDe.Write(networkStream, Accumulator.threadLocalAccumulatorRegistry.Count);
            foreach (var item in Accumulator.threadLocalAccumulatorRegistry)
            {
                var ms = new MemoryStream();
                var value =
                    item.Value.GetType()
                        .GetField("value", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(item.Value);
                logger.LogDebug(string.Format("({0}, {1})", item.Key, value));
                formatter.Serialize(ms, new KeyValuePair<int, dynamic>(item.Key, value));
                byte[] buffer = ms.ToArray();
                SerDe.Write(networkStream, buffer.Length);
                SerDe.Write(networkStream, buffer);
            }
        }

        public static void PrintFiles()
        {
            logger.LogDebug("Files available in executor");
            var driverFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var files = Directory.EnumerateFiles(driverFolder);
            foreach (var file in files)
            {
                logger.LogDebug(file);
            }
        }

        private static long ToUnixTime(DateTime dt)
        {
            return (long)(dt - UnixTimeEpoch).TotalMilliseconds;
        }

        private static IEnumerable<dynamic> GetIterator(Stream inputStream, string serializedMode)
        {
            logger.LogInfo("Serialized mode in GetIterator: " + serializedMode);
            IFormatter formatter = new BinaryFormatter();
            var mode = (SerializedMode)Enum.Parse(typeof(SerializedMode), serializedMode);
            int messageLength;
            Stopwatch watch = Stopwatch.StartNew();
            while ((messageLength = SerDe.ReadInt(inputStream)) != (int)SpecialLengths.END_OF_DATA_SECTION)
            {
                watch.Stop();
                if (messageLength > 0 || messageLength == (int)SpecialLengths.NULL)
                {
                    watch.Start();
                    byte[] buffer = messageLength > 0 ? SerDe.ReadBytes(inputStream, messageLength) : null;
                    watch.Stop();
                    switch (mode)
                    {
                        case SerializedMode.String:
                            {
                                if (messageLength > 0)
                                {
                                    if (buffer == null)
                                    {
                                        logger.LogDebug("Buffer is null. Message length is {0}", messageLength);
                                    }
                                    yield return SerDe.ToString(buffer);
                                }
                                else
                                {
                                    yield return null;
                                }
                                break;
                            }

                        case SerializedMode.Row:
                            {
                                Debug.Assert(messageLength > 0);
                                var unpickledObjects = PythonSerDe.GetUnpickledObjects(buffer);
                                foreach (var row in unpickledObjects.Select(item => (item as RowConstructor).GetRow()))
                                {
                                    yield return row;
                                }
                                break;
                            }

                        case SerializedMode.Pair:
                            {
                                byte[] pairKey = buffer;
                                byte[] pairValue = null;

                                watch.Start();
                                int valueLength = SerDe.ReadInt(inputStream);
                                if (valueLength > 0)
                                {
                                    pairValue = SerDe.ReadBytes(inputStream, valueLength);
                                }
                                else if (valueLength == (int)SpecialLengths.NULL)
                                {
                                    pairValue = null;
                                }
                                else
                                {
                                    throw new Exception(string.Format("unexpected valueLength: {0}", valueLength));
                                }
                                watch.Stop();

                                yield return new KeyValuePair<byte[], byte[]>(pairKey, pairValue);
                                break;
                            }

                        case SerializedMode.None: //just return raw bytes
                            {
                                yield return buffer;
                                break;
                            }

                        case SerializedMode.Byte:
                        default:
                            {
                                if (buffer != null)
                                {
                                    var ms = new MemoryStream(buffer);
                                    yield return formatter.Deserialize(ms);
                                }
                                else
                                {
                                    yield return null;
                                }
                                break;
                            }
                    }
                }
                watch.Start();
            }

            logger.LogInfo(string.Format("total receive time: {0}", watch.ElapsedMilliseconds));
        }
    }
}
