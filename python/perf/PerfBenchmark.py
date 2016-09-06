# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""
Perf benchmark that users Freebase deletions data
This data is licensed under CC-BY license (http:# creativecommons.org/licenses/by/2.5)
Data is available for download at "Freebase Deleted Triples" at https:# developers.google.com/freebase
Data format - CSV, size - 8 GB uncompressed
Columns in the dataset are
    1. creation_timestamp (Unix epoch time in milliseconds)
    2. creator
    3. deletion_timestamp (Unix epoch time in milliseconds)
    4. deletor
    5. subject (MID)
    6. predicate (MID)
    7. object (MID/Literal)
    8. language_code
"""
from __future__ import print_function
import sys, os, re, time
from datetime import datetime
import inspect

class PerfBenchmark(object):
    PerfNameResults = {} ## <string, List<long>>
    ExecutionTimeList = []  # List<long>

    def RunPerfSuite(perfClass, args, sparkContext, sqlContext):
        for name, fun in inspect.getmembers(perfClass, lambda fun : inspect.isfunction(fun) and fun.__name__.startswith("Run")) :
            PerfBenchmark.ExecutionTimeList = []
            runCount = int(args[1])
            for k in range(runCount):
                print(str(datetime.now()) + " Starting perf suite : " + str(perfClass.__name__) + "." + str(name) + " times[" + str(k+1) + "]-" + str(runCount))
                fun(args, sparkContext, sqlContext)

            executionTimeListRef = []
            for v in PerfBenchmark.ExecutionTimeList :
                executionTimeListRef.append(v)

            PerfBenchmark.PerfNameResults[name] = executionTimeListRef


    def ReportResult() :
        print(str(datetime.now()) + " ** Printing results of the perf run (python) **")
        allMedianCosts = {}
        for name in PerfBenchmark.PerfNameResults :
            perfResult = PerfBenchmark.PerfNameResults[name]
            # print(str(datetime.now()) + " " + str(result) + " time costs : " + ", ".join(("%.3f" % e) for e in perfResult))
            # multiple enumeration happening - ignoring that for now
            precision = "%.0f"
            minimum = precision % min(perfResult)
            maximum = precision % max(perfResult)
            runCount = len(perfResult)
            avg = precision % (sum(perfResult) / runCount)
            median = precision % PerfBenchmark.GetMedian(perfResult)
            values = ", ".join((precision % e) for e in perfResult)
            print(str(datetime.now()) + " ** Execution time for " + str(name) + " in seconds: " + \
                "Min=" + str(minimum) + ", Max=" + str(maximum) + ", Average=" + str(avg) + ", Median=" + str(median) + \
                ".  Run count=" + str(runCount)+ ", Individual execution duration=[" + values + "]")
            allMedianCosts[name] = median

        print(str(datetime.now()) + " ** *** **")
        print(str(datetime.now()) + " Run count = " + str(runCount) + ", all median time costs[" + str(len(allMedianCosts))+ "] : " + "; ".join((e + "=" + allMedianCosts[e]) for e in allMedianCosts))

    def GetMedian(values) :
        itemCount = len(values)
        values.sort()
        if itemCount == 1:
          return values[0]

        if itemCount%2 == 0:
          return (values[int(itemCount/2)] + values[int(itemCount/2 - 1)])/2

        return values[int((itemCount-1)/2)]
