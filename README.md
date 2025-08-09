# Gravship New Game Plus

🚀 A RimWorld mod that enables converting gravships into fully functional spaceships with a unique "New Game Plus" scenario.

**基于原作者辻堂的mod修改而来**: https://steamcommunity.com/sharedfiles/filedetails/?id=3531942788

**Based on the original mod by 辻堂**: https://steamcommunity.com/sharedfiles/filedetails/?id=3531942788

## Features

- 🛸 **Gravship Conversion**: Transform gravships into complete spaceships for space travel
- 🎮 **New Game Plus Scenario**: Special "Gravship New Game+" starting scenario with solo drop pod start
- 💾 **Save & Restore**: Save and restore gravship configurations between games
- 🌌 **Orbital Scenarios**: Custom map generation for orbital and space scenarios

## Installation

1. Subscribe to this mod on Steam Workshop or download manually
2. Place in your RimWorld Mods folder: `\RimWorld\Mods\GravshiptoSpaceship\`
3. Enable in the mod list before starting a new game
4. Select "Gravship New Game+" scenario when creating a new game

## Requirements

- **RimWorld 1.6+**
- **Harmony**

## 📖 Usage Guide

### How to Use New Game Plus

1. **🚀 Launch Your Spaceship**
   - Complete your spaceship construction in your current game
   - Launch the spaceship to trigger the ending sequence

2. **⚙️ Configure New Game Plus**
   - Go to **Options** → **Mod Settings** → **Gravship New Game Plus**
   - Select the spaceship you want to load in your New Game Plus

3. **🎮 Start New Game Plus**
   - Create a new game and select **"Gravship New Game+"** scenario
   - Begin your new adventure with your previous spaceship!

### 📋 Step-by-Step Instructions

**发射飞船后的操作步骤：**

1. **发射飞船** - 在当前游戏中完成飞船建造并发射
2. **进入模组设置** - 主菜单 → 选项 → 模组设置 → Gravship New Game Plus
3. **选择飞船** - 在模组选项中选择要在New Game Plus中加载的飞船
4. **开始新游戏** - 创建新游戏时选择"Gravship New Game+"场景
5. **享受游戏** - 使用你之前的飞船开始新的冒险！

## Known Issues

⚠️ **Please be aware of these current limitations:**

### 1. 🎬 **End Game Credits Issue**
- **Problem**: The escape pod survivor list appears empty in the end game credits scroll
- **Impact**: Visual only, does not affect gameplay

### 2. 👥 **Scenario Pawn Disappearance**
- **Problem**: Pawns from the scenario may mysteriously disappear when starting New Game Plus
- **Workaround**: Start a new game

### 3. 🌫️ **Environmental Generation on Ships**
- **Problem**: Fog, geothermal vents, or other terrain features may generate on spaceship tiles
- **Impact**: Can interfere with ship functionality
- **Workaround**: Use dev mode to remove unwanted terrain features

### 4. 🏛️ **Ideology Inheritance Issues**
- **Problem**: Ideologies cannot be fully inherited from previous saves
- **Workaround**: 
  1. Save your ideology in the old save file first
  2. Load the saved ideology in New Game Plus scenario

## Development

This mod is built using .NET Framework 4.7.2. To build from source:

1. Open the project in Visual Studio or use `dotnet build`
2. The compiled DLL will be placed in the Assemblies folder automatically
3. Ensure all language files are properly formatted in UTF-8

### Project Structure
```
GravshiptoSpaceship/
├── About/
│   └── About.xml
├── Assemblies/
├── Defs/
│   └── ScenarioDefs/
├── Languages/
│   ├── ChineseSimplified (简体中文)/
│   ├── English/
│   └── Japanese/
├── Patches/
└── README.md
```

## Contributing

Feel free to report issues or contribute improvements:
- 🐛 **Bug Reports**: Please include your log files and mod list
- 🌍 **Translations**: Additional language support welcome
- 💡 **Feature Requests**: Open an issue with detailed description

## License

This mod is provided as-is for the RimWorld community under open source principles.