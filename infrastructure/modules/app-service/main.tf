# modules/app-service/main.tf
# Azure App Service Plan + Linux Web App + (prod) staging slot
#
# App Service Configuration keys are mapped to ASP.NET Core config using __ as separator:
#   Jwt__SigningKey            → IConfiguration["Jwt:SigningKey"]
#   Jwt__Issuer                → IConfiguration["Jwt:Issuer"]
#   Jwt__Audience              → IConfiguration["Jwt:Audience"]
#   APPLICATIONINSIGHTS_CONNECTION_STRING → built-in AI SDK auto-pickup key
# Connection string is injected via the connection_string block (type SQLAzure) which
# ASP.NET Core reads via GetConnectionString("DefaultConnection").
#
# ⚠️ SendGrid / email keys are NOT configured here — deferred to Phase 1+.

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
  description = "Name of the resource group (created by data/ layer)"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "sku_name" {
  type        = string
  description = "App Service Plan SKU (B1, S1, P1v3)"
}

variable "connection_string" {
  type        = string
  sensitive   = true
  description = "SQL Database connection string — sourced from environments/{env}/data/ output"
}

variable "jwt_secret_key" {
  type        = string
  sensitive   = true
  description = "JWT signing key (base64-encoded, ≥32 bytes) — injected from CI/CD secrets; never stored in source control"

  # Primary control: enforce key strength at plan time so no Azure resource is created
  # with a weak or empty signing key. 44 chars = 32 bytes base64-encoded (minimum for HS256).
  # Generate: [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
  validation {
    condition     = length(var.jwt_secret_key) >= 44
    error_message = "jwt_secret_key must be at least 32 bytes (44 base64 chars). Generate with: [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))"
  }

  validation {
    condition     = can(regex("^[A-Za-z0-9+/]{43,}={0,2}$", var.jwt_secret_key))
    error_message = "jwt_secret_key must be a valid base64 string. Avoid plaintext passwords or dev placeholder values."
  }
}

variable "application_insights_key" {
  type        = string
  sensitive   = true
  description = "Application Insights connection string — sourced from monitoring module output"
}

variable "log_analytics_workspace_id" {
  type        = string
  description = "Log Analytics Workspace resource ID for diagnostic settings (FR7.6 MUST). Pass module.monitoring.workspace_id from the calling root module."
}

# ─────────────────────────────────────────────────────────────────
# App Service Plan
# ─────────────────────────────────────────────────────────────────

resource "azurerm_service_plan" "main" {
  name                = "innoventity-${var.environment}-asp"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# Linux Web App
# ─────────────────────────────────────────────────────────────────

resource "azurerm_linux_web_app" "main" {
  name                = "innoventity-${var.environment}-api"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.main.id

  # Force HTTPS — all HTTP traffic redirected to HTTPS (TLS 1.2 minimum)
  https_only = true

  site_config {
    # always_on is not supported on Basic (B1) tier
    always_on           = var.sku_name != "B1" ? true : false
    ftps_state          = "Disabled"
    http2_enabled       = true
    minimum_tls_version = "1.2"

    application_stack {
      dotnet_version = "8.0"
    }

    cors {
      allowed_origins = var.environment == "dev" ? [
        "http://localhost:4200"
        ] : [
        "https://innoventity-${var.environment}-web.azurestaticapps.net"
      ]
      support_credentials = true
    }
  }

  # App settings injected as environment variables.
  # ASP.NET Core maps __ to : for nested config sections.
  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                = var.environment == "prod" ? "Production" : "Development"
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = var.application_insights_key

    # JWT — keys match IConfiguration["Jwt:SigningKey"] etc. in JwtTokenService.cs
    "Jwt__SigningKey"                  = var.jwt_secret_key
    "Jwt__Issuer"                      = "https://innoventity-${var.environment}-api.azurewebsites.net"
    "Jwt__Audience"                    = "https://innoventity-${var.environment}-api.azurewebsites.net"
    "Jwt__AccessTokenExpirationMinutes" = "60"
    "Jwt__RefreshTokenExpirationDays"  = "7"

    # ⚠️ SendGrid keys deferred to Phase 1+ (email notifications not yet implemented)
    # "SendGrid__ApiKey" = var.sendgrid_api_key  # Add when Phase 1 begins
  }

  # SQL connection string — read by ASP.NET Core via GetConnectionString("DefaultConnection")
  # App Service injects this as SQLAZURECONNSTR_DefaultConnection which the runtime maps correctly.
  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = var.connection_string
  }

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# Staging slot — prod only, used by deploy.yml slot-swap job
# ─────────────────────────────────────────────────────────────────

resource "azurerm_linux_web_app_slot" "staging" {
  count          = var.environment == "prod" ? 1 : 0
  name           = "staging"
  app_service_id = azurerm_linux_web_app.main.id
  https_only     = true

  site_config {
    always_on           = true
    ftps_state          = "Disabled"  # H2: security parity with production slot
    minimum_tls_version = "1.2"       # H2: security parity with production slot
    http2_enabled       = true        # H2: security parity with production slot

    application_stack {
      dotnet_version = "8.0"
    }
  }

  # Staging shares the same app settings as production — validated here before swap
  app_settings = azurerm_linux_web_app.main.app_settings

  tags = {
    Environment = "${var.environment}-staging"
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

# ─────────────────────────────────────────────────────────────────
# Azure Monitor Diagnostic Settings (FR7.6 MUST)
# Streams App Service HTTP, console, and application logs + AllMetrics to Log Analytics.
# ─────────────────────────────────────────────────────────────────

resource "azurerm_monitor_diagnostic_setting" "app_service" {
  name                       = "innoventity-${var.environment}-app-diag"
  target_resource_id         = azurerm_linux_web_app.main.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "AppServiceHTTPLogs"
  }

  enabled_log {
    category = "AppServiceConsoleLogs"
  }

  enabled_log {
    category = "AppServiceAppLogs"
  }

  enabled_log {
    category = "AppServiceAuditLogs"
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}

# ─────────────────────────────────────────────────────────────────
# Outputs
# ─────────────────────────────────────────────────────────────────

output "default_hostname" {
  value       = azurerm_linux_web_app.main.default_hostname
  description = "App Service default hostname (without https://) — used for health checks and Pester tests"
}

output "app_service_name" {
  value       = azurerm_linux_web_app.main.name
  description = "App Service resource name — passed as APP_SERVICE_NAME env var to Pester suite"
}

output "staging_hostname" {
  value       = var.environment == "prod" ? azurerm_linux_web_app_slot.staging[0].default_hostname : null
  description = "Staging slot hostname (prod only)"
}
