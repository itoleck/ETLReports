# ETLReports
 
Produces various reports, usually in .csv format from Windows Event .etl log files.

Accepted arguments:

h | -h | /h | help | -help | /help | ? | -? | /? Shows this help screen

<ETLFILENAME>

<REPORTTYPE> [processes tasks gpos winlogon pnp servicestates hardfaults diskio fileio providerinfo minifilter1ms minifiltersummary cpusample cpusamplenoidle bootphases processzombies]

<.CSV OUTPUTFILENAME>

Example:

ETLReports.exe c:\trace.etl processes c:\trace_processes.csv

ETLReports.exe c:\trace.etl diskio c:\trace_diskio.csv

* Only 1 <REPORTTYPE> can be specified each run. Run multiple times for more reports.

