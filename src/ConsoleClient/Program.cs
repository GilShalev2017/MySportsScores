//using Microsoft.AspNetCore.SignalR.Client;
//using System.Text;
//using System.Text.Json;

//Console.WriteLine("========================================");
//Console.WriteLine("   365Scores Real-Time Console Client");
//Console.WriteLine("========================================\n");

//var notificationServiceUrl = args.Length > 0 ? args[0] : "http://localhost:5002";
//var hubUrl = $"{notificationServiceUrl}/sportshub";

//Console.WriteLine($"Connecting to SignalR Hub: {hubUrl}");

//var connection = new HubConnectionBuilder()
//    .WithUrl(hubUrl)
//    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
//    .Build();

//// Setup event handlers
//SetupEventHandlers(connection);

//try
//{
//    await connection.StartAsync();
//    Console.WriteLine("✅ Connected to NotificationService\n");

//    // Display menu
//    await DisplayMenuAndHandleInput(connection);
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"❌ Error: {ex.Message}");
//}
//finally
//{
//    await connection.DisposeAsync();
//}

//static void SetupEventHandlers(HubConnection connection)
//{
//    Console.OutputEncoding = Encoding.UTF8;

//    connection.Reconnecting += error =>
//    {
//        Console.WriteLine($"⚠️ Connection lost. Reconnecting...");
//        return Task.CompletedTask;
//    };

//    connection.Reconnected += connectionId =>
//    {
//        Console.WriteLine($"✅ Reconnected. Connection ID: {connectionId}");
//        return Task.CompletedTask;
//    };

//    connection.Closed += error =>
//    {
//        Console.WriteLine($"❌ Connection closed. {error?.Message}");
//        return Task.CompletedTask;
//    };

//    // Score updates
//    connection.On<object>("ScoreUpdate", scoreUpdate =>
//    {
//        var json = JsonSerializer.Serialize(scoreUpdate, new JsonSerializerOptions { WriteIndented = true });
//        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

//        Console.ForegroundColor = ConsoleColor.Green;
//        Console.WriteLine($"\n⚽ SCORE UPDATE - Match {data["matchId"]}");
//        Console.WriteLine($"   Score: {data["homeScore"]} - {data["awayScore"]}");
//        Console.WriteLine($"   Minute: {data["minute"]}'");
//        Console.WriteLine($"   Status: {data["status"]}");
//        Console.ResetColor();
//    });

//    // Sport events
//    connection.On<object>("SportEvent", sportEvent =>
//    {
//        var json = JsonSerializer.Serialize(sportEvent, new JsonSerializerOptions { WriteIndented = true });
//        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

//        var eventType = data["eventType"].GetInt32();
//        var eventName = eventType switch
//        {
//            1 => "⏱️ Match Start",
//            2 => "⚽ GOAL",
//            3 => "🟨 Card",
//            4 => "🔄 Substitution",
//            5 => "⏸️ Half Time",
//            6 => "🏁 Full Time",
//            7 => "🎯 Assist",
//            8 => "🎯 Shot",
//            9 => "🚩 Corner",
//            10 => "🚫 Foul",
//            _ => "📌 Event"
//        };

//        Console.ForegroundColor = eventType == 2 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
//        Console.WriteLine($"\n{eventName} - Match {data["matchId"]}");
//        Console.WriteLine($"   {data["description"]}");
//        Console.WriteLine($"   Minute: {data["minute"]}'");
//        Console.ResetColor();
//    });

//    // Team events
//    connection.On<object>("TeamEvent", teamEvent =>
//    {
//        var json = JsonSerializer.Serialize(teamEvent, new JsonSerializerOptions { WriteIndented = true });
//        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

//        Console.ForegroundColor = ConsoleColor.Magenta;
//        Console.WriteLine($"\n🏆 TEAM EVENT - Team {data["teamId"]}");
//        Console.WriteLine($"   {data["description"]}");
//        Console.ResetColor();
//    });

//    // Player events
//    connection.On<object>("PlayerEvent", playerEvent =>
//    {
//        var json = JsonSerializer.Serialize(playerEvent, new JsonSerializerOptions { WriteIndented = true });
//        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

//        Console.ForegroundColor = ConsoleColor.Blue;
//        Console.WriteLine($"\n👤 PLAYER EVENT - Player {data["playerId"]}");
//        Console.WriteLine($"   {data["description"]}");
//        Console.ResetColor();
//    });

