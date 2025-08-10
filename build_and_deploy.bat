@echo off
echo ========================================
echo GravshiptoSpaceship 模组编译和部署脚本
echo ========================================
echo.

REM 设置路径变量
set "PROJECT_PATH=c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\csproj\GravshiptoSpaceship.csproj"
set "BUILD_OUTPUT_PATH=c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\csproj\bin\Release\net472"
set "MOD_ASSEMBLIES_PATH=c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\Assemblies"

echo 正在编译模组...
dotnet build "%PROJECT_PATH%" --configuration Release

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ 编译失败！请检查错误信息。
    pause
    exit /b 1
)

echo.
echo ✅ 编译成功！
echo.

echo 正在复制文件到模组目录...

REM 复制 DLL 文件
copy "%BUILD_OUTPUT_PATH%\GravshiptoSpaceship.dll" "%MOD_ASSEMBLIES_PATH%\" /Y
if %ERRORLEVEL% neq 0 (
    echo ❌ 复制 DLL 文件失败！
    pause
    exit /b 1
)

REM 复制 PDB 文件（调试符号）
copy "%BUILD_OUTPUT_PATH%\GravshiptoSpaceship.pdb" "%MOD_ASSEMBLIES_PATH%\" /Y
if %ERRORLEVEL% neq 0 (
    echo ❌ 复制 PDB 文件失败！
    pause
    exit /b 1
)

echo.
echo ✅ 所有文件已成功复制到模组目录！
echo.
echo 📁 模组路径: %MOD_ASSEMBLIES_PATH%
echo 🎮 现在可以启动 RimWorld 测试模组了！
echo.
echo ========================================
echo 编译和部署完成！
echo ========================================
pause