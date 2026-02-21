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

    $app = az webapp show `
        --name $env:APP_SERVICE_NAME `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --query "{httpsOnly:httpsOnly,ftpsState:siteConfig.ftpsState,minTls:siteConfig.minTlsVersion,http2:siteConfig.http20Enabled}" `
        --output json | ConvertFrom-Json
}

Describe "App Service Configuration" {
    It "enforces HTTPS-only (httpsOnly = true)" {
        $app.httpsOnly | Should -Be $true
    }

    It "disables FTP (ftpsState = Disabled)" {
        $app.ftpsState | Should -Be "Disabled"
    }

    It "enforces minimum TLS 1.2" {
        $app.minTls | Should -Be "1.2"
    }

    It "has HTTP/2 enabled" {
        $app.http2 | Should -Be $true
    }
}

Describe "App Service — Required App Settings (value-liveness)" {
    BeforeAll {
        $script:settings = az webapp config appsettings list `
            --name $env:APP_SERVICE_NAME `
            --resource-group $env:RESOURCE_GROUP_NAME `
            --output json | ConvertFrom-Json
    }

    It "ASPNETCORE_ENVIRONMENT is non-empty and a recognised value" {
        $s = $script:settings | Where-Object { $_.name -eq "ASPNETCORE_ENVIRONMENT" }
        $s | Should -Not -BeNullOrEmpty
        $s.value | Should -BeIn @("Development", "Production", "Staging")
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
