dotnet build Acquaintance.csproj --configuration Release
if ERRORLEVEL 1 GOTO :error

dotnet pack Acquaintance.csproj --configuration Release --no-build
if ERRORLEVEL 1 GOTO :error

goto :done

:error
echo Build FAILED

:done