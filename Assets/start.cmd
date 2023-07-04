@echo off
echo NABU NetSim needs to be run as an administrator.
runas /env /user:%USERNAME% %~dp0\nns.exe
timeout 15
echo Opening Browser
start http://localhost:5000