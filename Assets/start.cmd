@echo off
start %~dp0\nns.exe
timeout 15
echo Opening Browser
rem You need to change the port below if you changed the port in the settings file
start http://localhost:5000