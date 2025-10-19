# Troubleshooting Guide

## Common Issues and Solutions

### 🔴 Issue: Kafka not starting or services can't connect

**Symptoms:**
- Services log "Connection refused" to Kafka
- `docker-compose ps` shows Kafka as unhealthy

**Solutions:**

```bash
# Check Kafka logs
docker logs kafka

# Restart Kafka and Zookeeper
docker-compose restart zookeeper kafka

# Wait for Kafka to be fully ready (check health)
docker exec kafka kafka-broker-api-versions --bootstrap-server localhost:9092

# Recreate Kafka topics if needed
docker-compose up kafka-init
```

**Common Causes:**
- Zookeeper not fully started before Kafka
- Port 9092 already in use
- Insufficient Docker resources (increase memory to 4GB+)

---

### 🔴 Issue: IngestService can't connect to SQL Server

**Symptoms:**
- `A network-related error occurred while connecting to SQL Server`
- IngestService logs show connection failures

**Solutions:**

```bash
# Verify SQL Server is running locally
# Windows: Check Services
# Mac/Linux: Check Docker or native installation

# Test connection
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd

# Create database if missing
CREATE DATABASE Sports365;
GO

# Update connection string in appsettings.json
# For Docker on Windows/Mac, use: host.docker.internal
# For Linux Docker, use: 172.17.0.1 (Docker bridge IP)

# Check SQL Server allows remote connections
# Enable TCP/IP in SQL Server Configuration Manager
```

**Connection String Examples:**

Windows/Mac Docker:
```
Server=host.docker.internal,1433;Database=Sports365;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

Linux Docker:
```
Server=172.17.0.1,1433;Database=Sports365;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

---

### 🔴 Issue: MongoDB connection fails

**Symptoms:**
- `MongoConnectionException` in IngestService logs
- Can't connect to `mongodb://host.docker.internal:27017`

**Solutions:**

```bash
# Verify MongoDB is running
# Mac: brew services list | grep mongodb
# Linux: systemctl status mongod
# Windows: Check Services

# Start MongoDB
# Mac: brew services start mongodb-community
# Linux: sudo systemctl start mongod
# Windows: net start MongoDB

# Test connection
mongo --eval "db.version()"
# or mongosh for newer versions
mongosh --eval "db.version()"

# Create database and collection
mongosh
use Sports365
db.createCollection("match_events")
exit

# Check MongoDB is listening on all interfaces
# Edit /etc/mongod.conf (Linux) or /usr/local/etc/mongod.conf (Mac)
net:
  bindIp: 0.0.0.0
  port: 27017
```

---

### 🔴 Issue: Redis connection timeout

**Symptoms:**
- `StackExchange.Redis.RedisConnectionException`
- NotificationService can't connect to Redis

**Solutions:**

```bash
# Check Redis is running
docker ps | grep redis

# Restart Redis
docker-compose restart redis

# Test Redis connection
docker exec -it redis redis-cli PING
# Should return: PONG

# Check Redis logs
docker logs redis

# If Redis is crashing, increase memory limit
docker-compose.yml:
  redis:
    deploy:
      resources:
        limits:
          memory: 512M
```

---

### 🔴 Issue: Elasticsearch not healthy

**Symptoms:**
- Elasticsearch container exits or restarts constantly
- Health check fails

**Solutions:**

```bash
# Check Elasticsearch logs
docker logs elasticsearch

# Common issue: Out of memory
# Increase ES heap size
docker-compose.yml:
  elasticsearch:
    environment:
      - "ES_JAVA_OPTS=-Xms1g -Xmx1g"  # Increase from 512m

# Check Elasticsearch health
curl http://localhost:9200/_cluster/health?pretty

# Reset Elasticsearch (WARNING: Deletes all data)
docker-compose down -v
docker volume rm 365scores_elasticsearch-data
docker-compose up -d elasticsearch

# Wait for yellow/green status
curl http://localhost:9200/_cluster/health?wait_for_status=yellow&timeout=50s
```

