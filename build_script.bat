@echo off
cd /d E:\DGodotProjectsTestCSharp
"C:\Program Files\dotnet\dotnet.exe" build SanguoshaMod.csproj -c Release --no-restore > C:\Users\Administrator\build_output.txt 2>&1
echo EXITCODE=%ERRORLEVEL% >> C:\Users\Administrator\build_output.txt
