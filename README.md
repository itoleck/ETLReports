# ETLReports

Produces various reports, usually in .csv format from Windows Event .etl log files.

Program will open and read a .etl trace and produce .csv formatted output to console based on the --processor: selected.

Accepted arguments:

h | -h | /h | help | -help | /help | ? | -? | /? Shows this help screen

--infile:<ETLFILENAME> (REQUIRED)

--processor:[processes tasks gpos winlogon pnp services hardfaults diskio fileio providerinfo minifilter minifiltersummary cpusample cpusamplenoidle bootphases processzombies] (REQUIRED)

--ms:<MILLISECONDS> - Used with minifilter processor to specify how many milliseconds above to save events

--outfolder:<.CSV Report OUTPUTFOLDER> (REQUIRED)

--measure - Show start time, end time and count of events being processed in console

## Examples

ETLReports.exe --infile:c:\trace.etl --processor:processes --outfolder:c:\

ETLReports.exe --infile:c:\trace.etl --processor:diskio --outfolder:c:\

ETLReports.exe --infile:'c:\trace with space in name.etl' --processor:cpusample --outfolder:'c:\'

ETLReports.exe --infile:c:\trace.etl --processor:minifilter --ms:15 --measure --outfolder:c:\

* Only 1 <processor> can be specified each run. Run multiple times for more reports.
