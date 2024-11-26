# Server Activity Monitor Bot

A Telegram bot for monitoring server resources and system status, built with .NET 8.0.

## Features

- CPU usage monitoring
- Memory usage monitoring
- Real-time system status updates
- Simple command interface

## Commands

- `/start` - Show welcome message and available commands
- `/status` - Display current server status (CPU and memory usage)

## Requirements

- .NET 8.0 SDK
- Windows OS (for PerformanceCounter functionality)
- Telegram Bot Token

## Setup

1. Clone the repository
2. Update the bot token in `Program.cs`
3. Build and run:
```bash
dotnet restore
dotnet build
dotnet run
```

## Dependencies

- Telegram.Bot (19.0.0)
- System.Diagnostics.PerformanceCounter (8.0.0)
- Microsoft.VisualBasic (10.3.0)

## Future Improvements

- Disk space monitoring
- Process list monitoring
- System uptime information
- Network statistics
- Resource usage alerts
