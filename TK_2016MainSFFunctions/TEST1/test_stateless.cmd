:start
curl "http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGetInfo?partition=1" >> log.txt
echo "" >> log.txt
curl "http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGetInfo?partition=2" >> log.txt
echo "" >> log.txt
goto start