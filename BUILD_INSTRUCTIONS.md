# GravshiptoSpaceship 编译说明

## 可用的编译脚本

### 1. `build.bat` - 推荐使用
**最稳定的编译脚本，使用英文界面避免编码问题**
- 自动编译模组
- 自动复制文件到模组目录
- 显示详细的编译状态

使用方法：双击运行或在命令行中执行
```cmd
build.bat
```

### 2. `quick_build.bat` - 中文界面
**中文界面的快速编译脚本**
- 功能与 build.bat 相同
- 中文界面（可能在某些系统上有编码问题）

### 3. `build_and_deploy.bat` - 详细版本
**最详细的编译脚本**
- 包含完整的路径信息
- 详细的错误检查
- 适合调试使用

### 4. `dev_build.bat` - 开发者版本
**高级开发者脚本，包含多种编译选项**
- Release 编译
- Debug 编译
- 清理并重新编译
- 仅复制现有文件

## 手动编译命令

如果需要手动编译，可以使用以下命令：

```cmd
cd "c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\GravshiptoSpaceship\csproj"
dotnet build GravshiptoSpaceship.csproj --configuration Release
copy "bin\Release\net472\GravshiptoSpaceship.dll" "..\Assemblies\" /Y
copy "bin\Release\net472\GravshiptoSpaceship.pdb" "..\Assemblies\" /Y
```

## 文件说明

- `GravshiptoSpaceship.dll` - 主要的模组程序集
- `GravshiptoSpaceship.pdb` - 调试符号文件（用于错误调试）

## 故障排除

1. **编译失败**：检查代码语法错误
2. **文件复制失败**：确保 RimWorld 没有在运行
3. **编码问题**：使用 `build.bat`（英文版本）

## 测试流程

1. 运行编译脚本
2. 启动 RimWorld
3. 在模组管理器中启用 GravshiptoSpaceship
4. 创建新游戏或加载存档进行测试

---
*最后更新：2025年1月*