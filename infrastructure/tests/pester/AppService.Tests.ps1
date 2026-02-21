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

Describe "App Service — Required App Settings" {
    BeforeAll {
        $settings = az webapp config appsettings list `
            --name $env:APP_SERVICE_NAME `
            --resource-group $env:RESOURCE_GROUP_NAME `
            --output json | ConvertFrom-Json
        $settingNames = $settings | ForEach-Object { $_.name }
    }

    It "has ASPNETCORE_ENVIRONMENT set" {
        $settingNames | Should -Contain "ASPNETCORE_ENVIRONMENT"
    }

    It "has APPLICATIONINSIGHTS_CONNECTION_STRING set" {
        $settingNames | Should -Contain "APPLICATIONINSIGHTS_CONNECTION_STRING"
    }

    It "has Jwt__SigningKey set" {
        $settingNames | Should -Contain "Jwt__SigningKey"
    }

    It "has Jwt__Issuer set" {
        $settingNames | Should -Contain "Jwt__Issuer"
    }

    It "has Jwt__Audience set" {
        $settingNames | Should -Contain "Jwt__Audience"
    }
}
