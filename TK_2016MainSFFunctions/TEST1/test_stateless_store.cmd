curl "http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessSet?partition=2&name=abc&value=3"
:start
curl "http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGet?partition=2&name=abc" >> log_writeread.txt
echo "" >> log_writeread.txt
goto start