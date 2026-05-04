# modules/sql-database/main.tf
# Azure SQL Server + Database
#
# Lives in environments/{env}/data/ — the stateful layer.
# prevent_destroy = true on the database is a hard backstop; routinely-destroyable
# resources live in environments/{env}/core/ (App Service, App Insights).
#
# See infrastructure.md §Split-State Principle for the full design rationale.

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.116"
    }
  }
  required_version = ">= 1.6"
}

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

variable "developer_cidr" {
  type        = string
  default     = null
  description = "Developer IP address for local SQL access (e.g. '203.0.113.42'). Null disables the rule. Set in terraform.tfvars — never commit real IPs. C4 fix."
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

  # H1: server destroy blocked at the Terraform layer.
  # Database lifecycle { prevent_destroy } alone is insufficient — deleting the server
  # cascades and removes the database regardless of the database guard.
  lifecycle {
    prevent_destroy = true
    # Ignore password changes — Azure SQL password is write-only (can't be read back).
    # Terraform always detects drift even when password hasn't changed.
    # Safe because: password is managed via secrets (SQL_ADMIN_PASSWORD), not state.
    ignore_changes = [administrator_login_password]
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
    # Ignore Azure-managed computed attributes that drift between provider versions
    # or are set by Azure backend (not exposed in Terraform state).
    # Common culprits: enclave_type, maintenance_configuration_name, secondary_type
    ignore_changes = [
      enclave_type,                 # Azure sets this based on server config
      maintenance_configuration_name, # Azure-managed attribute
      secondary_type                # Set by Azure for geo-replicated DBs
    ]
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
# C4 fix: rule is created only when environment=dev AND developer_cidr is explicitly provided.
# Null default prevents world-open firewall (0.0.0.0–255.255.255.255) being applied by default.
# Set developer_cidr in terraform.tfvars (never commit). Find your IP: curl -s https://api.ipify.org
resource "azurerm_mssql_firewall_rule" "allow_local_dev" {
  count            = (var.environment == "dev" && var.developer_cidr != null) ? 1 : 0
  name             = "AllowLocalDevelopment"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = var.developer_cidr
  end_ip_address   = var.developer_cidr
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

output "sql_server_name" {
  value       = azurerm_mssql_server.main.name
  description = "SQL Server resource name — set as SQL_SERVER_NAME env var before running Pester"
}

output "sql_server_id" {
  value       = azurerm_mssql_server.main.id
  description = "SQL Server resource ID — required input for diagnostic settings (Wave 2A, FR7.6)"
}
