@echo off

REM Define the source and destination directories
set "CLIENT_SRC=Chat App Client\bin\Release\net6.0-windows"
set "SERVER_SRC=Chat App Server\bin\Release\net6.0"
set "CLIENT_DST=Distribution\Client"
set "SERVER_DST=Distribution\Server"

REM Delete the bin\Distribution directory if it exists
if exist Distribution rmdir /s /q Distribution

REM Create the destination directories if they don't exist
mkdir "%CLIENT_DST%"
mkdir "%SERVER_DST%"

REM Copy Client files
copy "%CLIENT_SRC%\client.dll" "%CLIENT_DST%"
copy "%CLIENT_SRC%\client.exe" "%CLIENT_DST%"
copy "%CLIENT_SRC%\client.runtimeconfig.json" "%CLIENT_DST%"

REM Copy Server files
copy "%SERVER_SRC%\server.dll" "%SERVER_DST%"
copy "%SERVER_SRC%\server.exe" "%SERVER_DST%"
copy "%SERVER_SRC%\server.runtimeconfig.json" "%SERVER_DST%"

REM Zip Client and Server folders using WinRAR
cd Distribution
"..\resources\WinRAR.exe" a -r ChatApp.zip "Client\*" "Server\*"

REM Cleanup
if exist Client rmdir /s /q Client
if exist Server rmdir /s /q Server

echo Distribution located at Distribution\ChatApp.zip
pause