//    // Subscription confirmations
//    connection.On<object>("SubscriptionConfirmed", confirmation =>
//    {
//        var json = JsonSerializer.Serialize(confirmation, new JsonSerializerOptions { WriteIndented = true });
//        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

//        Console.ForegroundColor = ConsoleColor.Green;
//        Console.WriteLine($"✅ Subscribed to {data["type"]}: {data["id"]}");
//        Console.ResetColor();
//    });

//    // User preferences
//    connection.On<object>("UserPreferences", preferences =>
//    {
//        Console.ForegroundColor = ConsoleColor.White;
//        Console.WriteLine("\n📋 Your Current Preferences:");
//        var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
//        Console.WriteLine(json);
//        Console.ResetColor();
//    });
//}

//static async Task DisplayMenuAndHandleInput(HubConnection connection)
//{
//    var running = true;

//    while (running)
//    {
//        Console.WriteLine("\n========================================");
//        Console.WriteLine("Menu Options:");
//        Console.WriteLine("========================================");
//        Console.WriteLine("1. Subscribe to Team");
//        Console.WriteLine("2. Subscribe to Player");
//        Console.WriteLine("3. Subscribe to League");
//        Console.WriteLine("4. Subscribe to Match");
//        Console.WriteLine("5. Unsubscribe from Team");
//        Console.WriteLine("6. Unsubscribe from Player");
//        Console.WriteLine("7. View My Preferences");
//        Console.WriteLine("8. Query Live Matches (API)");
//        Console.WriteLine("9. Exit");
//        Console.Write("\nEnter choice (1-9): ");

//        var choice = Console.ReadLine();

//        try
//        {
//            switch (choice)
//            {
//                case "1":
//                    Console.Write("Enter Team ID: ");
//                    if (int.TryParse(Console.ReadLine(), out int teamId))
//                    {
//                        await connection.InvokeAsync("SubscribeToTeam", teamId);
//                        Console.WriteLine($"Subscribing to Team {teamId}...");
//                    }
//                    break;

//                case "2":
//                    Console.Write("Enter Player ID: ");
//                    if (int.TryParse(Console.ReadLine(), out int playerId))
//                    {
//                        await connection.InvokeAsync("SubscribeToPlayer", playerId);
//                        Console.WriteLine($"Subscribing to Player {playerId}...");
//                    }
//                    break;

//                case "3":
//                    Console.Write("Enter League ID: ");
//                    if (int.TryParse(Console.ReadLine(), out int leagueId))
//                    {
//                        await connection.InvokeAsync("SubscribeToLeague", leagueId);
//                        Console.WriteLine($"Subscribing to League {leagueId}...");
//                    }
//                    break;

//                case "4":
//                    Console.Write("Enter Match ID (1-20): ");
//                    if (int.TryParse(Console.ReadLine(), out int matchId))
//                    {
//                        await connection.InvokeAsync("SubscribeToMatch", matchId);
//                        Console.WriteLine($"Subscribing to Match {matchId}...");
//                    }
//                    break;

//                case "5":
//                    Console.Write("Enter Team ID to unsubscribe: ");
//                    if (int.TryParse(Console.ReadLine(), out int unsubTeamId))
//                    {
//                        await connection.InvokeAsync("UnsubscribeFromTeam", unsubTeamId);
//                        Console.WriteLine($"Unsubscribed from Team {unsubTeamId}");
//                    }
//                    break;

//                case "6":
//                    Console.Write("Enter Player ID to unsubscribe: ");
//                    if (int.TryParse(Console.ReadLine(), out int unsubPlayerId))
//                    {
//                        await connection.InvokeAsync("UnsubscribeFromPlayer", unsubPlayerId);
//                        Console.WriteLine($"Unsubscribed from Player {unsubPlayerId}");
//                    }
//                    break;

//                case "7":
//                    await connection.InvokeAsync("GetUserPreferences");
//                    break;

//                case "8":
//                    await QueryLiveMatches();
//                    break;

//                case "9":
//                    running = false;
//                    Console.WriteLine("Disconnecting...");
//                    break;

//                default:
//                    Console.WriteLine("Invalid choice. Please enter 1-9.");
//                    break;
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.ForegroundColor = ConsoleColor.Red;
//            Console.WriteLine($"Error: {ex.Message}");
//            Console.ResetColor();
//        }

//        if (running)
//        {
//            await Task.Delay(500); // Brief pause for better UX
//        }
//    }
//}

//static async Task QueryLiveMatches()
//{
//    var dataApiUrl = "http://localhost:5004"; // DataAPI URL

