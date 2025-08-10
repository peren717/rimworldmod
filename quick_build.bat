@echo off
echo ========================================
echo 🚀 GravshiptoSpaceship 快速编译脚本
echo ========================================
echo.

cd /d "c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\csproj"

echo 📦 正在编译模组...
dotnet build GravshiptoSpaceship.csproj --configuration Release --verbosity quiet

if %ERRORLEVEL% equ 0 (
    echo ✅ 编译成功！
    echo.
    echo 📁 正在复制文件到模组目录...
    copy "bin\Release\net472\GravshiptoSpaceship.dll" "..\Assemblies\" /Y >nul
    copy "bin\Release\net472\GravshiptoSpaceship.pdb" "..\Assemblies\" /Y >nul
    echo ✅ 文件复制完成！
    echo.
    echo 🎮 模组已准备就绪，可以启动 RimWorld 测试！
    echo 📂 模组位置: ..\Assemblies\
) else (
    echo ❌ 编译失败！请检查代码错误。
)

echo.
echo ========================================
pause