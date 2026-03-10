# Azure Bicep Deployment

## Deploy
```bash
az group create -n vectorscale-rg -l eastus
az deployment group create -g vectorscale-rg -f main.bicep
```

## Outputs
```bash
az deployment group show -g vectorscale-rg -n main --query properties.outputs.apiUrl.value
```

## Cleanup
```bash
az group delete -n vectorscale-rg --yes
```
