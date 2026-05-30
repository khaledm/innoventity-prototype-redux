# environments/dev/core/main.tf
#
# Stateless resources: Application Insights + App Service
# State: tfstate-dev-core (see backend.tf)
#
# IMPORTANT — Split-State Design:
#   This root module does NOT create the resource group or SQL database.
#   Both are owned by environments/dev/data/ (separate state file).
#   Destroying this root module removes App Service + App Insights ONLY.
#   SQL Server, SQL Database, and Resource Group are unaffected.
#
# Workflow:
#   1. Apply data/ first:  cd ../data && terraform apply
#   2. Copy connection string: terraform output -raw connection_string
#   3. Apply this module:  terraform apply -var connection_string="<paste>"
#   See infrastructure/README.md for the full creation workflow.

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.117"
    }
  }
  required_version = ">= 1.6"
}

provider "azurerm" {
  features {}
}

# ─────────────────────────────────────────────────────────────────
# Variables
# ─────────────────────────────────────────────────────────────────

variable "environment" {
  type    = string
  default = "dev"
}

variable "location" {
  type    = string
  default = "West Europe"  # PremiumV3 quota is 0 in North Europe on this VS Professional subscription; West Europe has it available.
}

variable "app_service_sku" {
  type        = string
  description = "App Service Plan SKU"
  default     = "P1v3"  # PremiumV3 — only P0v3/P1v3/P2v3/P3v3 have non-zero quota (limit=30) on this VS Professional subscription.
}

variable "jwt_secret_key" {
  type        = string
  sensitive   = true
  description = "JWT signing key (base64, ≥32 bytes) — provide via tfvars or CI/CD secret; never commit"
}

# Sourced from data/ output: cd ../data && terraform output -raw connection_string
variable "connection_string" {
  type        = string
  sensitive   = true
  description = "SQL connection string from data/ layer"
}

# Sourced from data/ output: cd ../data && terraform output -raw sql_server_id
# Required for SQL Server diagnostic settings (FR7.6). Run after data/ apply.
variable "sql_server_id" {
  type        = string
  default     = null
  description = "SQL Server resource ID from data/ layer — enables diagnostic settings on the SQL Server. Inject at apply time: -var sql_server_id=$(cd ../data && terraform output -raw sql_server_id)"
}

# ─────────────────────────────────────────────────────────────────
# Modules
# ─────────────────────────────────────────────────────────────────

module "monitoring" {
  source              = "../../../modules/monitoring"
  environment         = var.environment
  resource_group_name = "innoventity-${var.environment}-rg"
  location            = var.location
  retention_days      = 30
}

module "app_service" {
  source                   = "../../../modules/app-service"
  environment              = var.environment
  resource_group_name      = "innoventity-${var.environment}-rg"
  location                 = var.location
  sku_name                 = var.app_service_sku
  jwt_secret_key           = var.jwt_secret_key
  connection_string        = var.connection_string
  application_insights_key = module.monitoring.connection_string
  log_analytics_workspace_id = module.monitoring.workspace_id
  # Pass the auto-generated SWA hostname so the App Service CORS allowed_origins
  # list includes the actual production URL (not a predictable naming pattern).
  swa_hostname             = module.static_web_app.default_host_name
}

module "static_web_app" {
  source              = "../../../modules/static-web-app"
  name                = "innoventity-${var.environment}-web"
  resource_group_name = "innoventity-${var.environment}-rg"
  location            = var.location
  sku_tier            = "Free"
  tags = {
    Environment = var.environment
  }
}

# ─────────────────────────────────────────────────────────────────
# Azure Monitor Diagnostic Settings — SQL Server (FR7.6 MUST)
# Created here (not in sql-database module) because the Log Analytics workspace
# lives in core/ state while SQL Server lives in data/ state. Cross-state wiring
# is done by injecting sql_server_id as input variable at apply time.
# ─────────────────────────────────────────────────────────────────

resource "azurerm_monitor_diagnostic_setting" "sql_server" {
  count                      = var.sql_server_id != null ? 1 : 0
  name                       = "innoventity-${var.environment}-sql-diag"
  target_resource_id         = var.sql_server_id
  log_analytics_workspace_id = module.monitoring.workspace_id

  # No enabled_log blocks: SQLSecurityAuditEvents and DevOpsOperationsAudit are
  # conditional categories only available when SQL Server Auditing is explicitly
  # configured via an audit policy. Without one, Azure returns 400 "not supported".

  metric {
    category = "AllMetrics"  # SQL Server only supports "AllMetrics", not "Basic"
    enabled  = true
  }
}

# ─────────────────────────────────────────────────────────────────
# Outputs
# ─────────────────────────────────────────────────────────────────

output "app_service_hostname" {
  value       = module.app_service.default_hostname
  description = "App Service hostname — used in health check: https://<hostname>/health"
}

# APP_SERVICE_NAME — required input for Pester suite (Invoke-Pester -CI)
output "app_service_name" {
  value       = module.app_service.app_service_name
  description = "App Service resource name — set as APP_SERVICE_NAME env var before running Pester"
}

# RESOURCE_GROUP_NAME — required input for Pester suite
output "resource_group_name" {
  value       = "innoventity-${var.environment}-rg"
  description = "Resource group name — set as RESOURCE_GROUP_NAME env var before running Pester"
}

output "appinsights_name" {
  value       = module.monitoring.app_insights_name
  description = "Application Insights resource name"
}

output "static_web_app_hostname" {
  value       = module.static_web_app.default_host_name
  description = "SWA hostname (without https://) — production URL: https://<hostname>"
}

output "static_web_app_api_key" {
  value       = module.static_web_app.api_key
  sensitive   = true
  description = "SWA deployment token — store as AZURE_STATIC_WEB_APPS_API_TOKEN GitHub secret"
}

output "static_web_app_id" {
  value       = module.static_web_app.id
  description = "SWA resource ID"
}
