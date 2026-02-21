# modules/sql-database/main.tf
# Azure SQL Server + Database
#
# Lives in environments/{env}/data/ — the stateful layer.
# prevent_destroy = true on the database is a hard backstop; routinely-destroyable
# resources live in environments/{env}/core/ (App Service, App Insights).
#
# See infrastructure.md §Split-State Principle for the full design rationale.

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

variable "sku_name" {
  type        = string
  description = "Database SKU (Basic, S0, S1, S2)"
}

variable "max_size_gb" {
  type        = number
  description = "Maximum database size in GB"
  default     = 2
}

variable "admin_password" {
  type        = string
  sensitive   = true
  description = "SQL admin password — injected from CI/CD secrets; never stored in source control"
}

# ─────────────────────────────────────────────────────────────────
# SQL Server
# ─────────────────────────────────────────────────────────────────

resource "azurerm_mssql_server" "main" {
  name                         = "innoventity-${var.environment}-sql"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = "innoventity-admin"
  administrator_login_password = var.admin_password
  minimum_tls_version          = "1.2"

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# Database
# ─────────────────────────────────────────────────────────────────

resource "azurerm_mssql_database" "main" {
  name        = "Innoventity"
  server_id   = azurerm_mssql_server.main.id
  sku_name    = var.sku_name
  max_size_gb = var.max_size_gb

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }

  # Decision (2026-02-20): Production database is persistent — split-state layout.
  # The database lives in environments/{env}/data/ (separate Terraform root module).
  # Destroying environments/{env}/core/ (App Service, App Insights) never touches this state.
  # To intentionally destroy the database:
  #   1. Remove this lifecycle block (or comment it out)
  #   2. cd environments/{env}/data && terraform destroy
  # That operation requires explicit change-control — it is NOT part of any routine workflow.
  lifecycle {
    prevent_destroy = true
  }
}

# ─────────────────────────────────────────────────────────────────
# Firewall rules
# ─────────────────────────────────────────────────────────────────

# Allow Azure services (0.0.0.0/0.0.0.0) — required for App Service connectivity
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Dev only — allow local developer connections.
# ⚠️ Replace 0.0.0.0–255.255.255.255 with your actual developer IP range before applying.
resource "azurerm_mssql_firewall_rule" "allow_local_dev" {
  count            = var.environment == "dev" ? 1 : 0
  name             = "AllowLocalDevelopment"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "255.255.255.255"
}

# ─────────────────────────────────────────────────────────────────
# Outputs
# ─────────────────────────────────────────────────────────────────

output "connection_string" {
  value       = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azurerm_mssql_database.main.name};User ID=${azurerm_mssql_server.main.administrator_login};Password=${var.admin_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  sensitive   = true
  description = "SQL connection string — pass to core/ apply as -var connection_string=\"$(terraform output -raw connection_string)\""
}

output "server_fqdn" {
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
  description = "SQL Server fully-qualified domain name"
}

output "database_name" {
  value       = azurerm_mssql_database.main.name
  description = "Database name"
}
