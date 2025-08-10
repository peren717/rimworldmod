@echo off
setlocal enabledelayedexpansion

echo ========================================
echo 🛠️  GravshiptoSpaceship 开发者编译脚本
echo ========================================
echo.

REM 设置路径变量
set "PROJECT_DIR=c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\csproj"
set "PROJECT_FILE=%PROJECT_DIR%\GravshiptoSpaceship.csproj"
set "BUILD_OUTPUT=%PROJECT_DIR%\bin\Release\net472"
set "MOD_ASSEMBLIES=c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\Assemblies"

echo 选择编译选项:
echo 1. 快速编译 (Release)
echo 2. 调试编译 (Debug)
echo 3. 清理并重新编译
echo 4. 仅复制现有文件
echo.
set /p choice="请输入选项 (1-4): "

cd /d "%PROJECT_DIR%"

if "%choice%"=="1" goto release_build
if "%choice%"=="2" goto debug_build
if "%choice%"=="3" goto clean_build
if "%choice%"=="4" goto copy_only
goto invalid_choice

:release_build
echo.
echo 📦 正在进行 Release 编译...
dotnet build "%PROJECT_FILE%" --configuration Release
set "BUILD_OUTPUT=%PROJECT_DIR%\bin\Release\net472"
goto copy_files

:debug_build
echo.
echo 🐛 正在进行 Debug 编译...
dotnet build "%PROJECT_FILE%" --configuration Debug
set "BUILD_OUTPUT=%PROJECT_DIR%\bin\Debug\net472"
goto copy_files

:clean_build
echo.
echo 🧹 正在清理项目...
dotnet clean "%PROJECT_FILE%"
echo 📦 正在重新编译...
dotnet build "%PROJECT_FILE%" --configuration Release
set "BUILD_OUTPUT=%PROJECT_DIR%\bin\Release\net472"
goto copy_files

:copy_only
echo.
echo 📁 仅复制现有文件...
set "BUILD_OUTPUT=%PROJECT_DIR%\bin\Release\net472"
goto copy_files

:copy_files
if %ERRORLEVEL% neq 0 (
    if not "%choice%"=="4" (
        echo ❌ 编译失败！
        goto end
    )
)

echo.
echo 📁 正在复制文件到模组目录...

if exist "%BUILD_OUTPUT%\GravshiptoSpaceship.dll" (
    copy "%BUILD_OUTPUT%\GravshiptoSpaceship.dll" "%MOD_ASSEMBLIES%\" /Y >nul
    echo ✅ DLL 文件已复制
) else (
    echo ❌ 找不到 DLL 文件: %BUILD_OUTPUT%\GravshiptoSpaceship.dll
)

if exist "%BUILD_OUTPUT%\GravshiptoSpaceship.pdb" (
    copy "%BUILD_OUTPUT%\GravshiptoSpaceship.pdb" "%MOD_ASSEMBLIES%\" /Y >nul
    echo ✅ PDB 文件已复制
) else (
    echo ⚠️  找不到 PDB 文件 (调试符号)
)

echo.
echo 📊 文件信息:
dir "%MOD_ASSEMBLIES%\GravshiptoSpaceship.*" /T:W

echo.
echo 🎮 模组已准备就绪！
echo 📂 模组路径: %MOD_ASSEMBLIES%
goto end

:invalid_choice
echo ❌ 无效选项！

:end
echo.
echo ========================================
pause