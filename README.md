# Gravship to Spaceship

一个 RimWorld 模组，允许玩家将重力船转换为太空船。

## 功能特性

- 重力船到太空船的转换功能
- 零小人开局场景支持
- 重力船数据的保存和恢复
- 与 Save Our Ship 模组的兼容性

## 安装方法

1. 将模组文件夹放置在 RimWorld 的 Mods 目录中
2. 在游戏中启用模组
3. 确保模组加载顺序正确

## 依赖模组

- Harmony
- Save Our Ship Simplified（推荐）

## 开发信息

### 编译

在 `csproj` 目录下运行：
```bash
dotnet build
```

### 项目结构

- `About/` - 模组信息和描述
- `Assemblies/` - 编译后的 DLL 文件
- `Defs/` - 游戏定义文件
- `Languages/` - 本地化文件
- `Patches/` - Harmony 补丁
- `csproj/` - C# 源代码

## 更新日志

### 最新版本
- 移除了意识形态相关功能以避免兼容性问题
- 优化了重力船数据保存和恢复逻辑
- 修复了零小人开局的相关问题

## 许可证

请遵循 RimWorld 模组开发的相关许可协议。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个模组。