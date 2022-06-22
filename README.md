# ETLReports

Produces various reports, usually in .csv format from Windows Event .etl log files.

Accepted arguments:

h | -h | /h | help | -help | /help | ? | -? | /? Shows this help screen

--infile:<ETLFILENAME>

--processor:[processes tasks gpos winlogon pnp servicestates hardfaults diskio fileio providerinfo minifilter1ms minifiltersummary cpusample cpusamplenoidle 
bootphases processzombies]

--outfile:<.CSV OUTPUTFILENAME>

Example:

ETLReports.exe --infile:c:\trace.etl --processor:processes --outfile:c:\trace_processes.csv

ETLReports.exe --infile:c:\trace.etl --processor:diskio --outfile:c:\trace_diskio.csv

ETLReports.exe --infile:'c:\trace with space in name.etl' --processor:cpusample --outfile:'c:\trace cpusample.csv'

* Only 1 <REPORTTYPE> can be specified each run. Run multiple times for more reports.
