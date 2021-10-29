@echo off
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
	"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" build.proj
) ELSE (
	"%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe" build.proj
)
pause