param location string = 'eastus'
param environmentName string = 'vectorscale-env'

// Container Apps Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
    }
  }
}

// Redis Cache
resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: 'vectorscale-redis'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// Sidecar Container App
resource sidecar 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'vectorscale-sidecar'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 50051
      }
    }
    template: {
      containers: [
        {
          name: 'sidecar'
          image: 'ghcr.io/ritunjaym/vectorscale-sidecar:latest'
          resources: {
            cpu: json('2.0')
            memory: '4Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

// API Container App
resource api 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'vectorscale-api'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: 'ghcr.io/ritunjaym/vectorscale-api:latest'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: [
            {
              name: 'VectorScale__SidecarGrpcAddress'
              value: 'http://${sidecar.properties.configuration.ingress.fqdn}:50051'
            }
            {
              name: 'VectorScale__Redis__ConnectionString'
              value: '${redis.name}.redis.cache.windows.net:6380,password=${redis.listKeys().primaryKey},ssl=True'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
