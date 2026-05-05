
@echo off
cd /d "%~dp0"

if exist "..\..\artifacts\FileCleanup\win-x64" (
    rmdir /s /q "..\..\artifacts\FileCleanup\win-x64"
)

dotnet publish .\FileCleanup.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -o ..\..\artifacts\FileCleanup\win-x64

pause