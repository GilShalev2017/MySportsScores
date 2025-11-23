import React, { useState, useEffect, useRef } from 'react';
import { Activity, Trophy, Search, Bell, X, AlertCircle } from 'lucide-react';
import * as signalR from '@microsoft/signalr';

// Types
interface Match {
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  minute: number;
  status: string;
  league: string;
}

interface SportEvent {
  type: string;
  icon?: string;
  color?: string;
  message: string;
  matchId?: number;
  minute?: number;
  timestamp: Date;
  description?: string;
}

interface Subscriptions {
  teams: number[];
  players: number[];
  leagues: number[];
  matches: number[];
}

type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'error';
type SearchType = 'matches' | 'players' | 'events';
type TabType = 'live' | 'events' | 'subscriptions' | 'search';

const App: React.FC = () => {
  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected');
  const [events, setEvents] = useState<SportEvent[]>([]);
  const [liveMatches, setLiveMatches] = useState<Match[]>([]);
  const [subscriptions, setSubscriptions] = useState<Subscriptions>({
    teams: [],
    players: [],
    leagues: [],
    matches: []
  });
  const [activeTab, setActiveTab] = useState<TabType>('live');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [searchResults, setSearchResults] = useState<any>(null);
  const [searchType, setSearchType] = useState<SearchType>('matches');
  
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const notificationServiceUrl = 'http://localhost:5002';
  const dataApiUrl = 'http://localhost:5091';
  const hubUrl = `${notificationServiceUrl}/sportshub`;
  const maxEvents = 100;

  useEffect(() => {
    connectToSignalR();
    fetchLiveMatches();
    const interval = setInterval(fetchLiveMatches, 30000);
    
    return () => {
      clearInterval(interval);
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  const connectToSignalR = async (): Promise<void> => {
    try {
      setConnectionState('connecting');
      
      // Create SignalR connection
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Setup reconnection handlers
      connection.onreconnecting((error) => {
        console.log('⚠️ Connection lost. Reconnecting...', error);
        setConnectionState('connecting');
      });

      connection.onreconnected((connectionId) => {
        console.log('✅ Reconnected. Connection ID:', connectionId);
        setConnectionState('connected');
        addEvent({
          type: 'system',
          icon: '✅',
          color: 'text-green-400',
          message: 'Reconnected to server',
          timestamp: new Date()
        });
      });

      connection.onclose((error) => {
        console.log('❌ Connection closed.', error);
        setConnectionState('disconnected');
        addEvent({
          type: 'system',
          icon: '❌',
          color: 'text-red-400',
          message: 'Connection closed',
          timestamp: new Date()
        });
      });

      // Score updates
      connection.on('ScoreUpdate', (scoreUpdate: any) => {
        console.log('📊 Score Update:', scoreUpdate);
        addEvent({
          type: 'score',
          icon: '⚽',
          color: 'text-green-400',
          message: `SCORE UPDATE - Match ${scoreUpdate.matchId}`,
          matchId: scoreUpdate.matchId,
          minute: scoreUpdate.minute,
          timestamp: new Date(),
          description: `${scoreUpdate.homeScore} - ${scoreUpdate.awayScore} | ${scoreUpdate.status} ${scoreUpdate.minute}'`
        });
      });

      // Sport events
      connection.on('SportEvent', (sportEvent: any) => {
        console.log('🎯 Sport Event:', sportEvent);
        
        const eventType = sportEvent.eventType;
        const eventInfo = getEventInfo(eventType);
        
        addEvent({
          type: 'sport',
          icon: eventInfo.icon,
          color: eventInfo.color,
          message: eventInfo.name,
          matchId: sportEvent.matchId,
          minute: sportEvent.minute,
          timestamp: new Date(),
          description: `Match ${sportEvent.matchId} - ${sportEvent.description} at ${sportEvent.minute}'`
        });
      });

      // Team events
      connection.on('TeamEvent', (teamEvent: any) => {
        console.log('🏆 Team Event:', teamEvent);
        addEvent({
          type: 'team',
          icon: '🏆',
          color: 'text-purple-400',
          message: `TEAM EVENT - Team ${teamEvent.teamId}`,
          timestamp: new Date(),
          description: teamEvent.description
        });
      });

      // Player events
      connection.on('PlayerEvent', (playerEvent: any) => {
        console.log('👤 Player Event:', playerEvent);
        addEvent({
          type: 'player',
          icon: '👤',
          color: 'text-blue-400',
          message: `PLAYER EVENT - Player ${playerEvent.playerId}`,
          timestamp: new Date(),
          description: playerEvent.description
        });
      });

      // Subscription confirmations
      connection.on('SubscriptionConfirmed', (confirmation: any) => {
        console.log('✅ Subscription Confirmed:', confirmation);
        addEvent({
          type: 'subscription',
          icon: '✅',
          color: 'text-green-400',
          message: `Subscribed to ${confirmation.type}: ${confirmation.id}`,
          timestamp: new Date()
        });
      });

      // User preferences
      connection.on('UserPreferences', (preferences: any) => {
        console.log('📋 User Preferences:', preferences);
        addEvent({
          type: 'system',
          icon: '📋',
          color: 'text-cyan-400',
          message: 'User preferences updated',
          timestamp: new Date(),
          description: JSON.stringify(preferences, null, 2)
        });
      });

      // Start connection
      await connection.start();
      console.log('✅ Connected to SignalR Hub');
      setConnectionState('connected');
      connectionRef.current = connection;
      
      addEvent({
        type: 'system',
        icon: '🎉',
        color: 'text-green-400',
        message: 'Connected to 365Scores SignalR Hub',
        timestamp: new Date()
      });
    } catch (error) {
      console.error('Connection error:', error);
      setConnectionState('error');
      addEvent({
        type: 'system',
        icon: '❌',
        color: 'text-red-400',
        message: `Connection error: ${error}`,
        timestamp: new Date()
      });
    }
  };

  const getEventInfo = (eventType: number) => {
    const eventMap: Record<number, { icon: string; color: string; name: string }> = {
      1: { icon: '⏱️', color: 'text-blue-400', name: 'Match Start' },
      2: { icon: '⚽', color: 'text-yellow-400', name: 'GOAL!' },
      3: { icon: '🟨', color: 'text-yellow-300', name: 'Card' },
      4: { icon: '🔄', color: 'text-blue-400', name: 'Substitution' },
      5: { icon: '⏸️', color: 'text-gray-400', name: 'Half Time' },
      6: { icon: '🏁', color: 'text-green-400', name: 'Full Time' },
      7: { icon: '🎯', color: 'text-cyan-400', name: 'Assist' },
      8: { icon: '🎯', color: 'text-cyan-400', name: 'Shot' },
      9: { icon: '🚩', color: 'text-green-400', name: 'Corner' },
      10: { icon: '🚫', color: 'text-red-400', name: 'Foul' }
    };
    
    return eventMap[eventType] || { icon: '📌', color: 'text-white', name: 'Event' };
  };

  const addEvent = (event: SportEvent): void => {
    setEvents(prev => [event, ...prev].slice(0, maxEvents));
  };

  const fetchLiveMatches = async (): Promise<void> => {
    try {
      const response = await fetch(`${dataApiUrl}/api/matches/live`);
      if (response.ok) {
        const data: Match[] = await response.json();
        setLiveMatches(data);
      }
    } catch (error) {
      console.error('Error fetching live matches:', error);
    }
  };

  const handleSearch = async (): Promise<void> => {
    if (!searchQuery.trim()) return;
    
    try {
      const endpoint = searchType === 'matches' ? 'matches' : 
                      searchType === 'players' ? 'players' : 'events';
      const response = await fetch(
        `${dataApiUrl}/api/search/${endpoint}?query=${encodeURIComponent(searchQuery)}`
      );
      if (response.ok) {
        const data = await response.json();
        setSearchResults(data);
      }
    } catch (error) {
      console.error('Search error:', error);
      addEvent({
        type: 'system',
        icon: '❌',
        color: 'text-red-400',
        message: `Search error: ${error}`,
        timestamp: new Date()
      });
    }
  };

  const subscribe = async (type: string, id: number): Promise<void> => {
    if (!connectionRef.current || connectionState !== 'connected') {
      addEvent({
        type: 'system',
        icon: '⚠️',
        color: 'text-yellow-400',
        message: 'Not connected to server',
        timestamp: new Date()
      });
      return;
    }

    try {
      const method = `SubscribeTo${type.charAt(0).toUpperCase() + type.slice(1)}`;
      await connectionRef.current.invoke(method, id);
      
      setSubscriptions(prev => ({
        ...prev,
        [`${type}s`]: [...prev[`${type}s` as keyof Subscriptions], id]
      }));
      
      console.log(`Subscribed to ${type} ${id}`);
    } catch (error) {
      console.error('Subscribe error:', error);
      addEvent({
        type: 'system',
        icon: '❌',
        color: 'text-red-400',
        message: `Subscribe error: ${error}`,
        timestamp: new Date()
      });
    }
  };

  const unsubscribe = async (type: string, id: number): Promise<void> => {
    if (!connectionRef.current || connectionState !== 'connected') {
      addEvent({
        type: 'system',
        icon: '⚠️',
        color: 'text-yellow-400',
        message: 'Not connected to server',
        timestamp: new Date()
      });
      return;
    }

    try {
      const method = `UnsubscribeFrom${type.charAt(0).toUpperCase() + type.slice(1)}`;
      await connectionRef.current.invoke(method, id);
      
      setSubscriptions(prev => ({
        ...prev,
        [`${type}s`]: (prev[`${type}s` as keyof Subscriptions] as number[]).filter(i => i !== id)
      }));
      
      addEvent({
        type: 'subscription',
        icon: '🔕',
        color: 'text-gray-400',
        message: `Unsubscribed from ${type} ${id}`,
        timestamp: new Date()
      });
      
      console.log(`Unsubscribed from ${type} ${id}`);
    } catch (error) {
      console.error('Unsubscribe error:', error);
      addEvent({
        type: 'system',
        icon: '❌',
        color: 'text-red-400',
        message: `Unsubscribe error: ${error}`,
        timestamp: new Date()
      });
    }
  };

  const getConnectionColor = (): string => {
    switch (connectionState) {
      case 'connected': return 'bg-green-500';
      case 'connecting': return 'bg-yellow-500 animate-pulse';
      case 'error': return 'bg-red-500';
      default: return 'bg-gray-500';
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-gray-900 text-white">
      {/* Header */}
      <header className="bg-gray-900/50 backdrop-blur-sm border-b border-gray-700 sticky top-0 z-50">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <Activity className="w-8 h-8 text-blue-400" />
              <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
                365Scores Real-Time
              </h1>
            </div>
            
            <div className="flex items-center space-x-4">
              <div className="flex items-center space-x-2">
                <div className={`w-3 h-3 rounded-full ${getConnectionColor()}`}></div>
                <span className="text-sm text-gray-300 capitalize">{connectionState}</span>
              </div>
              
              <div className="flex items-center space-x-2 bg-gray-800 rounded-lg px-3 py-2">
                <Bell className="w-4 h-4 text-blue-400" />
                <span className="text-sm">{events.length} events</span>
              </div>
            </div>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-6">
        {/* Tab Navigation */}
        <div className="flex space-x-2 mb-6 bg-gray-800/50 rounded-lg p-1">
          {(['live', 'events', 'subscriptions', 'search'] as TabType[]).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`flex-1 py-2 px-4 rounded-md transition-all ${
                activeTab === tab
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-400 hover:text-white hover:bg-gray-700/50'
              }`}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {activeTab === 'live' && (
              <div className="bg-gray-800/50 backdrop-blur-sm rounded-lg border border-gray-700 p-6">
                <div className="flex items-center space-x-2 mb-4">
                  <Trophy className="w-5 h-5 text-yellow-400" />
                  <h2 className="text-xl font-bold">Live Matches</h2>
                </div>
                
                <div className="space-y-3">
                  {liveMatches.length === 0 ? (
                    <div className="text-center py-8 text-gray-400">
                      <AlertCircle className="w-12 h-12 mx-auto mb-2 opacity-50" />
                      <p>No live matches at the moment</p>
                    </div>
                  ) : (
                    liveMatches.map(match => (
                      <div
                        key={match.matchId}
                        className="bg-gray-900/50 rounded-lg p-4 hover:bg-gray-900/70 transition-all cursor-pointer border border-gray-700 hover:border-blue-500"
                        onClick={() => subscribe('match', match.matchId)}
                      >
                        <div className="flex items-center justify-between mb-2">
                          <span className="text-xs text-gray-400">{match.league}</span>
                          <span className={`text-xs px-2 py-1 rounded ${
                            match.status === 'Live' ? 'bg-red-600' : 'bg-yellow-600'
                          }`}>
                            {match.status} {match.minute}'
                          </span>
                        </div>
                        
                        <div className="grid grid-cols-3 items-center gap-4">
                          <div className="text-right">
                            <div className="font-semibold">{match.homeTeam}</div>
                          </div>
                          <div className="text-center">
                            <div className="text-3xl font-bold text-blue-400">
                              {match.homeScore} - {match.awayScore}
                            </div>
                          </div>
                          <div className="text-left">
                            <div className="font-semibold">{match.awayTeam}</div>
                          </div>
                        </div>
                        
                        <div className="mt-2 text-center">
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              subscribe('match', match.matchId);
                            }}
                            className="text-xs text-blue-400 hover:text-blue-300"
                          >
                            Subscribe to updates
                          </button>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            {activeTab === 'events' && (
              <div className="bg-gray-800/50 backdrop-blur-sm rounded-lg border border-gray-700 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center space-x-2">
                    <Activity className="w-5 h-5 text-green-400" />
                    <h2 className="text-xl font-bold">Real-Time Events</h2>
                  </div>
                  <button
                    onClick={() => setEvents([])}
                    className="text-sm text-gray-400 hover:text-white"
                  >
                    Clear All
                  </button>
                </div>
                
                <div className="space-y-2 max-h-[600px] overflow-y-auto">
                  {events.length === 0 ? (
                    <div className="text-center py-8 text-gray-400">
                      <Activity className="w-12 h-12 mx-auto mb-2 opacity-50" />
                      <p>Waiting for events...</p>
                    </div>
                  ) : (
                    events.map((event, idx) => (
                      <div
                        key={idx}
                        className="bg-gray-900/50 rounded-lg p-3 border border-gray-700 hover:border-gray-600 transition-all"
                      >
                        <div className="flex items-start space-x-3">
                          <span className="text-2xl">{event.icon || '📌'}</span>
                          <div className="flex-1">
                            <div className={`font-semibold ${event.color || 'text-white'}`}>
                              {event.message}
                            </div>
                            {event.description && (
                              <div className="text-sm text-gray-400 mt-1">
                                {event.description}
                              </div>
                            )}
                            <div className="text-xs text-gray-500 mt-1">
                              {event.timestamp.toLocaleTimeString()}
                            </div>
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            {activeTab === 'subscriptions' && (
              <div className="bg-gray-800/50 backdrop-blur-sm rounded-lg border border-gray-700 p-6">
                <div className="flex items-center space-x-2 mb-4">
                  <Bell className="w-5 h-5 text-purple-400" />
                  <h2 className="text-xl font-bold">My Subscriptions</h2>
                </div>
                
                <div className="space-y-4">
                  {(Object.entries(subscriptions) as [keyof Subscriptions, number[]][]).map(([type, items]) => (
                    <div key={type}>
                      <h3 className="text-sm font-semibold text-gray-400 uppercase mb-2">
                        {type}
                      </h3>
                      {items.length === 0 ? (
                        <p className="text-sm text-gray-500">No {type} subscribed</p>
                      ) : (
                        <div className="flex flex-wrap gap-2">
                          {items.map(id => (
                            <div
                              key={id}
                              className="bg-blue-600/20 border border-blue-500/50 rounded-lg px-3 py-2 flex items-center space-x-2"
                            >
                              <span className="text-sm">{type.slice(0, -1)} {id}</span>
                              <button
                                onClick={() => unsubscribe(type.slice(0, -1), id)}
                                className="text-red-400 hover:text-red-300"
                              >
                                <X className="w-4 h-4" />
                              </button>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>

                <div className="mt-6 pt-6 border-t border-gray-700">
                  <h3 className="text-sm font-semibold mb-3">Quick Subscribe</h3>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => subscribe('match', Math.floor(Math.random() * 20) + 1)}
                      className="bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-lg transition-all"
                    >
                      Random Match
                    </button>
                    <button
                      onClick={() => subscribe('team', Math.floor(Math.random() * 40) + 1)}
                      className="bg-green-600 hover:bg-green-700 px-4 py-2 rounded-lg transition-all"
                    >
                      Random Team
                    </button>
                  </div>
                </div>
              </div>
            )}

            {activeTab === 'search' && (
              <div className="bg-gray-800/50 backdrop-blur-sm rounded-lg border border-gray-700 p-6">
                <div className="flex items-center space-x-2 mb-4">
                  <Search className="w-5 h-5 text-cyan-400" />
                  <h2 className="text-xl font-bold">Search</h2>
                </div>
                
                <div className="space-y-4">
                  <div className="flex space-x-2">
                    {(['matches', 'players', 'events'] as SearchType[]).map(type => (
                      <button
                        key={type}
                        onClick={() => setSearchType(type)}
                        className={`px-4 py-2 rounded-lg transition-all ${
                          searchType === type
                            ? 'bg-blue-600 text-white'
                            : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                        }`}
                      >
                        {type.charAt(0).toUpperCase() + type.slice(1)}
                      </button>
                    ))}
                  </div>

                  <div className="flex space-x-2">
                    <input
                      type="text"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                      placeholder={`Search ${searchType}...`}
                      className="flex-1 bg-gray-900 border border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500"
                    />
                    <button
                      onClick={handleSearch}
                      className="bg-blue-600 hover:bg-blue-700 px-6 py-2 rounded-lg transition-all"
                    >
                      Search
                    </button>
                  </div>

                  {searchResults && (
                    <div className="mt-4 bg-gray-900/50 rounded-lg p-4">
                      <h3 className="font-semibold mb-2">Results</h3>
                      <pre className="text-sm text-gray-300 overflow-x-auto">
                        {JSON.stringify(searchResults, null, 2)}
                      </pre>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Stats Card */}
            <div className="bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg p-6">
              <h3 className="text-lg font-bold mb-4">System Stats</h3>
              <div className="space-y-3">
                <div className="flex justify-between items-center">
                  <span className="text-sm opacity-90">Live Matches</span>
                  <span className="text-2xl font-bold">{liveMatches.length}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm opacity-90">Events Received</span>
                  <span className="text-2xl font-bold">{events.length}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm opacity-90">Subscriptions</span>
                  <span className="text-2xl font-bold">
                    {Object.values(subscriptions).reduce((acc, arr) => acc + arr.length, 0)}
                  </span>
                </div>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="bg-gray-800/50 backdrop-blur-sm rounded-lg border border-gray-700 p-6">
              <h3 className="text-lg font-bold mb-4">Quick Actions</h3>
              <div className="space-y-2">
                <button
                  onClick={() => setActiveTab('live')}
                  className="w-full bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-lg transition-all flex items-center justify-center space-x-2"
                >
                  <Trophy className="w-4 h-4" />
                  <span>View Live Matches</span>
                </button>
                <button
                  onClick={() => setActiveTab('subscriptions')}
                  className="w-full bg-purple-600 hover:bg-purple-700 px-4 py-2 rounded-lg transition-all flex items-center justify-center space-x-2"
                >
                  <Bell className="w-4 h-4" />
                  <span>Manage Subscriptions</span>
                </button>
                <button
                  onClick={fetchLiveMatches}
                  className="w-full bg-green-600 hover:bg-green-700 px-4 py-2 rounded-lg transition-all flex items-center justify-center space-x-2"
                >
                  <Activity className="w-4 h-4" />
                  <span>Refresh Data</span>
                </button>
              </div>
            </div>

            {/* Recent Events Preview */}
            <div className="bg-gray-800/50 backdrop-blur-sm rounded-lg border border-gray-700 p-6">
              <h3 className="text-lg font-bold mb-4">Recent Events</h3>
              <div className="space-y-2">
                {events.slice(0, 5).map((event, idx) => (
                  <div key={idx} className="text-sm p-2 bg-gray-900/50 rounded">
                    <div className="flex items-center space-x-2">
                      <span>{event.icon}</span>
                      <span className="text-gray-300 truncate">{event.message}</span>
                    </div>
                  </div>
                ))}
                {events.length === 0 && (
                  <p className="text-sm text-gray-500 text-center py-4">
                    No events yet
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default App;