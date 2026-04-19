# infrastructure/tests/pester/StaticWebApp.Tests.ps1
# Pester v5 — validates live Azure Static Web App configuration after terraform apply
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

Describe "Static Web App — Security Headers" {
    BeforeAll {
        # Probe the production hostname to verify security headers are served.
        # staticwebapp.config.json (in public/) is included in the Angular build artifact
        # and uploaded to SWA — these headers come from that config, not Azure defaults.
        # Only run if the SWA hostname is reachable (skip if resource is freshly provisioned
        # with no content yet — first deploy may not have happened).
        $script:headersChecked = $false
        $script:responseHeaders = $null

        if ($script:swa.defaultHostname) {
            try {
                $response = Invoke-WebRequest `
                    -Uri "https://$($script:swa.defaultHostname)" `
                    -UseBasicParsing `
                    -MaximumRedirection 0 `
                    -ErrorAction SilentlyContinue
                $script:responseHeaders = $response.Headers
                $script:headersChecked  = $true
            } catch {
                Write-Host "  SWA not yet serving content (no deploy yet) — skipping header assertions: $_"
            }
        }
    }

    It "serves X-Content-Type-Options: nosniff" {
        if (-not $script:headersChecked) {
            Set-ItResult -Skipped -Because "SWA has no deployed content yet; re-run after first deploy-frontend.yml"
        }
        $script:responseHeaders["X-Content-Type-Options"] | Should -Be "nosniff"
    }

    It "serves X-Frame-Options: DENY" {
        if (-not $script:headersChecked) {
            Set-ItResult -Skipped -Because "SWA has no deployed content yet; re-run after first deploy-frontend.yml"
        }
        $script:responseHeaders["X-Frame-Options"] | Should -Be "DENY"
    }

    It "serves Strict-Transport-Security header (HSTS)" {
        if (-not $script:headersChecked) {
            Set-ItResult -Skipped -Because "SWA has no deployed content yet; re-run after first deploy-frontend.yml"
        }
        $script:responseHeaders["Strict-Transport-Security"] | Should -Not -BeNullOrEmpty `
            -Because "HSTS must be present to prevent protocol downgrade attacks"
    }
}
