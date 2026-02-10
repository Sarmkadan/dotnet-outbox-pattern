// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Deployment Guide

This guide covers deploying the Outbox Pattern to various environments.

## Local Development

### Prerequisites

```bash
# Check .NET installation
dotnet --version

# Check SQL Server LocalDB
sqllocaldb info
```

### Setup

```bash
# Navigate to project
cd dotnet-outbox-pattern

# Restore and build
dotnet build

# Run migrations
dotnet ef database update

# Start the application
dotnet run
```

## Docker Deployment

### Build Image

```bash
docker build -t dotnet-outbox-pattern:1.0.0 .

# Tag for registry
docker tag dotnet-outbox-pattern:1.0.0 myregistry.azurecr.io/dotnet-outbox-pattern:1.0.0

# Push to registry
docker push myregistry.azurecr.io/dotnet-outbox-pattern:1.0.0
```

### Docker Compose (Local)

```bash
docker-compose up

# In another terminal
curl https://localhost:5001/swagger

# Cleanup
docker-compose down
```

### Environment Variables

Create `.env` file:

```bash
SA_PASSWORD=YourStrongPassword123!
ASPNETCORE_ENVIRONMENT=Development
Outbox__ProcessorEnabled=true
Outbox__BatchSize=100
```

## Kubernetes Deployment

### Namespace and Secrets

```bash
kubectl create namespace outbox
kubectl apply -f k8s/secrets.yaml -n outbox
```

**secrets.yaml:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: outbox-secrets
  namespace: outbox
type: Opaque
stringData:
  connection-string: "Server=sql-server;Database=OutboxPattern;..."
  sa-password: "YourStrongPassword123!"
```

### Database Migration Job

```bash
kubectl apply -f k8s/migrations-job.yaml -n outbox
```

**migrations-job.yaml:**
```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: outbox-migrations
  namespace: outbox
spec:
  template:
    spec:
      containers:
      - name: migrations
        image: dotnet-outbox-pattern:1.0.0
        command: ["dotnet", "ef", "database", "update"]
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: outbox-secrets
              key: connection-string
      restartPolicy: Never
```

### Application Deployment

```bash
kubectl apply -f k8s/deployment.yaml -n outbox
kubectl apply -f k8s/service.yaml -n outbox
```

**deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: outbox-api
  namespace: outbox
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: outbox-api
  template:
    metadata:
      labels:
        app: outbox-api
    spec:
      containers:
      - name: api
        image: dotnet-outbox-pattern:1.0.0
        imagePullPolicy: Always
        ports:
        - containerPort: 5001
          name: https
        - containerPort: 5000
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: ASPNETCORE_URLS
          value: https://+:5001
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: outbox-secrets
              key: connection-string
        - name: Outbox__ProcessorEnabled
          value: "true"
        - name: Outbox__BatchSize
          value: "100"
        livenessProbe:
          httpGet:
            path: /health
            port: 5001
            scheme: HTTPS
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health
            port: 5001
            scheme: HTTPS
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

**service.yaml:**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: outbox-api
  namespace: outbox
spec:
  type: ClusterIP
  ports:
  - port: 5001
    targetPort: 5001
    protocol: TCP
    name: https
  selector:
    app: outbox-api
```

### Horizontal Pod Autoscaling

```bash
kubectl apply -f k8s/hpa.yaml -n outbox
```

**hpa.yaml:**
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: outbox-api-hpa
  namespace: outbox
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: outbox-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

## Azure App Service

### Prepare

```bash
# Login
az login
az account set --subscription "your-subscription"

# Create resource group
az group create --name outbox-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name outbox-plan \
  --resource-group outbox-rg \
  --sku B2
```

### Create SQL Database

```bash
az sql server create \
  --name outbox-sqlserver \
  --resource-group outbox-rg \
  --admin-user sqladmin \
  --admin-password "YourStrongPassword123!"

az sql db create \
  --server outbox-sqlserver \
  --resource-group outbox-rg \
  --name OutboxPattern
```

### Deploy App

```bash
# Create web app
az webapp create \
  --name outbox-api \
  --resource-group outbox-rg \
  --plan outbox-plan \
  --runtime "dotnet:10"

# Configure connection string
az webapp config connection-string set \
  --name outbox-api \
  --resource-group outbox-rg \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=outbox-sqlserver.database.windows.net;Database=OutboxPattern;..."

# Deploy from local Git
git remote add azure https://<username>@outbox-api.scm.azurewebsites.net:443/outbox-api.git
git push azure main
```