//    Console.WriteLine($"\nQuerying DataAPI: {dataApiUrl}/api/matches/live");

//    try
//    {
//        using var httpClient = new HttpClient();
//        var response = await httpClient.GetAsync($"{dataApiUrl}/api/matches/live");

//        if (response.IsSuccessStatusCode)
//        {
//            var json = await response.Content.ReadAsStringAsync();
//            var matches = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

//            Console.ForegroundColor = ConsoleColor.White;
//            Console.WriteLine("\n📊 Live Matches:");
//            Console.WriteLine("========================================");

//            foreach (var match in matches)
//            {
//                Console.WriteLine($"Match {match["matchId"]}: {match["homeTeam"]} {match["homeScore"]} - {match["awayScore"]} {match["awayTeam"]}");
//                Console.WriteLine($"   Status: {match["status"]} | Minute: {match["minute"]}'");
//                Console.WriteLine($"   League: {match["league"]}");
//                Console.WriteLine("----------------------------------------");
//            }
//            Console.ResetColor();
//        }
//        else
//        {
//            Console.WriteLine($"API returned status code: {response.StatusCode}");
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.ForegroundColor = ConsoleColor.Red;
//        Console.WriteLine($"Error querying API: {ex.Message}");
//        Console.ResetColor();
//    }
//}

using Microsoft.AspNetCore.SignalR.Client;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("========================================");
Console.WriteLine("   365Scores Real-Time Console Client");
Console.WriteLine("========================================\n");

var notificationServiceUrl = args.Length > 0 ? args[0] : "http://localhost:5002";
var dataApiUrl = args.Length > 1 ? args[1] : "http://localhost:5091";
var hubUrl = $"{notificationServiceUrl}/sportshub";

Console.WriteLine($"🔗 SignalR Hub: {hubUrl}");
Console.WriteLine($"🔗 DataAPI: {dataApiUrl}\n");

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
    .Build();

// Setup event handlers
SetupEventHandlers(connection);

try
{
    await connection.StartAsync();
    Console.WriteLine("✅ Connected to NotificationService\n");

    // Display menu
    await DisplayMenuAndHandleInput(connection, dataApiUrl);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}

static void SetupEventHandlers(HubConnection connection)
{
    Console.OutputEncoding = Encoding.UTF8;

    connection.Reconnecting += error =>
    {
        Console.WriteLine($"⚠️ Connection lost. Reconnecting...");
        return Task.CompletedTask;
    };

    connection.Reconnected += connectionId =>
    {
        Console.WriteLine($"✅ Reconnected. Connection ID: {connectionId}");
        return Task.CompletedTask;
    };

    connection.Closed += error =>
    {
        Console.WriteLine($"❌ Connection closed. {error?.Message}");
        return Task.CompletedTask;
    };

    // Score updates
    connection.On<object>("ScoreUpdate", scoreUpdate =>
    {
        var json = JsonSerializer.Serialize(scoreUpdate, new JsonSerializerOptions { WriteIndented = true });
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n⚽ SCORE UPDATE - Match {data["matchId"]}");
        Console.WriteLine($"   Score: {data["homeScore"]} - {data["awayScore"]}");
        Console.WriteLine($"   Minute: {data["minute"]}'");
        Console.WriteLine($"   Status: {data["status"]}");
        Console.ResetColor();
    });

    // Sport events
    connection.On<object>("SportEvent", sportEvent =>
    {
        var json = JsonSerializer.Serialize(sportEvent, new JsonSerializerOptions { WriteIndented = true });
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        var eventType = data["eventType"].GetInt32();
        var eventName = eventType switch
        {
            1 => "⏱️ Match Start",
            2 => "⚽ GOAL",
            3 => "🟨 Card",
            4 => "🔄 Substitution",
            5 => "⏸️ Half Time",
            6 => "🏁 Full Time",
            7 => "🎯 Assist",
            8 => "🎯 Shot",
            9 => "🚩 Corner",
            10 => "🚫 Foul",
            _ => "📌 Event"
        };

        Console.ForegroundColor = eventType == 2 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
        Console.WriteLine($"\n{eventName} - Match {data["matchId"]}");
        Console.WriteLine($"   {data["description"]}");
        Console.WriteLine($"   Minute: {data["minute"]}'");
        Console.ResetColor();
    });

    // Team events
    connection.On<object>("TeamEvent", teamEvent =>
    {
        var json = JsonSerializer.Serialize(teamEvent, new JsonSerializerOptions { WriteIndented = true });
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n🏆 TEAM EVENT - Team {data["teamId"]}");
        Console.WriteLine($"   {data["description"]}");
        Console.ResetColor();
    });

    // Player events
    connection.On<object>("PlayerEvent", playerEvent =>
    {
        var json = JsonSerializer.Serialize(playerEvent, new JsonSerializerOptions { WriteIndented = true });
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"\n👤 PLAYER EVENT - Player {data["playerId"]}");
        Console.WriteLine($"   {data["description"]}");
        Console.ResetColor();
    });

    // Subscription confirmations
    connection.On<object>("SubscriptionConfirmed", confirmation =>
    {
        var json = JsonSerializer.Serialize(confirmation, new JsonSerializerOptions { WriteIndented = true });
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Subscribed to {data["type"]}: {data["id"]}");
        Console.ResetColor();
    });

    // User preferences
    connection.On<object>("UserPreferences", preferences =>
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n📋 Your Current Preferences:");
        var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
        Console.ResetColor();
    });
}

