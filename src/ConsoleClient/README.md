// =====================================================
// README.md - Console Client Usage
// =====================================================
# 365Scores Console Client

## Running the Client

### Local Development
```bash
cd 365Scores.ConsoleClient
dotnet run
```

### With Docker
```bash
docker-compose up console-client
```

### With Custom NotificationService URL
```bash
dotnet run http://notification-service:5003
```

## Features

1. **Real-time Notifications**: Receive live updates via SignalR
2. **Subscription Management**: Subscribe to teams, players, leagues, and matches
3. **API Queries**: Query the DataAPI for live match information
4. **Automatic Reconnection**: Handles connection drops gracefully

## Example Usage

### Subscribe to a match:
- Choose option 4
- Enter match ID (1-20)
- You'll receive all events for that match

### Subscribe to a team:
- Choose option 1
- Enter team ID (odd numbers 1-39 for home teams, even 2-40 for away teams)
- You'll receive all events involving that team

### Query live matches:
- Choose option 8
- View all currently live matches from the DataAPI

## Event Types

- ⚽ GOAL - Goal scored
- 🟨 Card - Yellow/Red card
- 🔄 Substitution - Player substitution
- 🎯 Shot - Shot on/off target
- 🚩 Corner - Corner kick
- ⏸️ Half Time - End of first half
- 🏁 Full Time - Match finished
