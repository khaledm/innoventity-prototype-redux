# infrastructure/tests/pester/DeploymentValidation.Tests.ps1
# Pester v5 — validates deployed Angular application on Azure Static Web Apps
#
# Purpose: Post-deployment validation for application-level concerns.
# Runs AFTER deploy-frontend.yml completes to verify:
#   - Security headers from staticwebapp.config.json are served
#   - SPA routing works correctly
#   - Static assets are accessible
#
# Inputs (from environment variables):
#   $env:SWA_URL — full HTTPS URL to deployed SWA (e.g., https://preview-url.azurestaticapps.net)
#                  For preview: from deploy-preview job output
#                  For production: from SWA default hostname
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/DeploymentValidation.Tests.ps1

BeforeAll {
    if (-not $env:SWA_URL) {
        throw "SWA_URL env var is required (e.g., https://preview-url.azurestaticapps.net)"
    }

    # Normalize URL (remove trailing slash)
    $script:swaUrl = $env:SWA_URL.TrimEnd('/')

    Write-Host "Validating deployment at: $script:swaUrl"

    # Fetch the homepage to get headers
    try {
        $script:response = Invoke-WebRequest `
            -Uri $script:swaUrl `
            -UseBasicParsing `
            -MaximumRedirection 5 `
            -TimeoutSec 30

        $script:headers = $script:response.Headers
        $script:statusCode = $script:response.StatusCode
        Write-Host "  ✓ Homepage returned HTTP $script:statusCode"
    } catch {
        throw "Failed to reach SWA at $script:swaUrl - deployment may not be complete: $_"
    }
}

Describe "Deployment Validation — Security Headers" {
    It "serves X-Content-Type-Options: nosniff" {
        $script:headers["X-Content-Type-Options"] | Should -Be "nosniff" `
            -Because "staticwebapp.config.json must configure X-Content-Type-Options to prevent MIME-sniffing attacks"
    }

    It "serves X-Frame-Options: DENY" {
        $script:headers["X-Frame-Options"] | Should -Be "DENY" `
            -Because "staticwebapp.config.json must configure X-Frame-Options to prevent clickjacking"
    }

    It "serves Strict-Transport-Security header (HSTS)" {
        $script:headers["Strict-Transport-Security"] | Should -Not -BeNullOrEmpty `
            -Because "staticwebapp.config.json must configure HSTS to prevent protocol downgrade attacks"

        # Verify HSTS includes max-age directive
        $script:headers["Strict-Transport-Security"] | Should -Match "max-age=\d+" `
            -Because "HSTS header must include max-age directive"
    }

    It "serves Content-Security-Policy header" {
        $script:headers["Content-Security-Policy"] | Should -Not -BeNullOrEmpty `
            -Because "staticwebapp.config.json must configure CSP to mitigate XSS attacks"

        # Verify CSP includes expected directives
        $csp = $script:headers["Content-Security-Policy"]
        $csp | Should -Match "default-src" -Because "CSP must define default-src"
        $csp | Should -Match "script-src" -Because "CSP must define script-src"
    }
}

Describe "Deployment Validation — Application Availability" {
    It "homepage returns HTTP 200" {
        $script:statusCode | Should -Be 200 `
            -Because "SWA must serve the Angular app successfully"
    }

    It "homepage returns HTML content" {
        $script:response.Headers["Content-Type"] | Should -Match "text/html" `
            -Because "index.html must be served with correct content type"
    }

    It "homepage contains Angular bootstrap" {
        # Verify the response contains Angular's root element
        $script:response.Content | Should -Match '<app-root' `
            -Because "index.html must contain Angular's app-root element"
    }
}

Describe "Deployment Validation — SPA Routing" {
    It "non-existent route falls back to index.html (SPA routing)" {
        # staticwebapp.config.json configures navigationFallback to serve index.html
        # for unknown routes — verify this works for a typical Angular route
        try {
            $response = Invoke-WebRequest `
                -Uri "$script:swaUrl/register" `
                -UseBasicParsing `
                -MaximumRedirection 0 `
                -TimeoutSec 30

            $response.StatusCode | Should -Be 200 `
                -Because "navigationFallback should serve index.html for /register route"

            $response.Content | Should -Match '<app-root' `
                -Because "SPA route must serve the Angular app (index.html)"
        } catch {
            throw "SPA routing failed for /register route: $_"
        }
    }

    It "static assets are excluded from SPA routing fallback" {
        # staticwebapp.config.json excludes /assets/* from navigationFallback
        # Verify a non-existent asset returns 404, not index.html
        try {
            $response = Invoke-WebRequest `
                -Uri "$script:swaUrl/assets/nonexistent.png" `
                -UseBasicParsing `
                -MaximumRedirection 0 `
                -ErrorAction Stop

            # If we get here, the asset path returned 200 — that's wrong
            throw "Expected 404 for nonexistent asset, got $($response.StatusCode)"
        } catch {
            if ($_.Exception.Response.StatusCode -eq 404) {
                # This is the expected behavior — pass the test
                $true | Should -Be $true
            } else {
                throw "Unexpected error fetching nonexistent asset: $_"
            }
        }
    }
}
