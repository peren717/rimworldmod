@echo off
echo ========================================
echo GravshiptoSpaceship Build Script
echo ========================================
echo.

cd /d "c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\csproj"

echo Building mod...
dotnet build GravshiptoSpaceship.csproj --configuration Release --verbosity quiet

if %ERRORLEVEL% equ 0 (
    echo Build successful!
    echo.
    echo Copying files to mod directory...
    copy "bin\Release\net472\GravshiptoSpaceship.dll" "..\Assemblies\" /Y >nul
    copy "bin\Release\net472\GravshiptoSpaceship.pdb" "..\Assemblies\" /Y >nul
    echo Files copied successfully!
    echo.
    echo Mod is ready for testing!
    echo Location: ..\Assemblies\
) else (
    echo Build failed! Please check for errors.
)

echo.
echo ========================================
pause