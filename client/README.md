# 🎯 SPT Leaderboard Mod

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)](https://dotnet.microsoft.com/)
[![SPT](https://img.shields.io/badge/SPT-4.0+-green)](https://www.sp-tarkov.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

**An advanced mod for SPT (Single Player Tarkov) that tracks player statistics and displays them on an online leaderboard.**


## ✨ Features

### 📊 **Statistics Tracking**
- **Automatic data collection**: Raid results, kills, damage, play time
- **Detailed statistics**: PMC/SCAV levels, win streaks, kill distances
- **Item tracking**: Equipment, looting, trading
- **Raid history**: Complete history of your gaming sessions

### 🏆 **Achievement System**
- **Personal achievements**: Best results, records
- **Battle Pass progress**: Experience and level tracking
- **Leaderboards**: Compare results with other players

<!-- ### 🗺️ **Zone System**
- **Hierarchical zones**: Support for main zones and sub-zones
- **Time tracking**: Time spent in each zone
- **Visualization**: Display zones in-game with color differentiation
- **Zone editor**: Tools for creating and configuring zones -->

### 🌐 **Server Integration**
- **Online synchronization**: Automatic statistics upload
- **Error handling**: Smart retry system
- **Data encryption**: Personal information protection
- **Multilingual support**: Multiple language support

## 🚀 Installation

### 📋 **Requirements**
- **SPT 4.0**
- **.NET Framework 4.7.2**

### 📦 **Quick Installation**
1. **Download** the latest release from [Releases](https://github.com/your-repo/releases)
2. **Extract** the archive to your SPT root folder
3. **Launch** SPT server to generate your token
4. **Play** and enjoy the statistics!

### ⚙️ **File Locations**
```
📁 SPT/
├── 📁 BepInEx/
│   ├── 📁 plugins/
│   │   └── 📁 SPT-Leaderboard/
│   │       ├── SPTLeaderboard.dll
│   │       └── [other mod files]
│   └── 📁 config/
│       └── SPT-Leaderboard.cfg  # Mod configuration
```

### 📊 **Statistics Viewing**
- **Website profile**: Visit the leaderboard website for detailed statistics
- **Raid history**: View all your gaming sessions
- **Comparison**: Compare results with other players

## 🏗️ **For Developers**

### 📂 **Project Structure**
```
📁 Client/
├── 📁 Configuration/     # Application settings
├── 📁 Services/         # Business logic
│   ├── EncryptionService.cs
│   ├── LocalizationService.cs
│   ├── NetworkApiRequest.cs
│   └── ProcessProfileService.cs
├── 📁 Data/            # Data models
│   ├── Base/           # Base models
│   ├── Response/       # API responses
│   └── Internal/       # Internal models
├── 📁 Utils/           # Utilities
│   ├── Zones/          # Zone system
│   └── [other utilities]
├── 📁 Patches/         # Harmony patches
└── 📁 Enums/           # Enumerations
```

### 🛠️ **Building the Project**

#### **Build Requirements:**
- **Visual Studio 2022** or **dotnet CLI**
- **SPT paths** configured in `SPTLeaderboard.csproj`

#### **Build Commands:**
```bash
# Debug version
dotnet build SPTLeaderboard.csproj --configuration Debug

# Release version
dotnet build SPTLeaderboard.csproj --configuration Release

# Beta version
dotnet build SPTLeaderboard.csproj --configuration Beta
```

#### **Environment Variables (Optional):**
```bash
# Override default SPT paths (defaults are set in .csproj)
TarkovDir=C:\Games\SPT\
TarkovDevDir=C:\Games\SPTDEV\
```

> **Note**: Default paths are already configured in `SPTLeaderboard.csproj`. Use environment variables only if you need custom paths.

### 📞 **Support**
- **Issues**: [GitHub Issues](https://github.com/SPT-Leaderboard/Client/issues)
- **Discord**: [Join our Discord server](https://discord.gg/psV2PY8brW)
- **Logs**: Press LEFT CTRL+LEFT SHIFT+D+SPACE in Main menu for detailed logs

## 📄 **License**

This project is distributed under the **MIT** license. Details in the [LICENSE](LICENSE) file.

---

**🎯 Enjoy the game and track your progress!**