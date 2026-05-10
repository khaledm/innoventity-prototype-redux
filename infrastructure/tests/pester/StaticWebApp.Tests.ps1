# infrastructure/tests/pester/StaticWebApp.Tests.ps1
# Pester v5 — validates Azure Static Web App infrastructure provisioning
#
# Purpose: Infrastructure-level validation after terraform apply.
# Tests ONLY resource provisioning (SKU, location, tags, etc.).
# Application-level concerns (security headers, routing) are validated
# in DeploymentValidation.Tests.ps1 after deploy-frontend.yml completes.
#
# Inputs (from environment variables — never hardcode):
#   $env:SWA_NAME            — from: innoventity-<environment>-web (derived in infra.yml)
#   $env:RESOURCE_GROUP_NAME — from: terraform output -raw resource_group_name (core/)
#   $env:ENVIRONMENT         — set by infra.yml (dev/test/prod)
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/StaticWebApp.Tests.ps1

BeforeAll {
    if (-not $env:SWA_NAME)            { throw "SWA_NAME env var is required" }
    if (-not $env:RESOURCE_GROUP_NAME) { throw "RESOURCE_GROUP_NAME env var is required" }

    $script:swa = az staticwebapp show `
        --name $env:SWA_NAME `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --query "{name:name, sku:sku.name, tier:sku.tier, location:location, defaultHostname:defaultHostname}" `
        --output json | ConvertFrom-Json
}

Describe "Static Web App — Provisioning" {
    It "resource exists in the expected resource group" {
        $script:swa | Should -Not -BeNullOrEmpty `
            -Because "az staticwebapp show must return a result for '$env:SWA_NAME' in '$env:RESOURCE_GROUP_NAME'"
        $script:swa.name | Should -Be $env:SWA_NAME
    }

    It "SKU tier is Free (spec requirement: Free tier for dev)" {
        # spec.md §FR-SWA-01: Free tier for all non-prod environments.
        # Standard tier costs ~$9/month — a misconfiguration would incur unexpected charges.
        $script:swa.tier | Should -Be "Free" `
            -Because "Static Web App must use Free tier in $env:ENVIRONMENT (spec FR-SWA-01)"
    }

    It "has a non-empty default hostname" {
        $script:swa.defaultHostname | Should -Not -BeNullOrEmpty `
            -Because "Azure assigns a hostname on creation; empty value indicates provisioning failure"
    }

    It "hostname follows the expected Azure SWA naming pattern" {
        # Azure SWA hostnames are of the form: <unique-id>.azurestaticapps.net
        $script:swa.defaultHostname | Should -Match "\.azurestaticapps\.net$" `
            -Because "SWA default hostname must end with .azurestaticapps.net"
    }
}
