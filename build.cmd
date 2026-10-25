@echo off
rem 编译 💊性能调控（无需安装任何 SDK，使用系统自带 .NET Framework 编译器）
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

"%CSC%" /nologo /codepage:65001 /target:winexe /win32icon:app.ico /out:HonorTune.exe HonorTune.cs /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll
if errorlevel 1 (
  echo.
  echo 编译失败。
  pause
  exit /b 1
)
echo.
echo 编译成功: HonorTune.exe
pause
