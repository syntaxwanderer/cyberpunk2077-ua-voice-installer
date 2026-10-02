@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set REFS=/r:System.Windows.Forms.dll /r:System.Drawing.dll
%CSC% /nologo /codepage:65001 /optimize+ /platform:anycpu /target:winexe /out:UA_Voice_Setup.exe %REFS% Setup.cs Program.cs
if errorlevel 1 exit /b 1
%CSC% /nologo /codepage:65001 /optimize+ /platform:anycpu /target:exe /out:UA_Voice_Setup_cli.exe %REFS% Setup.cs Program.cs
if errorlevel 1 exit /b 1
echo Build OK