---

### 🔴 Issue: SignalR connection fails in Console Client

**Symptoms:**
- "Failed to connect" errors
- WebSocket handshake failures

**Solutions:**

```bash
# Verify NotificationService is running
docker ps | grep notification

# Check NotificationService logs
docker logs notification-service-1

# Test HTTP endpoint
curl http://localhost:5003/health

# Test SignalR negotiate endpoint
curl http://localhost:5003/sportshub/negotiate

# For CORS issues, verify NotificationService has AllowAll policy
# Program.cs should have:
app.UseCors("AllowAll");

# If using HTTPS, ensure certificate is valid or disable SSL validation (dev only)
```

---

### 🔴 Issue: Consumer lag keeps increasing

**Symptoms:**
- Kafka consumer lag grows continuously
- IngestService can't keep up with messages

**Solutions:**

```bash
# Check consumer lag
docker exec kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe --group ingest-service-group

# Scale IngestService
docker-compose up -d --scale ingest-service-1=5

# Increase batch size and processing efficiency
# IngestService appsettings.json:
"Kafka": {
  "MaxBatchSize": 500,
  "MaxPollIntervalMs": 300000
}

# Check for slow database writes
# Add indexes, optimize queries, use bulk inserts

# Monitor IngestService logs for errors
docker logs -f ingest-service-1 | grep ERROR
```

---

### 🔴 Issue: Docker Compose fails to build

**Symptoms:**
- Build errors during `docker-compose up`
- Missing project references
- NuGet restore failures

**Solutions:**

```bash
# Clean Docker build cache
docker-compose build --no-cache

# Verify all .csproj files exist
ls -la 365Scores.*/365Scores.*.csproj

# Check Dockerfile COPY paths match directory structure
# Ensure all Dockerfiles reference correct project paths

# Build solution locally first to verify
dotnet build

# If NuGet packages fail, clear cache
dotnet nuget locals all --clear

# Rebuild with verbose output
docker-compose build --progress=plain
```

---

### 🔴 Issue: Port conflicts

**Symptoms:**
- "Bind for 0.0.0.0:XXXX failed: port is already allocated"

**Solutions:**

```bash
# Find process using the port (example: 9092)
# Windows:
netstat -ano | findstr :9092

# Mac/Linux:
lsof -i :9092

# Kill the process
# Windows: taskkill /PID <pid> /F
# Mac/Linux: kill -9 <pid>

# Or change ports in docker-compose.yml
services:
  kafka:
    ports:
      - "9094:9092"  # Use different external port
```

---

### 🔴 Issue: EF Core migrations not applied

**Symptoms:**
- SQL tables don't exist
- "Invalid object name" errors

**Solutions:**

```bash
# Navigate to IngestService
cd 365Scores.IngestService

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update

# Or use code-based migration (already in Program.cs)
# The line: context.Database.EnsureCreated();
# Will create tables automatically

# Verify tables exist
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd -d Sports365 -Q "SELECT * FROM INFORMATION_SCHEMA.TABLES"
```

---

## 🛠️ Useful Commands

### Docker Management

```bash
# View all containers
docker-compose ps

# View logs for all services
docker-compose logs -f

# View logs for specific service
docker logs -f feed-listener

# Restart specific service
docker-compose restart ingest-service-1

# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: Deletes data)
docker-compose down -v

# Rebuild specific service
docker-compose build feed-listener

# Scale service
docker-compose up -d --scale ingest-service-1=5

# Execute command in container
docker exec -it kafka bash
```

### Kafka Commands

