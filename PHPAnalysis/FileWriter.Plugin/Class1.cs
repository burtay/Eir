﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FileWriter.Plugin;
using PHPAnalysis.Analysis;
using PHPAnalysis.Analysis.PHPDefinitions;

namespace WordPress.Plugin
{
    [Export(typeof(IVulnerabilityReporter))]
    [Export(typeof(IAnalysisStartingListener))]
    [Export(typeof(IAnalysisEndedListener))]
    public sealed class FileVulnerabilityReporter : IVulnerabilityReporter, IAnalysisStartingListener, IAnalysisEndedListener
    {
        private readonly string _vulnerabilityFile;
        private int vulnCounter = 1;
        private readonly DbFileWriter dbFileWriter;
        public FileVulnerabilityReporter()
        {
            dbFileWriter = new DbFileWriter();
            this._vulnerabilityFile = "scan-report.txt";
        }
        public void AnalysisStarting(object o, AnalysisStartingEventArgs e)
        {
            WriteInfoLine("              -----------------------------              ");
            WriteInfoLine("=============              Eir              =============");
            WriteInfoLine("============= Vulnerability Scanning Report =============");
            WriteInfoLine("              -----------------------------              ");
            WriteInfoLine("Target                  : " + e.Arguments.Target);
            WriteInfoLine("Scanning all subroutines: " + (e.Arguments.ScanAllSubroutines ? "Yes" : "No"));
            WriteInfoLine("---------------------------------------------------------");
            dbFileWriter.WriteStart(e.Arguments.Target);
        }

        public void AnalysisEnding(object o, AnalysisEndedEventArgs e)
        {
            WriteInfoLine("---------------------------------------------------------");
            WriteInfoLine("Time spent: " + e.TimeElapsed);
            WriteInfoLine("---------------------------------------------------------");
            dbFileWriter.WriteEnd(e.TimeElapsed);
        }

        public void ReportVulnerability(IVulnerabilityInfo vulnerabilityInfo)
        {
            WriteBeginVulnerability();
            WriteInfoLine("Message: " + vulnerabilityInfo.Message);
            WriteInfoLine("Include stack:" + String.Join(" → ", vulnerabilityInfo.IncludeStack));
            WriteInfo("Call stack: " + String.Join(" → ", vulnerabilityInfo.CallStack.Select(c => c.Name)));
            WriteFilePath(vulnerabilityInfo);
            WriteEndVulnerability();
            dbFileWriter.WriteVulnerability(vulnerabilityInfo);
        }

        public void ReportStoredVulnerability(IVulnerabilityInfo[] vulnerabilityPathInfos)
        {
            WriteBeginVulnerability();

            foreach (var pathInfo in vulnerabilityPathInfos)
            {
                WriteInfoLine(">> Taint Path: ");
                WriteInfoLine(pathInfo.Message);
                WriteInfoLine(String.Join("->", pathInfo.IncludeStack));
                WriteInfoLine("Callstack: " + String.Join(" → ", pathInfo.CallStack.Select(c => c.Name)));
                WriteFilePath(pathInfo);
            }

            WriteEndVulnerability();
            dbFileWriter.WriteStoredVulnerability(vulnerabilityPathInfos);
        }

        private void WriteBeginVulnerability()
        {
            File.AppendAllLines(_vulnerabilityFile, new[] { "|> " + vulnCounter++ });
        }
        private void WriteInfo(string info)
        {
            File.AppendAllText(_vulnerabilityFile, info);
        }

        public void WriteFilePath(IVulnerabilityInfo vulnInfo)
        {
            var funcList = vulnInfo.CallStack.Any() ? FunctionsHandler.Instance.LookupFunction(vulnInfo.CallStack.Peek().Name) : null;
            if (funcList == null || !funcList.Any())
            {
                return;
            }
            if (funcList.Count == 1)
            {
                var str = "Function/method: " + funcList.First().Name +
                          (string.IsNullOrWhiteSpace(funcList.First().File) ? "" : Environment.NewLine + "In file: " + funcList.First().File);
                WriteInfo(str);
            }
            else
            {
                WriteInfo("Function/method: " + funcList.First().Name + Environment.NewLine
                          + "File candidates: " + Environment.NewLine
                          + string.Join(Environment.NewLine, funcList.Select(x => x.File)));
            }
        }

        private void WriteInfoLine(string info)
        {
            WriteInfo(info);
            WriteInfo(Environment.NewLine);
        }
        private void WriteEndVulnerability()
        {
            File.AppendAllLines(_vulnerabilityFile, new[] { "", "<|" });
        }
    }
}
