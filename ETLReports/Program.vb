Imports System
Imports System.Data
Imports System.IO
Imports System.Threading
Imports Microsoft.Windows.EventTracing
Imports Microsoft.Windows.EventTracing.Cpu
Imports Microsoft.Windows.EventTracing.Disk
Imports Microsoft.Windows.EventTracing.Events
Imports Microsoft.Windows.EventTracing.File
Imports Microsoft.Windows.EventTracing.Memory
Imports Microsoft.Windows.EventTracing.Network
Imports Microsoft.Windows.EventTracing.Processes
Imports Microsoft.Windows.EventTracing.ScheduledTasks
Imports Microsoft.Windows.EventTracing.Services
Imports Microsoft.Windows.EventTracing.Symbols
Imports Microsoft.Diagnostics.Tracing
Imports Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP
Imports Microsoft.Diagnostics.Tracing.Parsers

Module Program

    Dim filename As String = ""                             '.etl file path from command arg
    Dim processor As String = ""                           'processor from command arg
    Dim outputfolder As String = ""                      'folder path from command arg
    Dim outputfile As String = ""                           'computed full output file name from folder, file and processor
    Dim milli As Int32 = 0                                      'For duration filters if supported by processor
    Dim measureruntime As Boolean = False      'switch for showing trace processing stats in console
    Dim measurestart As DateTime = Now          'Time processing of .etl began
    Dim tracesettings As New TraceProcessorSettings     'Settings for allowing lost events and time inversion
    Dim isBootTrace As Boolean = False
    Sub Main()
        Main(Environment.GetCommandLineArgs())
    End Sub

    Private Sub Main(ByVal args() As String)
        If args.Length > 1 Then

            Select Case args(1).ToLower
                Case "help"
                    ShowHelp()
                    End
                Case "-help"
                    ShowHelp()
                    End
                Case "--help"
                    ShowHelp()
                    End
                Case "/help"
                    ShowHelp()
                    End
                Case "h"
                    ShowHelp()
                    End
                Case "-h"
                    ShowHelp()
                    End
                Case "/h"
                    ShowHelp()
                    End
                Case "?"
                    ShowHelp()
                    End
                Case "-?"
                    ShowHelp()
                    End
                Case "/?"
                    ShowHelp()
                    End
            End Select

            'Console.ForegroundColor = ConsoleColor.Red

            For Each arg In args
                If arg.StartsWith("--measure") Then
                    measureruntime = True
                End If
            Next

            For Each arg In args
                If arg.StartsWith("--ms:") Then
                    milli = arg.Replace("'", "").Replace("""", "").Trim(" ").Remove(0, 5)
                    milli = milli * 1000
                End If
            Next

            For Each arg In args
                If arg.StartsWith("--infile:") Then
                    filename = arg.Replace("'", "").Replace("""", "").Trim(" ").Remove(0, 9)
                End If
            Next

            For Each arg In args
                If arg.StartsWith("--outfolder:") Then
                    outputfolder = arg.Replace("'", "").Replace("""", "").Trim(" ").Trim("\").Remove(0, 12)
                    outputfolder = outputfolder + "\"
                End If
            Next

            For Each arg In args
                If arg.StartsWith("--processor:") Then
                    processor = arg.Replace("'", "").Replace("""", "").Trim(" ").Remove(0, 12)
                End If
            Next

            If Not File.Exists(filename) Then
                Console.WriteLine("File not found. " + filename)
                ShowHelp()
            End If

            Try
                If (Path.GetDirectoryName(outputfolder).Intersect(Path.GetInvalidPathChars()).Any() Or Not Directory.Exists(outputfolder)) Then
                    Console.WriteLine("Output folder not valid. " + outputfolder)
                    ShowHelp()
                Else
                    File.Delete(outputfolder + Path.GetFileNameWithoutExtension(filename) + "_" + processor + ".csv")
                End If
            Catch ex As Exception
                Console.WriteLine("Folder not found. " + filename)
                ShowHelp()
            End Try

            If Nothing = filename Then
                Console.WriteLine("No filename specified.")
                ShowHelp()
            End If

            If Nothing = processor Then
                Console.WriteLine("No processor specified.")
                ShowHelp()
            End If

            If Not File.Exists(filename) Then
                Console.WriteLine("Filename not found. " + filename)
                ShowHelp()
            End If

            Go()
        Else
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("No filenames or processor specified.")
            ShowHelp()
        End If

        Console.ResetColor()

    End Sub

    Private Sub Go()

        tracesettings.AllowLostEvents = True
        tracesettings.AllowTimeInversion = True

        If measureruntime Then
            Console.WriteLine(measurestart)
        End If

        outputfile = (outputfolder + Path.GetFileNameWithoutExtension(filename) + "_" + processor + ".csv")

        Select Case processor.ToLower
            Case "processes"
                Processes()
            Case "tasks"
                Tasks()
            Case "gpos"
                GPOS()
            Case "winlogon"
                Winlogon()
            Case "pnp"
                PnP()
            Case "services"
                Services()
            Case "hardfaults"
                HardFaults()
            Case "diskio"
                DiskIO()
            Case "fileio"
                FileIO()
            Case "providerinfo"
                ProviderInfo()
            Case "minifilter"
                MiniFilter()
            Case "minifiltersummary"
                MiniFilterSummary()
            Case "cpusample"
                CpuSample(True)
            Case "cpusamplenoidle"
                CpuSample(False)
            Case "bootphases"
                BootPhases()
            Case "processzombies"
                ProcessZombies()
            Case "cpucontextswitch"
                CpuContextSwitch()
            Case "marks"
                Marks()

            Case Else
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("No processor `" + processor + "` found.")
                ShowHelp()
                End
        End Select

        If measureruntime Then
            Console.WriteLine(vbLf)
            Console.WriteLine(Now)
        End If

    End Sub

    Private Sub CpuContextSwitch()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("CPU" + "," + "In_PID" + "," + "In_TID" + "," + "In_Pri" + "," + "In_Wait" + "," + "In_Quantum" + "," + "In_QOS" + "," + "Out_PID" + "," + "Out_TID" + "," + "Out_Pri" + "," + "Out_Quantum" + "," + "Out_QOS")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)

                Try
                    Console.WriteLine("Loading symbols...")
                    Dim pendingsymbolData As IPendingResult(Of ISymbolDataSource) = trace.UseSymbols
                    Dim pendingData As IPendingResult(Of ICpuSchedulingDataSource) = trace.UseCpuSchedulingData

                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()

                    Dim symbolData As ISymbolDataSource = pendingsymbolData.Result
                    Dim traceData As ICpuSchedulingDataSource = pendingData.Result

                    symbolData.LoadSymbolsAsync(SymCachePath.Automatic, SymbolPath.Automatic).GetAwaiter.GetResult()

                    Dim count As Int32 = 1
                    Dim iserror As Boolean = False

                    Dim cs_cpu As Int32 = -1
                    Dim cs_in_pid = -1
                    Dim cs_in_pri = -1
                    Dim cs_in_tid = -1
                    Dim cs_in_wait = -1
                    Dim cs_in_quantum = -1
                    Dim cs_in_qos = -1

                    Dim cs_out_pid = -1
                    Dim cs_out_pri = -1
                    Dim cs_out_tid = -1
                    Dim cs_out_quantum = -1
                    Dim cs_out_qos = -1

                    For Each contextswitchinfo As IContextSwitch In traceData.ContextSwitches

                        cs_cpu = contextswitchinfo.Processor

                        Try
                            cs_in_pid = contextswitchinfo.SwitchIn.Process.Id
                        Catch ex As Exception

                        End Try

                        Try
                            cs_in_pri = contextswitchinfo.SwitchIn.Priority
                        Catch ex As Exception

                        End Try

                        Try
                            cs_in_tid = contextswitchinfo.SwitchIn.ThreadId
                        Catch ex As Exception

                        End Try

                        Try
                            cs_in_wait = contextswitchinfo.SwitchIn.WaitTime.Value.TotalMilliseconds
                        Catch ex As Exception

                        End Try

                        Try
                            cs_in_quantum = contextswitchinfo.SwitchIn.Quantum
                        Catch ex As Exception

                        End Try

                        Try
                            cs_in_qos = contextswitchinfo.SwitchIn.ThreadQualityOfService
                        Catch ex As Exception

                        End Try

                        Try
                            cs_out_pid = contextswitchinfo.SwitchOut.Process.Id
                        Catch ex As Exception

                        End Try

                        Try
                            cs_out_pri = contextswitchinfo.SwitchOut.Priority
                        Catch ex As Exception

                        End Try

                        Try
                            cs_out_tid = contextswitchinfo.SwitchOut.ThreadId
                        Catch ex As Exception

                        End Try

                        Try
                            cs_out_quantum = contextswitchinfo.SwitchOut.Quantum
                        Catch ex As Exception

                        End Try

                        Try
                            cs_out_qos = contextswitchinfo.SwitchOut.ThreadQualityOfService
                        Catch ex As Exception

                        End Try

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        wr.WriteLine(cs_cpu.ToString() + "," + cs_in_pid.ToString() + "," + cs_in_tid.ToString() + "," + cs_in_pri.ToString() + "," + cs_in_wait.ToString() + "," + cs_in_quantum.ToString() + "," + cs_in_qos.ToString() + "," + cs_out_pid.ToString() + "," + cs_out_tid.ToString() + "," + cs_out_pri.ToString() + "," + cs_out_quantum.ToString() + "," + cs_out_qos.ToString())
                        wr.Flush()

                        count += 1

                        cs_cpu = -1
                        cs_in_pid = -1
                        cs_in_pri = -1
                        cs_in_tid = -1
                        cs_in_wait = -1
                        cs_in_quantum = -1
                        cs_in_qos = -1

                        cs_out_pid = -1
                        cs_out_pri = -1
                        cs_out_tid = -1
                        cs_out_quantum = -1
                        cs_out_qos = -1

                    Next


                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    'ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub CpuSample(ShowIdle As Boolean)
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("Process" + "," + "PID" + "," + "ParentPID" + "," + "TID" + "," + "Priority" + "," + "CPU" + "," + "User" + "," + "IsDPC" + "," + "IsISR" + "," + "Function" + "," + "Weight")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)

                Try
                    Console.WriteLine("Loading symbols...")
                    Dim pendingsymbolData As IPendingResult(Of ISymbolDataSource) = trace.UseSymbols
                    Dim pendingData As IPendingResult(Of ICpuSampleDataSource) = trace.UseCpuSamplingData

                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()

                    Dim symbolData As ISymbolDataSource = pendingsymbolData.Result
                    Dim traceData As ICpuSampleDataSource = pendingData.Result

                    symbolData.LoadSymbolsAsync(SymCachePath.Automatic, SymbolPath.Automatic).GetAwaiter.GetResult()

                    Dim count As Int32 = 1
                    Dim iserror As Boolean = False

                    'Dim pattern As IThreadStackPattern = AnalyzerThreadStackPattern.Parse("{imageName}!{functionName}")
                    Dim pname As String = ""
                    Dim pid As String = ""
                    Dim p_pid As String = ""
                    Dim user As String = ""
                    Dim pri As String = ""
                    Dim dpc As String = ""
                    Dim isr As String = ""
                    Dim proc As String = ""
                    Dim tid As String = ""
                    Dim funct As String = ""
                    Dim weight As String = ""

                    For Each cpuinfo As ICpuSample In traceData.Samples

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        If cpuinfo.Process.ImageName IsNot Nothing Then
                            pname = cpuinfo.Process.ImageName.ToString()
                        End If

                        If cpuinfo.Process.Id.ToString() IsNot Nothing Then
                            pid = cpuinfo.Process.Id.ToString()
                        End If

                        If cpuinfo.Process.ParentId.ToString() IsNot Nothing Then
                            p_pid = cpuinfo.Process.ParentId.ToString()
                        End If

                        If cpuinfo.Process.User.Value IsNot Nothing Then
                            user = cpuinfo.Process.User.Value
                        End If

                        If cpuinfo.Priority IsNot Nothing Then
                            pri = cpuinfo.Priority.ToString()
                        End If

                        If cpuinfo.IsExecutingDeferredProcedureCall IsNot Nothing Then
                            dpc = cpuinfo.IsExecutingDeferredProcedureCall.ToString()
                        End If

                        If cpuinfo.IsExecutingInterruptServicingRoutine IsNot Nothing Then
                            isr = cpuinfo.IsExecutingInterruptServicingRoutine.ToString()
                        End If

                        If cpuinfo.Processor.ToString() IsNot Nothing Then
                            proc = cpuinfo.Processor.ToString()
                        End If

                        If cpuinfo.Thread.Id.ToString() IsNot Nothing Then
                            tid = cpuinfo.Thread.Id.ToString()
                        End If

                        'If StackWalk is not captured skip
                        'TODO
                        Try
                            If cpuinfo.Stack.Frames IsNot Nothing Then
                                For Each f In cpuinfo.Stack.Frames
                                    If f.Symbol IsNot Nothing Then
                                        funct = funct + f.Symbol.FunctionName + "<"
                                    End If
                                Next
                            End If
                        Catch ex As Exception
                            'Console.WriteLine(vbLf + ex.Message + vbLf + "Frame symbol missing for " + cpuinfo.Process.ImageName)
                        End Try


                        weight = cpuinfo.Weight.TotalMicroseconds.ToString()

                        If (pname <> "Idle") Then
                            wr.WriteLine(pname + "," + pid + "," + p_pid + "," + tid + "," + pri + "," + proc + "," + user + "," + dpc + "," + isr + ",""" + funct + """," + weight)
                            wr.Flush()
                        Else
                            If (ShowIdle) Then
                                wr.WriteLine(pname + ",-1,-1,-1,-1,-1,N/A,false,false,N/A," + weight)
                                wr.Flush()
                            End If
                        End If

                        count += 1
                        pname = ""
                        pid = ""
                        p_pid = ""
                        user = ""
                        pri = ""
                        dpc = ""
                        isr = ""
                        proc = ""
                        tid = ""
                        funct = ""
                        weight = ""

                    Next

                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub MiniFilterSummary()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("Filter" + "," + "TotalDuration-ms" + "," + "Publisher" + "," + "ProductName" + "," + "Description" + "," + "Version" + "," + "Checksum")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingMinifilterData As IPendingResult(Of IMinifilterDataSource) = trace.UseMiniffilterDelayIntervalData
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim traceData As IMinifilterDataSource = pendingMinifilterData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim Filters As New DataTable
                    Dim filter As String = ""
                    Dim dur As Decimal = 0
                    Dim pub As String = ""
                    Dim product As String = ""
                    Dim ver As String = ""
                    Dim desc As String = ""
                    Dim md5 As String = ""

                    Filters.Columns.Add("filter", GetType(System.String))
                    Filters.Columns.Add("duration", GetType(System.Decimal))
                    Filters.Columns.Add("publisher", GetType(System.String))
                    Filters.Columns.Add("product", GetType(System.String))
                    Filters.Columns.Add("description", GetType(System.String))
                    Filters.Columns.Add("version", GetType(System.String))
                    Filters.Columns.Add("md5", GetType(System.String))

                    For Each mfinfo As IMinifilterDelayInterval In traceData.DelayIntervals

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            filter = mfinfo.FilterImage.FileName.ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            dur = mfinfo.Duration.TotalMilliseconds
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            pub = mfinfo.FilterImage.CompanyName.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            product = mfinfo.FilterImage.ProductName.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            ver = mfinfo.FilterImage.FileVersion.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            desc = mfinfo.FilterImage.FileDescription.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            md5 = mfinfo.FilterImage.Checksum.ToString()
                        Catch ex As Exception

                        End Try

                        'Only add data if there was a filter and duration. Other fields can be empty.
                        If Not iserror Then
                            Filters.Rows.Add(filter, dur, pub, product, desc, ver, md5)
                        End If

                        filter = ""
                        dur = 0
                        product = ""
                        ver = ""
                        desc = ""
                        md5 = ""
                    Next

                    Dim distinctFilters As DataTable = Filters.DefaultView.ToTable(True, "filter", "publisher", "product", "description", "version", "md5")

                    For Each row As DataRow In distinctFilters.Rows
                        Dim totaldur As Decimal = 0

                        For Each row2 As DataRow In Filters.Rows
                            If row("filter") = row2("filter") Then
                                totaldur = totaldur + row2("duration")
                            End If
                        Next

                        wr.WriteLine(row("filter").ToString().Replace("""", "") + "," + totaldur.ToString() + "," + row("publisher").ToString().Replace("""", "") + "," + row("product").ToString().Replace("""", "") + "," + row("description").ToString().Replace("""", "") + "," + row("version").ToString().Replace("""", "") + "," + row("md5").ToString().Replace("""", ""))
                        wr.Flush()

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub MiniFilter()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "FilterName" + """,""" + "FilePath" + """,""" + "ProcessID" + """,""" + "ThreadID" + """,""" + "Duration" + """,""" + "StartTime" + """,""" + "StopTime" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingMinifilterData As IPendingResult(Of IMinifilterDataSource) = trace.UseMiniffilterDelayIntervalData
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim traceData As IMinifilterDataSource = pendingMinifilterData.Result
                    Dim count As Int32 = 1
                    Dim iserror As Boolean = False
                    Dim filepath As String = ""
                    Dim pid As Int32 = -1
                    Dim tid As Int32 = -1
                    Dim dur As Int64 = -1
                    Dim filter As String = ""
                    Dim starttime As Int64 = -1
                    Dim stoptime As Int64 = -1

                    Debug.WriteLine(traceData.DelayIntervals.Count)

                    Console.WriteLine("Saving all minifilter events >= " + milli.ToString + "ms")

                    For Each mfinfo As IMinifilterDelayInterval In traceData.DelayIntervals

                        Console.CursorTop = 3
                        Console.CursorLeft = 0
                        Console.Write("Events Processed: " + count.ToString)

                        Try
                            dur = mfinfo.Duration.TotalMicroseconds
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If dur >= milli Then
                            Try
                                filepath = mfinfo.FilePath.ToString()
                            Catch ex As Exception

                            End Try

                            Try
                                pid = mfinfo.Process.Id.ToString()
                            Catch ex As Exception

                            End Try

                            Try
                                tid = mfinfo.Thread.Id.ToString()
                            Catch ex As Exception

                            End Try

                            Try
                                starttime = mfinfo.StartTime.TotalMicroseconds
                            Catch ex As Exception

                            End Try

                            Try
                                stoptime = mfinfo.StopTime.TotalMicroseconds
                            Catch ex As Exception

                            End Try

                            Try
                                filter = mfinfo.FilterImage.FileName
                            Catch ex As Exception
                                iserror = True
                            End Try

                            If iserror = False Then
                                count += 1
                                wr.WriteLine("""" + filter + """,""" + filepath + """,""" + pid.ToString() + """,""" + tid.ToString() + """,""" + dur.ToString() + """,""" + starttime.ToString() + """,""" + stoptime.ToString() + """")
                                wr.Flush()
                            End If

                        End If

                        iserror = False
                        filepath = ""
                        pid = -1
                        tid = -1
                        dur = -1
                        filter = ""
                        starttime = -1
                        stoptime = -1

                    Next

                    Console.WriteLine(vbLf + "Percent of events >= " + milli.ToString + "ms: " + (count / traceData.DelayIntervals.Count * 100).ToString)

                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(vbLf + ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub ProviderInfo()
        Console.WriteLine(outputfile)
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "ProviderType" + """,""" + "ProviderID" + """,""" + "ProviderName" + """,""" + "DataSize" + """,""" + "EventSize" + """,""" + "HeaderSize" + """,""" + "ExtendedSize" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Dim pendingTraceData As IPendingResult(Of ITraceStatisticsDataSource) = trace.UseTraceStatistics
                Console.WriteLine("Processing file: " + filename)
                trace.Process()
                Dim traceData As ITraceStatisticsDataSource = pendingTraceData.Result
                Dim count As Int32 = 0
                Dim iserror As Boolean = False
                Dim errormsg As String = ""

                Debug.WriteLine(traceData.ClassicProviders.Count)
                Debug.WriteLine(traceData.ManifestedProviders.Count)
                Debug.WriteLine(traceData.TraceLoggingProviders.Count)
                Debug.WriteLine(traceData.TraceMessageProviders.Count)

                Try
                    For Each tinfo As IClassicProviderStatistics In traceData.ClassicProviders
                        Debug.WriteLine(tinfo.EventCount)
                        wr.WriteLine("""" + "Classic" + """,""" + tinfo.Id.ToString() + """,""" + tinfo.Name + """,""" + tinfo.TotalDataSize.Bytes.ToString() + """,""" + tinfo.TotalEventSize.Bytes.ToString() + """,""" + tinfo.TotalHeaderSize.Bytes.ToString() + """,""" + tinfo.TotalExtendedDataSize.Bytes.ToString() + """")
                        wr.Flush()
                    Next
                Catch ex As Exception
                    Debug.WriteLine(ex.Message)
                    errormsg = ex.Message
                    iserror = True
                End Try

                Try
                    For Each tinfo As IManifestedProviderStatistics In traceData.ManifestedProviders
                        Debug.WriteLine(tinfo.EventCount)
                        wr.WriteLine("""" + "Manifest" + """,""" + tinfo.Id.ToString() + """,""" + tinfo.Name + """,""" + tinfo.TotalDataSize.Bytes.ToString() + """,""" + tinfo.TotalEventSize.Bytes.ToString() + """,""" + tinfo.TotalHeaderSize.Bytes.ToString() + """,""" + tinfo.TotalExtendedDataSize.Bytes.ToString() + """")
                        wr.Flush()
                    Next
                Catch ex As Exception
                    Debug.WriteLine(ex.Message)
                    errormsg = ex.Message
                    iserror = True
                End Try

                Try
                    For Each tinfo As ITraceLoggingProviderStatistics In traceData.TraceLoggingProviders
                        Debug.WriteLine(tinfo.EventCount)
                        wr.WriteLine("""" + "TraceLogging" + """,""" + tinfo.Id.ToString() + """,""" + tinfo.Name + """,""" + tinfo.TotalDataSize.Bytes.ToString() + """,""" + tinfo.TotalEventSize.Bytes.ToString() + """,""" + tinfo.TotalHeaderSize.Bytes.ToString() + """,""" + tinfo.TotalExtendedDataSize.Bytes.ToString() + """")
                        wr.Flush()
                    Next
                Catch ex As Exception
                    Debug.WriteLine(ex.Message)
                    errormsg = ex.Message
                    iserror = True
                End Try

                Try
                    For Each tinfo As ITraceMessageProviderStatistics In traceData.TraceMessageProviders
                        Debug.WriteLine(tinfo.EventCount)
                        wr.WriteLine("""" + "TraceMessage" + """,""" + tinfo.Id.ToString() + """,""" + tinfo.Name + """,""" + tinfo.TotalDataSize.Bytes.ToString() + """,""" + tinfo.TotalEventSize.Bytes.ToString() + """,""" + tinfo.TotalHeaderSize.Bytes.ToString() + """,""" + tinfo.TotalExtendedDataSize.Bytes.ToString() + """")
                        wr.Flush()
                    Next
                Catch ex As Exception
                    Debug.WriteLine(ex.Message)
                    errormsg = ex.Message
                    iserror = True
                End Try

                If iserror = False Then

                Else
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(errormsg)
                    ShowHelp()
                End If

            End Using
        End Using
    End Sub

    Private Sub FileIO()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "EventType" + """,""" + "Path" + """,""" + "FileDuration-ms" + """,""" + "ErrorCode" + """,""" + "ProcessName" + """,""" + "ProcessID" + """,""" + "Thread" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Dim pendingFileData As IPendingResult(Of IFileActivityDataSource) = trace.UseFileIOData()
                Console.WriteLine("Processing file: " + filename)
                trace.Process()
                Dim fileioData As IFileActivityDataSource = pendingFileData.Result
                Dim count As Int32 = 0
                Dim iserror As Boolean = False
                Dim eventtype = "Read"
                Dim path = ""
                Dim fileduration = -1
                Dim errorcode = -1
                Dim fileprocess = ""
                Dim filepid = -1
                Dim filethread = -1

                Debug.WriteLine(fileioData.ReadFileActivity.Count)
                Debug.WriteLine(fileioData.WriteFileActivity.Count)
                Debug.WriteLine(fileioData.FlushFileActivity.Count)

                Try
                    For Each fileio As IFileActivity In fileioData.ReadFileActivity

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            path = fileio.Path.Trim("""")
                        Catch ex As Exception

                        End Try

                        Try
                            fileduration = (fileio.StopTime.DateTimeOffset - fileio.StartTime.DateTimeOffset).TotalMilliseconds
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            fileprocess = fileio.IssuingProcess.CommandLine.Trim("""")
                        Catch ex As Exception

                        End Try

                        Try
                            errorcode = fileio.ErrorCode
                        Catch ex As Exception

                        End Try

                        Try
                            filepid = fileio.IssuingProcess.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            filethread = fileio.IssuingThread.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + eventtype.ToString() + """,""" + path.ToString() + """,""" + fileduration.ToString() + """,""" + errorcode.ToString() + """,""" + fileprocess.ToString() + """,""" + filepid.ToString() + """,""" + filethread.ToString() + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        path = ""
                        fileduration = -1
                        errorcode = -1
                        fileprocess = ""
                        filepid = -1
                        filethread = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

                Try
                    For Each fileio As IFileActivity In fileioData.WriteFileActivity
                        eventtype = "Write"

                        Try
                            path = fileio.Path.Trim("""")
                        Catch ex As Exception

                        End Try

                        Try
                            fileduration = (fileio.StopTime.DateTimeOffset - fileio.StartTime.DateTimeOffset).TotalMilliseconds
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            fileprocess = fileio.IssuingProcess.CommandLine.Trim("""")
                        Catch ex As Exception

                        End Try

                        Try
                            errorcode = fileio.ErrorCode
                        Catch ex As Exception

                        End Try

                        Try
                            filepid = fileio.IssuingProcess.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            filethread = fileio.IssuingThread.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + eventtype.ToString() + """,""" + path.ToString() + """,""" + fileduration.ToString() + """,""" + errorcode.ToString() + """,""" + fileprocess.ToString() + """,""" + filepid.ToString() + """,""" + filethread.ToString() + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        path = ""
                        fileduration = -1
                        errorcode = -1
                        fileprocess = ""
                        filepid = -1
                        filethread = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

                Try
                    For Each fileio As IFileActivity In fileioData.FlushFileActivity
                        eventtype = "Flush"

                        Try
                            path = fileio.Path.Trim("""")
                        Catch ex As Exception

                        End Try

                        Try
                            fileduration = (fileio.StopTime.DateTimeOffset - fileio.StartTime.DateTimeOffset).TotalMilliseconds
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            fileprocess = fileio.IssuingProcess.CommandLine.Trim("""")
                        Catch ex As Exception

                        End Try

                        Try
                            errorcode = fileio.ErrorCode
                        Catch ex As Exception

                        End Try

                        Try
                            filepid = fileio.IssuingProcess.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            filethread = fileio.IssuingThread.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + eventtype.ToString() + """,""" + path.ToString() + """,""" + fileduration.ToString() + """,""" + errorcode.ToString() + """,""" + fileprocess.ToString() + """,""" + filepid.ToString() + """,""" + filethread.ToString() + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        path = ""
                        fileduration = -1
                        errorcode = -1
                        fileprocess = ""
                        filepid = -1
                        filethread = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub DiskIOSummary()

        'Will use milliseconds for all durations instead of microseconds for better latency understanding in report
        'If fields are null, 0, -1 or an error occurs the event will be skipped to preserve best possible aggregate inforation at the expense of missing data

        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Disk" + """,""" + "IOType" + """,""" + "Aggregate" + """,""" + "Min" + """,""" + "Max" + """,""" + "Avg" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingDiskData As IPendingResult(Of IDiskActivityDataSource) = trace.UseDiskIOData()
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim diskioData As IDiskActivityDataSource = pendingDiskData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim disknum As Int16 = -1
                    Dim iotype As Int16 = -1
                    Dim iopriority As Int16 = -1
                    Dim iosize As Int64 = -1
                    Dim ioduration As Decimal = -1
                    Dim disksvcduration As Decimal = -1
                    Dim qdepthinit As Int32 = -1
                    Dim qdepthcomplete As Int32 = -1
                    Dim diskbandwidth As Decimal = -1
                    Dim stackbandwidth As Decimal = -1
                    Dim filterduration As Decimal = -1
                    Dim Filters As New DataTable

                    Filters.Columns.Add("disk", GetType(System.String))
                    Filters.Columns.Add("iotype", GetType(System.String))
                    Filters.Columns.Add("iopriority", GetType(System.String))
                    Filters.Columns.Add("iosize", GetType(System.Int64))
                    Filters.Columns.Add("ioduration", GetType(System.Int64))
                    Filters.Columns.Add("disksvcduration", GetType(System.Int64))
                    Filters.Columns.Add("qdepthinit", GetType(System.Int32))
                    Filters.Columns.Add("qdepthcomplete", GetType(System.Int32))
                    Filters.Columns.Add("diskbandwidth", GetType(System.Int64))
                    Filters.Columns.Add("stackbandwidth", GetType(System.Int64))
                    Filters.Columns.Add("filterduration", GetType(System.Int64))

                    Debug.WriteLine(diskioData.Activity.Count)

                    'Fill datatable with the data
                    For Each diskio As IDiskActivity In diskioData.Activity

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            disknum = diskio.Disk
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> diskio.IOType Then
                                Select Case diskio.IOType
                                    Case -1
                                        iotype = "unknown"
                                    Case 0
                                        iotype = "read"
                                    Case 1
                                        iotype = "write"
                                    Case 2
                                        iotype = "flush"
                                End Select
                            End If
                        Catch ex As Exception
                            iotype = "unknown"
                        End Try

                        Try
                            If Nothing <> diskio.Priority Then
                                Select Case diskio.Priority
                                    Case 0
                                        iopriority = "verylow"
                                    Case 1
                                        iopriority = "low"
                                    Case 2
                                        iopriority = "normal"
                                    Case 3
                                        iopriority = "high"
                                    Case 4
                                        iopriority = "critical"
                                End Select
                            End If
                        Catch ex As Exception
                            iopriority = "unknown"
                        End Try



                        If iserror = False Then
                            Filters.Rows.Add(disknum, iotype, iopriority, iosize, ioduration, disksvcduration, qdepthinit, qdepthcomplete, diskbandwidth, stackbandwidth, filterduration)
                        End If

                        count += 1
                        iserror = False
                        disknum = -1
                        iotype = -1
                        iopriority = -1
                        iosize = -1
                        ioduration = -1
                        disksvcduration = -1
                        qdepthinit = -1
                        qdepthcomplete = -1
                        diskbandwidth = -1
                        stackbandwidth = -1
                        filterduration = -1
                    Next

                    'Process each aggregate, iopriority, iotype and disk
                    'Datatable provides distinct column values??? Would be easier
                    'Aggregates: Size, Disk Duration, Disk Stack Duration, Filter and Que Duration, Disk Bandwidth Bytes, Disk Stack Bandwidth Bytes, Que Depth Init, Que Depth Complete



                Catch ex As Exception

                End Try
            End Using
        End Using
    End Sub

    Private Sub DiskIO()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Disk" + """,""" + "Path" + """,""" + "DiskDuration-us" + """,""" + "DiskStackDuration-us" + """,""" + "FilterandQueDuration-us" + """,""" + "IOSizeBytes" + """,""" + "DiskBandwidthBytes-Calculated" + """,""" + "DiskStackBandwidthBytes-Calculated" + """,""" + "IOType" + """,""" + "ProcessID" + """,""" + "ThreadID" + """,""" + "IOPriority" + """,""" + "QueueDepthInit" + """,""" + "QueueDepthComplete" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingDiskData As IPendingResult(Of IDiskActivityDataSource) = trace.UseDiskIOData()
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim diskioData As IDiskActivityDataSource = pendingDiskData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim disknum As Int16 = -1
                    Dim path As String = ""
                    Dim ioduration As Int64 = -1
                    Dim iosize As Int64 = -1
                    Dim disksvcduration As Int64 = -1
                    Dim iotype As Int16 = -1
                    Dim ioprocess As Int32 = -1
                    Dim iothread As Int32 = -1
                    Dim iopriority As Int16 = -1
                    Dim qdepthinit As Int32 = -1
                    Dim qdepthcomplete As Int32 = -1
                    Dim diskbandwidth As Int64 = -1
                    Dim stackbandwidth As Int64 = -1
                    Dim filterduration As Int64 = -1

                    Debug.WriteLine(diskioData.Activity.Count)

                    For Each diskio As IDiskActivity In diskioData.Activity

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            disknum = diskio.Disk
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> diskio.Path Then
                                path = diskio.Path
                            End If
                        Catch ex As Exception

                        End Try

                        Try
                            ioduration = diskio.IODuration.TotalMicroseconds
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> diskio.Size Then
                                iosize = diskio.Size.Bytes
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If Nothing <> diskio.DiskServiceDuration Then
                            disksvcduration = diskio.DiskServiceDuration.TotalMicroseconds
                        End If

                        Try
                            'If Nothing <> diskio.IOType Then 'Misses disk reads does 0 = Nothing? Yes, it does seem so.
                            iotype = diskio.IOType
                            'End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> diskio.IssuingProcess.Id Then
                                ioprocess = diskio.IssuingProcess.Id
                            End If
                        Catch ex As Exception

                        End Try

                        Try
                            If Nothing <> diskio.IssuingThread.Id Then
                                iothread = diskio.IssuingThread.Id
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> diskio.Priority Then
                                iopriority = diskio.Priority
                            End If
                        Catch ex As Exception

                        End Try

                        Try
                            qdepthinit = diskio.QueueDepthAtInitializeTime
                        Catch ex As Exception

                        End Try

                        Try
                            qdepthcomplete = diskio.QueueDepthAtCompleteTime
                        Catch ex As Exception

                        End Try

                        If iosize <> 0 And iosize <> -1 And ioduration <> 0 And ioduration <> -1 Then
                            stackbandwidth = ((1000000 / ioduration) * iosize)
                        Else
                            stackbandwidth = 0
                        End If

                        If iosize <> 0 And iosize <> -1 And disksvcduration <> 0 And disksvcduration <> -1 Then
                            diskbandwidth = ((1000000 / disksvcduration) * iosize)
                        Else
                            diskbandwidth = 0
                        End If

                        If disksvcduration <> -1 And ioduration <> -1 Then
                            filterduration = ioduration - disksvcduration
                        End If

                        If iserror = False Then
                            wr.WriteLine("""" + disknum.ToString() + """,""" + path.ToString() + """,""" + disksvcduration.ToString() + """,""" + ioduration.ToString() + """,""" + filterduration.ToString() + """,""" + iosize.ToString() + """,""" + diskbandwidth.ToString() + """,""" + stackbandwidth.ToString() + """,""" + iotype.ToString() + """,""" + ioprocess.ToString() + """,""" + iothread.ToString() + """,""" + iopriority.ToString() + """,""" + qdepthinit.ToString() + """,""" + qdepthcomplete.ToString() + """")
                            wr.Flush()

                        Else

                        End If

                        count += 1
                        iserror = False
                        disknum = -1
                        path = ""
                        ioduration = -1
                        disksvcduration = -1
                        iosize = -1
                        iotype = -1
                        ioprocess = -1
                        iothread = -1
                        iopriority = -1
                        qdepthinit = -1
                        qdepthcomplete = -1
                        diskbandwidth = -1
                        stackbandwidth = -1
                        filterduration = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try
            End Using
        End Using
    End Sub

    Private Sub HardFaults()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Process" + """,""" + "ProcessPID" + """,""" + "Thread" + """,""" + "Path" + """,""" + "AvgIOTime-ms" + """,""" + "ByteCount" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingHardFaultData As IPendingResult(Of IHardFaultDataSource) = trace.UseHardFaults()
                    trace.Process()
                    Dim hardfaultData As IHardFaultDataSource = pendingHardFaultData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim process As String = ""
                    Dim processpid As Int32 = -1
                    Dim thread As Int32 = -1
                    Dim path As String = ""
                    Dim avgiotime As Int32 = -1
                    Dim bytecount As Int32 = -1

                    Debug.WriteLine(hardfaultData.Faults.Count)

                    For Each fault As IHardFault In hardfaultData.Faults

                        Try
                            If Nothing <> fault.FaultingProcess.CommandLine Then
                                process = fault.FaultingProcess.CommandLine.Trim("""")
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> fault.FaultingProcess.Id Then
                                processpid = fault.FaultingProcess.Id
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> fault.FaultingThread.Id Then
                                thread = fault.FaultingThread.Id
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> fault.Path Then
                                path = fault.Path
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> fault.IODuration.TotalMilliseconds Then
                                avgiotime = fault.IODuration.TotalMilliseconds
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            If Nothing <> fault.Size.Bytes Then
                                bytecount = fault.Size.Bytes
                            End If
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + process.ToString() + """,""" + processpid.ToString() + """,""" + thread.ToString() + """,""" + path + """,""" + avgiotime.ToString() + """,""" + bytecount.ToString() + """")
                            wr.Flush()

                        Else

                        End If

                        count += 1
                        iserror = False
                        process = ""
                        processpid = -1
                        thread = -1
                        path = ""
                        avgiotime = -1
                        bytecount = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub Services()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Service" + """,""" + "StartType" + """,""" + "Image" + """,""" + "PID" + """,""" + "State" + """,""" + "Time" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    'Oops, I have no idea where I got this provider GUID from. It was the original and no trace I open has it. Must be classic provider, no name.
                    'Dim provids = New Guid() {New Guid("b8ddcea7-b520-4909-bceb-e0170c9f0e99")}

                    'Microsoft-Windows-Services Provider GUID {0063715b-eeda-4007-9429-ad526f62696e}
                    'Dim provids = New Guid() {New Guid("0063715b-eeda-4007-9429-ad526f62696e")}
                    'Dim pendingSvcStatesData As IPendingResult(Of IGenericEventDataSource) = trace.UseGenericEvents(provids)

                    Dim pendingSvcStatesData As IPendingResult(Of IServiceDataSource) = trace.UseServices
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim SvcStateData As IServiceDataSource = pendingSvcStatesData.Result
                    Dim count As Int32 = 1
                    Dim iserror As Boolean = False
                    Dim name As String = ""
                    Dim starttype As String = ""
                    Dim imagename As String = ""
                    Dim pid As String = ""
                    Dim state As String = ""
                    Dim starttime As Decimal = -1
                    Dim Filters As New DataTable

                    Filters.Columns.Add("svcname", GetType(System.String))
                    Filters.Columns.Add("starttype", GetType(System.String))
                    Filters.Columns.Add("imagename", GetType(System.String))
                    Filters.Columns.Add("pid", GetType(System.String))
                    Filters.Columns.Add("state", GetType(System.String))
                    Filters.Columns.Add("starttime", GetType(System.Decimal))

                    For Each svcstate As IService In SvcStateData.Services

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            name = svcstate.Name
                        Catch ex As Exception

                        End Try

                        Try
                            starttype = svcstate.StartType.ToString
                        Catch ex As Exception

                        End Try

                        For Each change In svcstate.StateChanges
                            Try
                                imagename = change.ImageName.ToString
                            Catch ex As Exception

                            End Try

                            Try
                                pid = change.ProcessId.ToString
                            Catch ex As Exception

                            End Try

                            Try
                                state = change.State.ToString
                            Catch ex As Exception

                            End Try

                            Try
                                starttime = change.Timestamp.TotalMilliseconds
                            Catch ex As Exception

                            End Try

                            If Not iserror Then
                                Filters.Rows.Add(name, starttype, imagename, pid, state, starttime)
                            End If

                            imagename = ""
                            pid = ""
                            state = ""
                            starttime = -1
                        Next

                        count += 1
                        iserror = False
                        name = ""
                        starttype = ""

                    Next

                    For Each row As DataRow In Filters.Rows
                        wr.WriteLine("""" + row("svcname") + """,""" + row("starttype") + """,""" + row("imagename") + """,""" + row("pid") + """,""" + row("State") + """,""" + row("starttime").ToString + """")
                        wr.Flush()
                    Next

                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try
            End Using
        End Using
    End Sub

    Private Sub PnP()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Provider" + """,""" + "Timestamp" + """,""" + "Field1" + """,""" + "Message" + """,""" + "Task" + """,""" + "Opcode" + """,""" + "CPU" + """,""" + "PID" + """,""" + "Thread" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim provids = New Guid() {New Guid("9c205a39-1250-487d-abd7-e831c6290539")}
                    Dim pendingPnPData As IPendingResult(Of IGenericEventDataSource) = trace.UseGenericEvents(provids)
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim PnPData As IGenericEventDataSource = pendingPnPData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim provider As String = ""
                    'Dim id As int32 = 0
                    Dim timestamp As String = ""
                    Dim field1 As String = ""
                    Dim message As String = ""
                    Dim task As String = ""
                    Dim opcode As String = ""
                    Dim cpu As Int32 = -1
                    Dim pid As Int32 = -1
                    Dim thread As Int32 = -1

                    For Each pnp As IGenericEvent In PnPData.Events

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            provider = pnp.ProviderName
                        Catch ex As Exception

                        End Try

                        'Try
                        '    If pnp.Id = Nothing Then

                        '    Else
                        '        id = pnp.Id.ToString()
                        '    End If
                        'Catch ex As Exception

                        'End Try

                        Try
                            timestamp = pnp.Timestamp.TotalSeconds.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            field1 = pnp.Fields.Keys(0).ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            message = pnp.MessageTemplate.ToString().Replace(vbCrLf, "")
                        Catch ex As Exception

                        End Try

                        Try
                            task = pnp.TaskName.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            opcode = pnp.OpcodeName.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            cpu = pnp.Processor
                        Catch ex As Exception

                        End Try

                        Try
                            pid = pnp.ProcessId
                        Catch ex As Exception

                        End Try

                        Try
                            thread = pnp.ThreadId
                        Catch ex As Exception

                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + provider + """,""" + timestamp + """,""" + field1 + """,""" + message + """,""" + task + """,""" + opcode + """,""" + cpu.ToString() + """,""" + pid.ToString() + """,""" + thread.ToString() + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        provider = ""
                        'id = -1
                        timestamp = ""
                        field1 = ""
                        message = ""
                        opcode = ""
                        task = ""
                        cpu = -1
                        pid = -1
                        thread = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub Tasks()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "TaskName" + """,""" + "ExitCode" + """,""" + "Start" + """,""" + "End" + """,""" + "TriggerStart" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingTaskData As IPendingResult(Of IScheduledTaskDataSource) = trace.UseScheduledTasks()
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim taskData As IScheduledTaskDataSource = pendingTaskData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim name As String = ""
                    Dim startsec As Decimal = 0
                    Dim endsec As Decimal = -1
                    Dim exitcode As Int16 = -1
                    Dim starttrigger As Decimal = -1

                    For Each task As IScheduledTask In taskData.Tasks

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            name = task.FullName
                        Catch ex As Exception
                            name = ""
                        End Try

                        Try
                            startsec = task.StartTime.Value.TotalSeconds
                        Catch ex As Exception
                            startsec = 0 'Task start not available in Microsoft-Windows-TaskScheduler, maybe in the future?
                        End Try

                        Try
                            starttrigger = task.TriggerTime.Value.TotalSeconds
                        Catch ex As Exception
                            starttrigger = -1 'Task start not available in Microsoft-Windows-TaskScheduler, maybe in the future?
                        End Try

                        Try
                            endsec = task.StopTime.Value.TotalSeconds
                        Catch ex As Exception
                            endsec = -1 'Task ended not available in Microsoft-Windows-TaskScheduler, maybe in the future?
                        End Try

                        Try
                            exitcode = task.ExitCode.Value
                        Catch ex As Exception
                            exitcode = -1 'Exit codes not available in Microsoft-Windows-TaskScheduler, maybe in the future?
                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + name + """,""" + exitcode.ToString() + """,""" + startsec.ToString() + """,""" + endsec.ToString() + """,""" + starttrigger.ToString() + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        name = ""
                        startsec = 0
                        endsec = -1
                        exitcode = -1
                        starttrigger = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub Winlogon()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Provider" + """,""" + "Timestamp" + """,""" + "Task" + """,""" + "Message" + """,""" + "CPU" + """,""" + "PID" + """,""" + "Thread" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim provids = New Guid() {New Guid("dbe9b383-7cf3-4331-91cc-a3cb16a3b538")}
                    Dim pendingWinlogonData As IPendingResult(Of IGenericEventDataSource) = trace.UseGenericEvents(provids)
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim WinlogonData As IGenericEventDataSource = pendingWinlogonData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim provider As String = ""
                    'Dim id As String = ""
                    Dim timestamp As String = ""
                    Dim task As String = ""
                    Dim message As String = ""
                    Dim cpu As String = ""
                    Dim pid As String = ""
                    Dim thread As String = ""
                    'Dim value As Microsoft.Windows.EventTracing.Events.IGenericEventField

                    For Each logon As IGenericEvent In WinlogonData.Events

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            provider = logon.ProviderName
                        Catch ex As Exception
                            iserror = True
                        End Try

                        'Try
                        '    id = logon.Id.ToString()
                        'Catch ex As Exception

                        'End Try

                        Try
                            timestamp = logon.Timestamp.TotalSeconds.ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            task = logon.TaskName.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            message = logon.MessageTemplate.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            cpu = logon.Processor.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            pid = logon.ProcessId.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            thread = logon.ThreadId.ToString()
                        Catch ex As Exception

                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + provider + """,""" + timestamp + """,""" + task + """,""" + message.Replace(vbCrLf, "") + """,""" + cpu + """,""" + pid + """,""" + thread + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        provider = ""
                        'id = ""
                        timestamp = ""
                        task = ""
                        message = ""
                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub Processes()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "ProcessName" + """,""" + "ProcessPID" + """,""" + "ParentPID" + """,""" + "Start" + """,""" + "End" + """,""" + "Duration" + """,""" + "SessionID" + """,""" + "UserSID" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim pendingProcessData As IPendingResult(Of IProcessDataSource) = trace.UseProcesses()
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim processData As IProcessDataSource = pendingProcessData.Result
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim imagename As String = ""
                    Dim id As Int32 = -1
                    Dim parentid As Int32 = -1
                    Dim starttime As Decimal = 0
                    Dim endtime As Decimal = -1
                    Dim sessionid As Int32 = -1
                    Dim user As String = ""

                    For Each process As IProcess In processData.Processes

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            imagename = process.ImageName.ToString()
                        Catch ex As Exception

                        End Try

                        Try
                            id = process.Id
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            parentid = process.ParentId
                        Catch ex As Exception

                        End Try

                        Try
                            starttime = process.CreateTime.Value.TotalSeconds
                        Catch ex As Exception

                        End Try

                        Try
                            endtime = process.ExitTime.Value.TotalSeconds
                        Catch ex As Exception

                        End Try

                        Try
                            sessionid = process.SessionId
                        Catch ex As Exception

                        End Try

                        Try
                            user = process.User.Value
                        Catch ex As Exception

                        End Try

                        If iserror = False Then 'Only error if there is no PID
                            wr.WriteLine("""" + process.ImageName.ToString() + """,""" + process.Id.ToString() + """,""" + process.ParentId.ToString() + """,""" + starttime.ToString() + """,""" + endtime.ToString() + """,""" + sessionid.ToString() + """,""" + user + """")
                            wr.Flush()

                        Else

                        End If

                        count += 1
                        iserror = False
                        imagename = ""
                        id = -1
                        parentid = -1
                        starttime = 0
                        endtime = -1

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub GPOS()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "Provider" + """,""" + "Timestamp" + """,""" + "Field1" + """,""" + "Value1" + """,""" + "Message" + """,""" + "CPU" + """,""" + "PID" + """,""" + "Thread" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim provids = New Guid() {New Guid("aea1b4fa-97d1-45f2-a64c-4d69fffd92c9")}
                    Dim pendingGPOData As IPendingResult(Of IGenericEventDataSource) = trace.UseGenericEvents(provids)
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim GPOData As IGenericEventDataSource = pendingGPOData.Result
                    Dim value As Microsoft.Windows.EventTracing.Events.IGenericEventField
                    Dim count As Int32 = 0
                    Dim iserror As Boolean = False
                    Dim provider As String = ""
                    Dim timestamp As String = ""
                    Dim field1 As String = ""
                    Dim value1 As String = ""
                    Dim message As String = ""
                    Dim cpu As String = ""
                    Dim pid As String = ""
                    Dim thread As String = ""


                    For Each gpo As IGenericEvent In GPOData.Events

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        Try
                            provider = gpo.ProviderName
                        Catch ex As Exception
                            iserror = True
                        End Try

                        'Dim id As String = ""
                        'Try
                        '    id = gpo.Id.ToString()
                        'Catch ex As Exception
                        '    iserror = True
                        'End Try

                        Try
                            timestamp = gpo.Timestamp.TotalSeconds.ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            field1 = gpo.Fields.Keys(0).ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            value = gpo.Fields.Item(0)
                            value1 = value.AsObject().ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            message = gpo.MessageTemplate.ToString().Replace(vbCrLf, "")
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            cpu = gpo.Processor.ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            pid = gpo.ProcessId.ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        Try
                            thread = gpo.ThreadId.ToString()
                        Catch ex As Exception
                            iserror = True
                        End Try

                        If iserror = False Then
                            wr.WriteLine("""" + provider + """,""" + timestamp + """,""" + field1 + """,""" + value1 + """,""" + message + """,""" + cpu + """,""" + pid + """,""" + thread + """")
                            wr.Flush()
                        Else

                        End If

                        count += 1
                        iserror = False
                        provider = ""
                        timestamp = ""
                        field1 = ""
                        value1 = ""
                        message = ""
                        cpu = ""
                        pid = ""
                        thread = ""

                    Next
                Catch ex As Exception
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(ex.Message)
                    ShowHelp()
                End Try

            End Using
        End Using
    End Sub

    Private Sub Marks()
        ' Create TraceEventSource for reading the ETL file
        Dim source As New ETWTraceEventSource(filename)

        Dim parser As New KernelTraceEventParser(source)

        ' Register callback for PerfInfoMark events
        AddHandler parser.PerfInfoMark, AddressOf OnPerfInfoMarkEvent

        ' Process all events in the file
        source.Process()

    End Sub

    Private Sub OnPerfInfoMarkEvent(ByVal data As Microsoft.Diagnostics.Tracing.TraceEvent)
        ' Extract and display relevant information
        'Console.WriteLine($"Timestamp: {data.TimeStamp}")
        'Console.WriteLine($"Value: {data.ToString}")
        'Console.WriteLine($"ProcessName: {data.ProcessName}")
        'Console.WriteLine("----")

        If (data.ToString.ToLower.Contains("boot") Or data.ToString.ToLower.Contains("on/off")) Then
            Console.WriteLine("Boot trace")
            isBootTrace = True
        End If

    End Sub

    Private Sub BootPhases()

        Marks()

        ' Check to see if the trace is a boot trace
        If isBootTrace = True Then
            Using wr As New StreamWriter(outputfile, True)
                wr.WriteLine("""" + "BootPhase" + """,""" + "Start" + """,""" + "End" + """,""" + "Duration" + """")
                Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                    Try

                        Dim pendingProcessData As IPendingResult(Of IProcessDataSource) = trace.UseProcesses()
                        Console.WriteLine("Processing file: " + filename)
                        trace.Process()
                        Dim processData As IProcessDataSource = pendingProcessData.Result
                        Dim count As Int32 = 1
                        Dim iserror As Boolean = False
                        Dim phase As String = ""
                        Dim starttime As Decimal = 0
                        Dim endtime As Decimal = -1

                        Dim colcsrss As New Collection()
                        Dim colsmss As New Collection()
                        Dim colexplorer As New Collection()
                        Dim collogonui As New Collection()

                        For Each process As IProcess In processData.Processes

                            If measureruntime Then
                                Console.CursorTop = 3
                                Console.CursorLeft = 0
                                Console.Write("Events Processed: " + count.ToString)
                            End If

                            Dim objcsrss As New BootPhase()
                            Dim objsmss As New BootPhase()
                            Dim objexplorer As New BootPhase()
                            Dim objlogonui As New BootPhase()

                            Select Case process.ImageName.ToString().ToLower()
                                Case "csrss.exe"
                                    Try
                                        starttime = process.CreateTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    Try
                                        endtime = process.ExitTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    objcsrss.ProcessName = process.ImageName.ToString().ToLower()
                                    objcsrss.StartTime = starttime
                                    objcsrss.EndTime = endtime

                                    colcsrss.Add(objcsrss)

                                Case "smss.exe"
                                    Try
                                        starttime = process.CreateTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    Try
                                        endtime = process.ExitTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    objsmss.ProcessName = process.ImageName.ToString().ToLower()
                                    objsmss.StartTime = starttime
                                    objsmss.EndTime = endtime

                                    colsmss.Add(objsmss)

                                Case "explorer.exe"
                                    Try
                                        starttime = process.CreateTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    Try
                                        endtime = process.ExitTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    objexplorer.ProcessName = process.ImageName.ToString().ToLower()
                                    objexplorer.StartTime = starttime
                                    objexplorer.EndTime = endtime

                                    colexplorer.Add(objexplorer)

                                Case "logonui.exe"
                                    Try
                                        starttime = process.CreateTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    Try
                                        endtime = process.ExitTime.Value.TotalSeconds
                                    Catch ex As Exception

                                    End Try

                                    objlogonui.ProcessName = process.ImageName.ToString().ToLower()
                                    objlogonui.StartTime = starttime
                                    objlogonui.EndTime = endtime

                                    collogonui.Add(objlogonui)

                            End Select

                            count += 1
                            iserror = False

                        Next


                        Dim lstcsrss As New List(Of BootPhase)(
                            colcsrss.
                            OfType(Of BootPhase).
                            OrderBy(Function(l As BootPhase) l.StartTime))

                        Dim lstsmss As New List(Of BootPhase)(
                            colsmss.
                            OfType(Of BootPhase).
                            OrderBy(Function(l As BootPhase) l.StartTime))

                        Dim lstexplorer As New List(Of BootPhase)(
                            colexplorer.
                            OfType(Of BootPhase).
                            OrderBy(Function(l As BootPhase) l.StartTime))

                        Dim lstlogonui As New List(Of BootPhase)(
                            collogonui.
                            OfType(Of BootPhase).
                            OrderBy(Function(l As BootPhase) l.StartTime))

                        For Each l In lstsmss
                            'wr.WriteLine("""" + l.ProcessName + """,""" + l.StartTime.ToString + """,""" + l.EndTime.ToString + """")
                            'wr.Flush()
                        Next

                        For Each l In lstcsrss
                            'wr.WriteLine("""" + l.ProcessName + """,""" + l.StartTime.ToString + """,""" + l.EndTime.ToString + """")
                            'wr.Flush()
                        Next

                        For Each l In lstexplorer
                            'wr.WriteLine("""" + l.ProcessName + """,""" + l.StartTime.ToString + """,""" + l.EndTime.ToString + """")
                            'wr.Flush()
                        Next

                        For Each l In lstlogonui
                            'wr.WriteLine("""" + l.ProcessName + """,""" + l.StartTime.ToString + """,""" + l.EndTime.ToString + """")
                            'wr.Flush()
                        Next

                        'Pre-Session
                        Dim pre As String = lstsmss(0).StartTime.ToString
                        Dim predur As Double = pre - 0
                        wr.WriteLine("""" + "Pre-Session-Init" + """,""" + 0.ToString + """,""" + pre + """,""" + predur.ToString + """")
                        wr.Flush()

                        'Session
                        Dim sess As String = lstcsrss(1).StartTime.ToString
                        Dim sessdur As Double = sess - pre
                        wr.WriteLine("""" + "Session-Init" + """,""" + pre + """,""" + sess + """,""" + sessdur.ToString + """")
                        wr.Flush()

                        'WinLogon
                        Dim winlog As String = lstexplorer(0).StartTime.ToString
                        Dim winlogdur As Double = winlog - sess
                        wr.WriteLine("""" + "WinLogon" + """,""" + sess + """,""" + winlog + """,""" + winlogdur.ToString + """")
                        wr.Flush()

                        'Explorer
                        Dim exp As String = lstlogonui.Last.EndTime.ToString
                        Dim expdur As Double = exp - winlog
                        wr.WriteLine("""" + "Explorer-Init" + """,""" + winlog + """,""" + exp + """,""" + expdur.ToString + """")
                        wr.Flush()

                    Catch ex As Exception
                        Console.ForegroundColor = ConsoleColor.Red
                        Console.WriteLine(ex.Message)
                        ShowHelp()
                    End Try
                End Using
            End Using
        Else
            Console.WriteLine("This file does not look to be a boot trace. Make sure there is a PerfInfoMark event with word 'Boot' in the trace..")
            Console.WriteLine("A mark can be added to a trace using wpr.exe before saving and merging the running trace.")
            Console.WriteLine("wpr.exe -marker ""This is a boot trace.""")
        End If


    End Sub

    Private Sub ProcessZombies()
        Using wr As New StreamWriter(outputfile, True)
            wr.WriteLine("""" + "BootPhase" + """,""" + "Start" + """,""" + "End" + """,""" + "Duration" + """")
            Using trace As ITraceProcessor = TraceProcessor.Create(filename, tracesettings)
                Try
                    Dim count As Int32 = 1
                    Dim pendingProcessData As IPendingResult(Of IProcessDataSource) = trace.UseProcesses()
                    Console.WriteLine("Processing file: " + filename)
                    trace.Process()
                    Dim processData As IProcessDataSource = pendingProcessData.Result

                    For Each process As IProcess In processData.Processes

                        If measureruntime Then
                            Console.CursorTop = 3
                            Console.CursorLeft = 0
                            Console.Write("Events Processed: " + count.ToString)
                        End If

                        'Console.WriteLine(process.)
                        count += 1
                    Next

                Catch

                End Try
            End Using
        End Using
    End Sub

    Private Sub ShowHelp()
        Console.ResetColor()
        Console.WriteLine("")
        Console.WriteLine("Program will open and read a .etl trace and produce .csv formatted output to console based on the --processor: selected.")
        Console.WriteLine("")
        Console.WriteLine("Accepted arguments:")
        Console.WriteLine("h | -h | /h | help | -help | /help | ? | -? | /? Shows this help screen")
        Console.WriteLine("--infile:<ETLFILENAME> (REQUIRED)")
        Console.WriteLine("--processor:[processes tasks gpos winlogon pnp services hardfaults diskio fileio providerinfo minifilter minifiltersummary cpusample cpusamplenoidle bootphases processzombies] (REQUIRED)")
        Console.WriteLine("--ms:<MILLISECONDS> - Used with minifilter processor to specify how many milliseconds above to save events")
        Console.WriteLine("--outfolder:<.CSV Report OUTPUTFOLDER> (REQUIRED)")
        Console.WriteLine("--measure - Show start time, end time and count of events being processed in console")
        Console.WriteLine("")
        Console.WriteLine("Examples:")
        Console.WriteLine("ETLReports.exe --infile:c:\trace.etl --processor:processes --outfolder:c:\")
        Console.WriteLine("ETLReports.exe --infile:c:\trace.etl --processor:diskio --outfolder:c:\")
        Console.WriteLine("ETLReports.exe --infile:'c:\trace with space in name.etl' --processor:cpusample --outfolder:'c:\'")
        Console.WriteLine("ETLReports.exe --infile:c:\trace.etl --processor:minifilter --ms:15 --measure --outfolder:c:\")
        Console.WriteLine("")
        Console.WriteLine("* Only 1 <processor> can be specified each run. Run multiple times for more reports.")
        End
    End Sub

End Module