```bash
# List topics
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list

# Create topic manually
docker exec kafka kafka-topics --bootstrap-server localhost:9092 \
  --create --topic test-topic --partitions 3 --replication-factor 1

# Describe topic
docker exec kafka kafka-topics --bootstrap-server localhost:9092 \
  --describe --topic ingest-events

# Consume messages
docker exec kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic live-scores \
  --from-beginning

# Produce test message
docker exec -it kafka kafka-console-producer \
  --bootstrap-server localhost:9092 \
  --topic ingest-events

# Consumer groups
docker exec kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --list

# Reset consumer offset
docker exec kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group ingest-service-group \
  --reset-offsets --to-earliest \
  --topic ingest-events --execute

# Delete topic (be careful!)
docker exec kafka kafka-topics --bootstrap-server localhost:9092 \
  --delete --topic dead-letter-queue
```

### Redis Commands

```bash
# Connect to Redis CLI
docker exec -it redis redis-cli

# Inside Redis CLI:
PING                          # Test connection
KEYS *                        # List all keys
GET match:score:1            # Get specific key
HGETALL match:1              # Get hash
ZRANGE live:matches 0 -1     # Get sorted set
FLUSHDB                      # Clear current database (WARNING)
INFO                         # Server info
MONITOR                      # Watch commands in real-time
CONFIG GET maxmemory         # Check memory limit

# From bash:
docker exec redis redis-cli KEYS "*"
docker exec redis redis-cli GET match:score:1
docker exec redis redis-cli INFO stats
```

### MongoDB Commands

```bash
# Connect to MongoDB
mongosh

# Or if using older version
mongo

# Inside MongoDB shell:
use Sports365                               # Switch database
show collections                            # List collections
db.match_events.find().pretty()            # Query collection
db.match_events.find({matchId: 1}).pretty() # Filter query
db.match_events.count()                    # Count documents
db.match_events.createIndex({matchId: 1})  # Create index
db.match_events.drop()                     # Drop collection (WARNING)

# From bash:
mongosh --eval "use Sports365; db.match_events.count()"
```

### Elasticsearch Commands

```bash
# Cluster health
curl http://localhost:9200/_cluster/health?pretty

# Node info
curl http://localhost:9200/_nodes?pretty

# List indices
curl http://localhost:9200/_cat/indices?v

# Index stats
curl http://localhost:9200/sports-events/_stats?pretty

# Search all documents
curl http://localhost:9200/sports-events/_search?pretty

# Search with query
curl -X GET "http://localhost:9200/sports-events/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{
    "query": {
      "match": {
        "description": "goal"
      }
    }
  }'

# Count documents
curl http://localhost:9200/sports-events/_count?pretty

# Delete index (WARNING)
curl -X DELETE http://localhost:9200/sports-events

# Create index with mapping
curl -X PUT "http://localhost:9200/sports-events" \
  -H 'Content-Type: application/json' \
  -d '{
    "mappings": {
      "properties": {
        "matchId": { "type": "integer" },
        "eventType": { "type": "keyword" },
        "timestamp": { "type": "date" }
      }
    }
  }'
```

### SQL Server Commands

```bash
# Connect via sqlcmd
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd

# Inside sqlcmd:
SELECT @@VERSION;
GO

USE Sports365;
GO

SELECT * FROM Matches WHERE Status = 1;
GO

SELECT COUNT(*) FROM Matches;
GO

# From bash:
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd \
  -d Sports365 -Q "SELECT * FROM Matches"
```

### .NET CLI Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run specific project
dotnet run --project 365Scores.FeedListenerService

# Run with watch (auto-reload)
dotnet watch run --project 365Scores.DataAPI

# Clean build artifacts
dotnet clean

# Add package
dotnet add package Confluent.Kafka

# List packages
dotnet list package

# EF Core migrations
dotnet ef migrations add MigrationName --project 365Scores.IngestService
dotnet ef database update --project 365Scores.IngestService

# Run tests (if tests exist)
dotnet test
```

---

## 🔍 Performance Monitoring

### Check System Resources

```bash
# Docker stats
docker stats

# Specific container stats
docker stats feed-listener ingest-service-1

