# modules/monitoring/main.tf
# Log Analytics Workspace + Application Insights
#
# Lives in environments/{env}/core/ (stateless — recreatable without data loss).
# Historical telemetry is lost on recreation but no application data is affected.
# Retention is per-environment: dev/test = 30 days, prod = 90 days.

variable "environment" {
  type        = string
  description = "Environment name (dev, test, prod)"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "retention_days" {
  type        = number
  description = "Log Analytics retention period in days (30 for dev/test, 90 for prod)"
  default     = 30
}

# ─────────────────────────────────────────────────────────────────
# Log Analytics Workspace
# ─────────────────────────────────────────────────────────────────

resource "azurerm_log_analytics_workspace" "main" {
  name                = "innoventity-${var.environment}-logs"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = var.retention_days

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# Application Insights
# ─────────────────────────────────────────────────────────────────

resource "azurerm_application_insights" "main" {
  name                = "innoventity-${var.environment}-appinsights"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# Outputs
# ─────────────────────────────────────────────────────────────────

output "connection_string" {
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
  description = "Application Insights connection string — passed to app-service module as application_insights_key"
}

output "instrumentation_key" {
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
  description = "Application Insights instrumentation key (legacy SDK compatibility)"
}

output "app_insights_name" {
  value       = azurerm_application_insights.main.name
  description = "Application Insights resource name"
}
