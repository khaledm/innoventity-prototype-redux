# environments/dev/data/main.tf
#
# Stateful resources: Resource Group + SQL Server + SQL Database
# State: tfstate-dev-data (see backend.tf)
#
# IMPORTANT — Split-State Design:
#   This root module owns the resource group AND the database.
#   Destroying environments/dev/core/ (App Service, App Insights) never touches this state.
#
#   To intentionally destroy this layer:
#     1. Edit modules/sql-database/main.tf — comment out lifecycle { prevent_destroy = true }
#     2. cd environments/dev/data && terraform destroy
#   This is an irreversible, change-controlled operation. Do NOT run as part of routine teardown.
#
# Apply order:
#   1. terraform apply (this root module first — creates RG + SQL)
#   2. terraform output -raw connection_string   (copy output)
#   3. cd ../core && terraform apply -var connection_string="<paste>"

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
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
  default = "East US"
}

variable "sql_database_sku" {
  type        = string
  description = "Database SKU (Basic for dev, S2 for prod)"
  default     = "Basic"
}

variable "sql_admin_password" {
  type        = string
  sensitive   = true
  description = "SQL Server admin password — inject via CI/CD secret or local tfvars; never commit"
}

# ─────────────────────────────────────────────────────────────────
# Resource Group — owned by data/ layer
# Persists across core/ recreations. Destroying core/ does NOT touch this.
# ─────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "main" {
  name     = "innoventity-${var.environment}-rg"
  location = var.location

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# SQL Database module
# ─────────────────────────────────────────────────────────────────

module "sql" {
  source              = "../../../modules/sql-database"
  environment         = var.environment
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  sku_name            = var.sql_database_sku
  admin_password      = var.sql_admin_password

  depends_on = [azurerm_resource_group.main]
}

# ─────────────────────────────────────────────────────────────────
# Outputs
# ─────────────────────────────────────────────────────────────────

# Pass this to core/ apply:  terraform output -raw connection_string
output "connection_string" {
  value       = module.sql.connection_string
  sensitive   = true
  description = "SQL connection string — copy to core/ apply as -var connection_string=..."
}

output "resource_group_name" {
  value       = azurerm_resource_group.main.name
  description = "Resource group name"
}

output "server_fqdn" {
  value       = module.sql.server_fqdn
  description = "SQL Server fully-qualified domain name"
}
