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

Describe "App Service — Secret quality and placement" {
    It "Jwt__SigningKey has a valid base64 shape (≥44 chars — Pester defence-in-depth after Terraform validation)" {
        # Primary control: Terraform variable validation in modules/app-service/main.tf.
        # This assertion catches portal-originated drift: setting deleted and re-added with a blank
        # or placeholder value after terraform apply.
        $jwtSetting = $settings | Where-Object { $_.name -eq "Jwt__SigningKey" }
        $jwtSetting | Should -Not -BeNullOrEmpty
        $jwtSetting.value | Should -Not -BeNullOrEmpty
        $jwtSetting.value.Length | Should -BeGreaterOrEqual 44
        $jwtSetting.value | Should -Match "^[A-Za-z0-9+/]{43,}={0,2}$"
    }

    It "DefaultConnection does not appear in app_settings (must be in connection_string block only)" {
        # If this key exists here, a raw SQL password is visible in the appsettings API response.
        # Connection string must only appear in the dedicated connection_string block (type SQLAzure).
        $leaked = $settings | Where-Object { $_.name -match "(?i)(DefaultConnection|ConnectionStrings__)" }
        $leaked | Should -BeNullOrEmpty
    }

    It "No app setting value embeds a plaintext SQL password" {
        # Iterates each setting individually to avoid PowerShell array-to-string coercion,
        # which causes Should -Not -Match on string[] to join values before comparing.
        foreach ($setting in $settings) {
            $setting.value | Should -Not -Match "(?i)Password=\S{3,}" `
                -Because "Setting '$($setting.name)' should not embed a SQL password in app_settings"
        }
    }
}
