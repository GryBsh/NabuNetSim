@echo off
echo Starting NABU NetSim Web Server
start nns-wui.exe
timeout 15
echo Opening Browser
start http://localhost:5000