# infrastructure/tests/pester/Secrets.Tests.ps1
# Pester v5 — validates that no plaintext secret values are exposed in App Service
#             app_settings API responses
#
# Inputs (from environment variables — never hardcode):
#   $env:APP_SERVICE_NAME    — from: terraform output -raw app_service_name (core/)
#   $env:RESOURCE_GROUP_NAME — from: terraform output -raw resource_group_name (core/)
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/Secrets.Tests.ps1

BeforeAll {
    if (-not $env:APP_SERVICE_NAME)    { throw "APP_SERVICE_NAME env var is required" }
    if (-not $env:RESOURCE_GROUP_NAME) { throw "RESOURCE_GROUP_NAME env var is required" }

    $settings = az webapp config appsettings list `
        --name $env:APP_SERVICE_NAME `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --output json | ConvertFrom-Json
}

Describe "App Service — Secrets not plaintext in response" {
    It "Jwt__SigningKey value is not the development placeholder" {
        $jwtSetting = $settings | Where-Object { $_.name -eq "Jwt__SigningKey" }
        $jwtSetting.value | Should -Not -Match "DEV-ONLY-KEY|REPLACE_WITH|placeholder|changeme"
    }

    It "App settings do not contain a raw SQL password" {
        # Connection string lives in the connection_string block, not app_settings.
        # This test confirms no one accidentally duplicated it as a plain app setting.
        $settings.value | Should -Not -Match "Password=P@|Password=p@|Password=Test"
    }
}
