# infrastructure/tests/pester/AppService.Tests.ps1
# Pester v5 — validates live App Service configuration after terraform apply
#
# Inputs (from environment variables — never hardcode):
#   $env:APP_SERVICE_NAME    — from: terraform output -raw app_service_name (core/)
#   $env:RESOURCE_GROUP_NAME — from: terraform output -raw resource_group_name (core/)
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/AppService.Tests.ps1

BeforeAll {
    if (-not $env:APP_SERVICE_NAME)    { throw "APP_SERVICE_NAME env var is required" }
    if (-not $env:RESOURCE_GROUP_NAME) { throw "RESOURCE_GROUP_NAME env var is required" }

    $script:app = az webapp show `
        --name $env:APP_SERVICE_NAME `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --query "{httpsOnly:httpsOnly,ftpsState:siteConfig.ftpsState,minTls:siteConfig.minTlsVersion,alwaysOn:siteConfig.alwaysOn,planId:serverFarmId}" `
        --output json | ConvertFrom-Json

    # Fetch App Service Plan SKU — needed for the alwaysOn conditional assertion.
    # always_on is intentionally false on B1 (Basic tier does not support it);
    # on all other SKUs, alwaysOn = false is an availability misconfiguration causing cold starts.
    # Terraform module: always_on = var.sku_name != "B1" ? true : false
    $script:appSku = az appservice plan show `
        --ids $script:app.planId `
        --query "sku.name" `
        --output tsv
}

Describe "App Service Configuration" {
    It "enforces HTTPS-only (httpsOnly = true)" {
        $script:app.httpsOnly | Should -Be $true
    }

    It "disables FTP (ftpsState = Disabled)" {
        $script:app.ftpsState | Should -Be "Disabled"
    }

    It "enforces minimum TLS 1.2" {
        $script:app.minTls | Should -Be "1.2"
    }

    It "alwaysOn is false on B1, true on non-B1 SKUs (availability misconfiguration guard)" {
        # B1 (Basic tier) does not support always_on — Terraform intentionally sets it false.
        # On any other SKU, alwaysOn = false causes cold starts and is an active misconfiguration.
        # $script:appSku is fetched from az appservice plan show in BeforeAll.
        if ($script:appSku -eq "B1") {
            $script:app.alwaysOn | Should -Be $false `
                -Because "B1 SKU does not support always_on; Terraform sets it false intentionally"
        } else {
            $script:app.alwaysOn | Should -Be $true `
                -Because "always_on must be enabled on $($script:appSku) SKU to prevent cold starts (availability misconfiguration)"
        }
    }
}

Describe "App Service — Required App Settings (value-liveness)" {
    BeforeAll {
        $script:settings = az webapp config appsettings list `
            --name $env:APP_SERVICE_NAME `
            --resource-group $env:RESOURCE_GROUP_NAME `
            --output json | ConvertFrom-Json
    }

    It "ASPNETCORE_ENVIRONMENT matches the target environment (prod -> Production, all others -> Development)" {
        # Exact-match per environment: a dev environment misconfigured as 'Production' changes
        # logging verbosity, error detail exposure, HTTPS redirection, and developer-exception pages.
        # Requires $env:ENVIRONMENT to be set by validate-environment.ps1 (Fix 3).
        $s = $script:settings | Where-Object { $_.name -eq "ASPNETCORE_ENVIRONMENT" }
        $s | Should -Not -BeNullOrEmpty
        $expected = if ($env:ENVIRONMENT -eq "prod") { "Production" } else { "Development" }
        $s.value | Should -Be $expected `
            -Because "ASPNETCORE_ENVIRONMENT must be '$expected' for the '$env:ENVIRONMENT' environment"
    }

    It "APPLICATIONINSIGHTS_CONNECTION_STRING is non-empty and has a valid AI format" {
        $s = $script:settings | Where-Object { $_.name -eq "APPLICATIONINSIGHTS_CONNECTION_STRING" }
        $s | Should -Not -BeNullOrEmpty
        $s.value | Should -Not -BeNullOrEmpty
        # Connection strings use either legacy InstrumentationKey= or new format with EndpointSuffix=
        $s.value | Should -Match "(?i)(instrumentationkey|endpointSuffix)="
    }

    It "Jwt__SigningKey is non-empty and at least 44 chars (32-byte base64 minimum for HS256)" {
        # Defence-in-depth: Terraform variable validation is the primary control.
        # This assertion catches portal-originated drift (key deleted and re-added with blank/weak value).
        $s = $script:settings | Where-Object { $_.name -eq "Jwt__SigningKey" }
        $s | Should -Not -BeNullOrEmpty
        $s.value | Should -Not -BeNullOrEmpty
        $s.value.Length | Should -BeGreaterOrEqual 44
        $s.value | Should -Match "^[A-Za-z0-9+/]{43,}={0,2}$"
    }

    It "Jwt__Issuer is a well-formed HTTPS azurewebsites.net URL" {
        $s = $script:settings | Where-Object { $_.name -eq "Jwt__Issuer" }
        $s | Should -Not -BeNullOrEmpty
        $s.value | Should -Match "^https://innoventity-.*\.azurewebsites\.net$"
    }

    It "Jwt__Audience matches Jwt__Issuer (self-audience pattern required by JwtTokenService.cs)" {
        $issuer   = ($script:settings | Where-Object { $_.name -eq "Jwt__Issuer" }).value
        $audience = ($script:settings | Where-Object { $_.name -eq "Jwt__Audience" }).value
        $audience | Should -Not -BeNullOrEmpty
        $audience | Should -Be $issuer
    }

    It "DefaultConnection does NOT appear in app_settings (must be in connection_string block only)" {
        # If this key exists here, a raw SQL password is visible in the appsettings API response.
        $leak = $script:settings | Where-Object { $_.name -match "(?i)(DefaultConnection|ConnectionStrings__)" }
        $leak | Should -BeNullOrEmpty
    }
}