# Disk usage
docker system df

# Remove unused resources
docker system prune
```

### Application Metrics

```bash
# Monitor FeedListener throughput
docker logs -f feed-listener | grep "Events/sec"

# Monitor IngestService processing
docker logs -f ingest-service-1 | grep "Messages/sec"

# Monitor NotificationService
docker logs -f notification-service-1 | grep "Notifications/sec"

# Watch for errors across all services
docker-compose logs -f | grep -i error

# Watch for warnings
docker-compose logs -f | grep -i warning
```

### Kafka Performance

```bash
# Producer performance test
docker exec kafka kafka-producer-perf-test \
  --topic test-perf \
  --num-records 100000 \
  --record-size 1000 \
  --throughput -1 \
  --producer-props bootstrap.servers=localhost:9092

# Consumer performance test
docker exec kafka kafka-consumer-perf-test \
  --broker-list localhost:9092 \
  --topic test-perf \
  --messages 100000
```

---

## 🐛 Debugging Tips

### Enable Verbose Logging

Update `appsettings.Development.json` in each service:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Confluent.Kafka": "Debug"
    }
  }
}
```

### Attach Debugger (Visual Studio / Rider)

1. Run services locally (not in Docker)
2. Set breakpoints in your code
3. Start debugging (F5)
4. Trigger events through Console Client

### Remote Debugging in Docker

Add to Dockerfile:
```dockerfile
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 5000
```

---

## 📞 Getting Help

### Check Logs First

```bash
# All services
docker-compose logs --tail=100

# Specific service with timestamps
docker logs --timestamps feed-listener

# Follow logs
docker logs -f ingest-service-1
```

### Common Log Patterns

```bash
# Find errors
docker-compose logs | grep ERROR

# Find connection issues
docker-compose logs | grep -i "connection"

# Find timeout issues
docker-compose logs | grep -i "timeout"

# Find Kafka issues
docker-compose logs | grep -i "kafka"
```

### Environment Verification

```bash
# Check .NET version
dotnet --version

# Check Docker version
docker --version
docker-compose --version

# Check SQL Server
sqlcmd -S localhost -U sa -Q "SELECT @@VERSION"

# Check MongoDB
mongosh --version

# Verify all ports are free
netstat -an | grep LISTEN | grep -E ":(5001|5002|5003|5004|6379|9092|9200)"
```

---

## ✅ Health Check Endpoints

Test all services are running:

```bash
# FeedListenerService
curl http://localhost:5001/health

# IngestService
curl http://localhost:5002/health

# NotificationService  
curl http://localhost:5003/health

# DataAPI
curl http://localhost:5004/health

# Swagger UI
open http://localhost:5004/swagger

# Check all at once (bash)
for port in 5001 5002 5003 5004; do
  echo "Checking port $port..."
  curl -s http://localhost:$port/health || echo "FAILED"
done
```

---

## 🔄 Reset Everything

If all else fails, complete reset:

```bash
# Stop all services
docker-compose down -v

# Remove all Docker resources
docker system prune -a --volumes

# Drop SQL Server database
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd \
  -Q "DROP DATABASE IF EXISTS Sports365"

# Drop MongoDB database
mongosh --eval "use Sports365; db.dropDatabase()"

# Rebuild and start fresh
docker-compose build --no-cache
docker-compose up -d

# Wait for services to be healthy
sleep 30

# Run Console Client
cd 365Scores.ConsoleClient
dotnet run
```

---

## 📖 Additional Resources

- [Kafka Documentation](https://kafka.apache.org/documentation/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [MongoDB .NET Driver](https://www.mongodb.com/docs/drivers/csharp/)
- [Elasticsearch .NET Client](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/index.html)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)

---

**Remember**: This is a demo/learning project. For production deployments, add proper error handling, monitoring, security, and infrastructure as code (Kubernetes, Terraform, etc.).