static async Task DisplayMenuAndHandleInput(HubConnection connection, string dataApiUrl)
{
    var running = true;

    while (running)
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine("📱 Menu Options:");
        Console.WriteLine("========================================");
        Console.WriteLine("SignalR Subscriptions:");
        Console.WriteLine("  1. Subscribe to Team");
        Console.WriteLine("  2. Subscribe to Player");
        Console.WriteLine("  3. Subscribe to League");
        Console.WriteLine("  4. Subscribe to Match");
        Console.WriteLine("  5. Unsubscribe from Team");
        Console.WriteLine("  6. Unsubscribe from Player");
        Console.WriteLine("  7. View My Preferences");
        Console.WriteLine("\nREST API Queries:");
        Console.WriteLine("  8. Query Live Matches");
        Console.WriteLine("  9. Search Matches (Elasticsearch)");
        Console.WriteLine(" 10. Search Players (Elasticsearch)");
        Console.WriteLine(" 11. Search Events (Elasticsearch)");
        Console.WriteLine(" 12. View Match Events (MongoDB)");
        Console.WriteLine("\n  0. Exit");
        Console.Write("\n👉 Enter choice: ");

        var choice = Console.ReadLine();

        try
        {
            switch (choice)
            {
                case "1":
                    Console.Write("Enter Team ID: ");
                    if (int.TryParse(Console.ReadLine(), out int teamId))
                    {
                        await connection.InvokeAsync("SubscribeToTeam", teamId);
                        Console.WriteLine($"Subscribing to Team {teamId}...");
                    }
                    break;

                case "2":
                    Console.Write("Enter Player ID: ");
                    if (int.TryParse(Console.ReadLine(), out int playerId))
                    {
                        await connection.InvokeAsync("SubscribeToPlayer", playerId);
                        Console.WriteLine($"Subscribing to Player {playerId}...");
                    }
                    break;

                case "3":
                    Console.Write("Enter League ID: ");
                    if (int.TryParse(Console.ReadLine(), out int leagueId))
                    {
                        await connection.InvokeAsync("SubscribeToLeague", leagueId);
                        Console.WriteLine($"Subscribing to League {leagueId}...");
                    }
                    break;

                case "4":
                    Console.Write("Enter Match ID (1-20): ");
                    if (int.TryParse(Console.ReadLine(), out int matchId))
                    {
                        await connection.InvokeAsync("SubscribeToMatch", matchId);
                        Console.WriteLine($"Subscribing to Match {matchId}...");
                    }
                    break;

                case "5":
                    Console.Write("Enter Team ID to unsubscribe: ");
                    if (int.TryParse(Console.ReadLine(), out int unsubTeamId))
                    {
                        await connection.InvokeAsync("UnsubscribeFromTeam", unsubTeamId);
                        Console.WriteLine($"Unsubscribed from Team {unsubTeamId}");
                    }
                    break;

                case "6":
                    Console.Write("Enter Player ID to unsubscribe: ");
                    if (int.TryParse(Console.ReadLine(), out int unsubPlayerId))
                    {
                        await connection.InvokeAsync("UnsubscribeFromPlayer", unsubPlayerId);
                        Console.WriteLine($"Unsubscribed from Player {unsubPlayerId}");
                    }
                    break;

                case "7":
                    await connection.InvokeAsync("GetUserPreferences");
                    break;

                case "8":
                    await QueryLiveMatches(dataApiUrl);
                    break;

                case "9":
                    await SearchMatches(dataApiUrl);
                    break;

                case "10":
                    await SearchPlayers(dataApiUrl);
                    break;

                case "11":
                    await SearchEvents(dataApiUrl);
                    break;

                case "12":
                    await ViewMatchEvents(dataApiUrl);
                    break;

                case "0":
                    running = false;
                    Console.WriteLine("👋 Disconnecting...");
                    break;

                default:
                    Console.WriteLine("❌ Invalid choice. Please enter 0-12.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
        }

        if (running)
        {
            await Task.Delay(500); // Brief pause for better UX
        }
    }
}

static async Task QueryLiveMatches(string dataApiUrl)
{
    Console.WriteLine($"\n🔍 Querying Live Matches from DataAPI...");

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{dataApiUrl}/api/matches/live");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var matches = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n📊 Live Matches:");
            Console.WriteLine("========================================");

            if (matches == null || matches.Count == 0)
            {
                Console.WriteLine("No live matches at the moment.");
            }
            else
            {
                foreach (var match in matches)
                {
                    Console.WriteLine($"Match {match["matchId"]}: {match["homeTeam"]} {match["homeScore"]} - {match["awayScore"]} {match["awayTeam"]}");
                    Console.WriteLine($"   Status: {match["status"]} | Minute: {match["minute"]}'");
                    Console.WriteLine($"   League: {match["league"]}");
                    Console.WriteLine("----------------------------------------");
                }
            }
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"❌ API returned status code: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error querying API: {ex.Message}");
        Console.ResetColor();
    }
}

