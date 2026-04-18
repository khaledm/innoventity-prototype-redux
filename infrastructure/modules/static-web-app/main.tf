# modules/static-web-app/main.tf
#
# Azure Static Web Apps resource for Angular SPA hosting.
#
# Free tier limits:
#   - 1 production environment
#   - 3 preview environments (PR-based)
#   - When 3-preview limit is reached, deploy-preview job fails with a
#     descriptive error — do NOT use continue-on-error (FR-043).
#
# PR preview environments:
#   - Automatically created by Azure/static-web-apps-deploy@v1 on pull_request events
#   - Automatically deleted immediately on PR close/merge (pull_request: [closed])
#   - URL format: https://<swa-name>-<pr-number>.azurestaticapps.net
#
# ⚠️ This module does NOT provision GitHub Actions workflows or secrets.
#    After apply, extract api_key and store as AZURE_STATIC_WEB_APPS_API_TOKEN:
#      terraform output -raw static_web_app_api_key

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.116"
    }
  }
  required_version = ">= 1.6"
}

resource "azurerm_static_web_app" "main" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_tier            = var.sku_tier
  sku_size            = var.sku_tier  # sku_size must match sku_tier for azurerm ~>3.x

  tags = merge(var.tags, {
    ManagedBy = "Terraform"
    Component = "Frontend"
  })
}
