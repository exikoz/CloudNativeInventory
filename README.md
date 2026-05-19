# CloudNativeInventory API

A .NET 9 Inventory API demonstrating containerization, CI/CD and secure configuration management in Azure.

## Azure Services

| Service | Purpose |
|---------|---------|
| Azure Container Apps | Container hosting |
| Azure Container Registry | Docker image storage |
| Azure Key Vault | Secret management |
| System-Assigned Managed Identity | Passwordless auth to Key Vault |
| Log Analytics Workspace | Monitoring and logging |

## Run Locally

```bash
dotnet run --project CloudNativeInventory.Api
```

The API starts on `http://localhost:5000`. Test with `GET /api/inventory`.

Locally, an InMemory database is used and the development secret in `appsettings.json` is intentionally marked as insecure.

### Run with Docker

```bash
docker build -t inventory-api -f CloudNativeInventory.Api/Dockerfile .
docker run -p 8080:8080 inventory-api
```

The API responds on `http://localhost:8080/api/inventory`.

## CI/CD Pipeline

Defined in `.github/workflows/main.yml`. Triggered on push to `main`.

### Steps

1. **Build and Test** - `dotnet restore`, `build`, `test`. If tests fail, the pipeline stops.
2. **Docker Build and Push** - Builds the image with multi-stage Dockerfile and pushes to ACR. Tagged with commit SHA for traceability.
3. **Deploy** - Updates Azure Container Apps with the new image. Only runs if previous steps succeed.

### GitHub Secrets Used

| Secret | Purpose |
|--------|---------|
| `ACR_USERNAME` | Azure Container Registry username |
| `ACR_PASSWORD` | Azure Container Registry password |
| `AZURE_CREDENTIALS` | Service Principal for Azure deploy |
| `AZURE_RESOURCE_GROUP` | Resource group name |
| `AZURE_CONTAINER_APP` | Container App name |

No sensitive values exist in code or version-controlled files.

## Deploy and Verification

Deployment happens automatically when tests pass on `main`.

Verification endpoint:

```
GET https://aca-inventory-api.happyforest-8346c7c9.swedencentral.azurecontainerapps.io/api/inventory/system/verify-integration
```

Expected response in production:

```json
{
  "status": "Secured",
  "message": "Hemlighet laddades framgångsrikt via säker konfiguration."
}
```

This confirms that Key Vault integration works and secrets are loaded via Managed Identity.

## Monitoring

Azure Container Apps is connected to a Log Analytics Workspace that collects container logs, request logs, and resource usage.

```bash
az containerapp logs show --name aca-inventory-api --resource-group rg-cloudnative-inventory --type console --tail 50
```

---

## Architecture Decision Record (ADR)

### ADR-001: Container Hosting

**Decision:** Azure Container Apps

**Reasoning:**
- Scale-to-zero reduces cost for variable workloads
- Built for containers (no App Service Plan overhead)
- Native Managed Identity support
- Simpler than AKS while still container-native
- Built-in HTTPS ingress with TLS

Alternative considered: Azure App Service. Rejected due to always-on cost model and less container-native design.

### ADR-002: Container Registry

**Decision:** Azure Container Registry (ACR)

**Reasoning:**
- Same Azure ecosystem, simple integration with Container Apps
- Supports image pull via Managed Identity
- Integrated with GitHub Actions via service principal

### ADR-003: Secret Management

**Decision:** Azure Key Vault with System-Assigned Managed Identity and RBAC role `Key Vault Secrets User`.

**Reasoning:**
- Least privilege: the identity can only read secrets, not create or delete them
- Passwordless architecture: `DefaultAzureCredential` handles auth automatically
- Secrets are stored outside code and version-controlled files
- Secrets can be rotated in Key Vault without redeployment

**Security strategy:**
- No secrets in source code (appsettings.json only contains a local dev marker)
- No secrets in environment variables (only Key Vault URL is set, not the secret itself)
- No secrets in pipeline logs (GitHub masks all secrets)
- No secrets in git history

### ADR-004: Container Security (Dockerfile)

**Decision:** Multi-stage build with non-root user.

**Reasoning:**
- Multi-stage separates SDK (~900MB) from runtime (~200MB), reducing attack surface and image size
- `USER app` runs the process as non-root. If the container is compromised, the attacker cannot escalate to root
- Port 8080 is used because ports below 1024 require root
- `/p:UseAppHost=false` skips native executable, further reducing image size

**Layer optimization:**
- COPY csproj and restore first (cached unless dependencies change)
- COPY source and build second (rebuilds only on code changes)

### ADR-005: Pipeline Design

**Decision:** GitHub Actions with separate jobs for test, build, and deploy with explicit dependencies (gates).

**Reasoning:**
- Quality: tests run before image build. Failure stops the pipeline.
- Traceability: images tagged with git commit SHA. You can always trace what code runs in production.
- Delivery speed: automatic deploy on green push to main gives fast feedback.
- Security: credentials handled via GitHub Secrets, never exposed in logs.