static async Task SearchMatches(string dataApiUrl)
{
    Console.Write("\n🔍 Enter search term for matches (team name, league): ");
    var searchTerm = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        Console.WriteLine("❌ Search term cannot be empty.");
        return;
    }

    Console.WriteLine($"Searching Elasticsearch for matches: '{searchTerm}'...");

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{dataApiUrl}/api/search/matches?query={Uri.EscapeDataString(searchTerm)}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<JsonElement>(json);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🔎 Elasticsearch Match Results:");
            Console.WriteLine("========================================");
            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"❌ API returned status code: {response.StatusCode}");
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {error}");
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error searching matches: {ex.Message}");
        Console.ResetColor();
    }
}

static async Task SearchPlayers(string dataApiUrl)
{
    Console.Write("\n🔍 Enter player name to search: ");
    var searchTerm = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        Console.WriteLine("❌ Search term cannot be empty.");
        return;
    }

    Console.WriteLine($"Searching Elasticsearch for players: '{searchTerm}'...");

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{dataApiUrl}/api/search/players?query={Uri.EscapeDataString(searchTerm)}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<JsonElement>(json);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n👤 Elasticsearch Player Results:");
            Console.WriteLine("========================================");
            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"❌ API returned status code: {response.StatusCode}");
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {error}");
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error searching players: {ex.Message}");
        Console.ResetColor();
    }
}

static async Task SearchEvents(string dataApiUrl)
{
    Console.Write("\n🔍 Enter search term for events (goal, card, etc.): ");
    var searchTerm = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        Console.WriteLine("❌ Search term cannot be empty.");
        return;
    }

    Console.Write("Limit results (default 20): ");
    var limitStr = Console.ReadLine();
    var limit = int.TryParse(limitStr, out var l) ? l : 20;

    Console.WriteLine($"Searching Elasticsearch for events: '{searchTerm}' (limit: {limit})...");

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{dataApiUrl}/api/search/events?query={Uri.EscapeDataString(searchTerm)}&limit={limit}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<JsonElement>(json);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n📌 Elasticsearch Event Results:");
            Console.WriteLine("========================================");
            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"❌ API returned status code: {response.StatusCode}");
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {error}");
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error searching events: {ex.Message}");
        Console.ResetColor();
    }
}

static async Task ViewMatchEvents(string dataApiUrl)
{
    Console.Write("\n🔍 Enter Match ID to view events: ");
    if (!int.TryParse(Console.ReadLine(), out int matchId))
    {
        Console.WriteLine("❌ Invalid Match ID.");
        return;
    }

    Console.WriteLine($"Fetching events for Match {matchId} from MongoDB...");

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{dataApiUrl}/api/events/match/{matchId}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var events = JsonSerializer.Deserialize<JsonElement>(json);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n📜 MongoDB Events for Match {matchId}:");
            Console.WriteLine("========================================");
            Console.WriteLine(JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true }));
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"❌ API returned status code: {response.StatusCode}");
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {error}");
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error fetching match events: {ex.Message}");
        Console.ResetColor();
    }
}