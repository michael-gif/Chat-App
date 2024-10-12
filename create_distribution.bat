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

REM Copy files
xcopy "%CLIENT_SRC%\*" "%CLIENT_DST%\" /E /I /Y
xcopy "%SERVER_SRC%\*" "%SERVER_DST%\" /E /I /Y

REM Delete .pdb files
del /q "%CLIENT_DST%\*.pdb"
del /q "%SERVER_DST%\*.pdb"
echo Delete .pdb files

REM Zip Client and Server folders using WinRAR
echo Zipping...
cd Distribution
"..\resources\WinRAR.exe" a -r ChatApp.zip "Client\*" "Server\*"
echo Created zip file

REM Cleanup
if exist Client rmdir /s /q Client
if exist Server rmdir /s /q Server

echo Distribution located at Distribution\ChatApp.zip
pause
