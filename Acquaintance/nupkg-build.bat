C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild Acquaintance.csproj /t:Build /p:Configuration="Release"
if ERRORLEVEL 1 GOTO :error
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild Acquaintance.csproj /t:Build /p:Configuration="Release 4.5"
if ERRORLEVEL 1 GOTO :error
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild Acquaintance.csproj /t:Package
if ERRORLEVEL 1 GOTO :error
goto :done

:error
echo Build FAILED

:done