## AWS Elastic Beanstalk

### Prepare

```bash
# Install EB CLI
pip install awsebcli

# Initialize
eb init -p "dotnet-core" outbox-pattern

# Create .ebextensions/dotnet.config
```

**.ebextensions/dotnet.config:**
```yaml
option_settings:
  aws:elasticbeanstalk:container:dotnet:image:
    ECRRepositoryName: dotnet-outbox-pattern
  aws:autoscaling:asg:
    MinSize: 2
    MaxSize: 10
  aws:autoscaling:trigger:
    MeasureName: CPUUtilization
    Statistic: Average
    Unit: Percent
    UpperThreshold: 70
    LowerThreshold: 30
```

### Deploy

```bash
# Create environment
eb create outbox-prod

# Deploy updates
eb deploy

# Monitor
eb status
eb logs
```

## Production Checklist

### Pre-Deployment

- [ ] Run all tests: `dotnet test`
- [ ] Build release: `dotnet build -c Release`
- [ ] Update version in `.csproj`
- [ ] Review CHANGELOG.md
- [ ] Database backup created
- [ ] Connection string encrypted in vault
- [ ] Message broker certificates configured
- [ ] Logging configured for production

### Deployment

- [ ] Database migrations run successfully
- [ ] Health check passes: `curl /health`
- [ ] Swagger accessible (or disabled in production)
- [ ] Message processor running
- [ ] Metrics endpoint accessible
- [ ] Load balanced across instances
- [ ] SSL/TLS certificates installed
- [ ] Firewall rules configured

### Post-Deployment

- [ ] Monitor application logs: `kubectl logs -n outbox`
- [ ] Check metrics endpoint
- [ ] Verify message processing
- [ ] Test dead letter queue handling
- [ ] Confirm backup jobs running
- [ ] Monitor database performance
- [ ] Test failover scenarios
- [ ] Document issues and resolutions

### Monitoring

```bash
# Watch logs in real-time
kubectl logs -f -n outbox deployments/outbox-api

# Monitor resource usage
kubectl top pods -n outbox

# Check service status
kubectl get svc -n outbox
kubectl get pods -n outbox

# View events
kubectl describe pod <pod-name> -n outbox
```

### Rollback Procedure

```bash
# View deployment history
kubectl rollout history deployment/outbox-api -n outbox

# Rollback to previous version
kubectl rollout undo deployment/outbox-api -n outbox

# Rollback to specific revision
kubectl rollout undo deployment/outbox-api --to-revision=2 -n outbox
```

## Performance Tuning

### Database

```sql
-- Rebuild indexes
ALTER INDEX ALL ON OutboxMessages REBUILD;
ALTER INDEX ALL ON DeadLetters REBUILD;

-- Update statistics
UPDATE STATISTICS OutboxMessages;
UPDATE STATISTICS DeadLetters;

-- Check index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id 
    AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10;
```

### Application

```json
{
  "Outbox": {
    "BatchSize": 200,
    "DelayBetweenBatches": 2000,
    "PreservePartitionOrdering": true
  }
}
```

**Tuning tips:**
- Increase BatchSize for high throughput (max 500)
- Decrease DelayBetweenBatches for lower latency
- Monitor database connection pool
- Use read replicas for metrics queries

## Disaster Recovery

### Backup Strategy

```bash
# Automated daily backups (SQL Server)
az sql db backup create \
  --server outbox-sqlserver \
  --resource-group outbox-rg \
  --name OutboxPattern \
  --backup-name daily-$(date +%Y-%m-%d)
```

### Restore Procedure

```bash
# Restore from backup
az sql db restore \
  --dest-name OutboxPattern-restored \
  --server outbox-sqlserver \
  --resource-group outbox-rg \
  --backup-name daily-2024-01-15
```

### Message Recovery

```csharp
// If messages were lost, requeue from backup
var backupMessages = await backupContext.OutboxMessages.ToListAsync();
foreach (var msg in backupMessages)
{
    var newMsg = new OutboxMessage { /* ... */ };
    await productionContext.OutboxMessages.AddAsync(newMsg);
}
await productionContext.SaveChangesAsync();
```

