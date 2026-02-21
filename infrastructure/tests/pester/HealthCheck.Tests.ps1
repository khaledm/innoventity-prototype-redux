# infrastructure/tests/pester/HealthCheck.Tests.ps1
# Pester v5 — validates live App Service health endpoint after terraform apply + app deploy
#
# Used by:
#   deploy.yml   job: pester-health  (after every app deployment)
#   infra.yml    job: pester-infra   (after terraform apply)
#
# Inputs (from environment variables — never hardcode):
#   $env:APP_SERVICE_NAME — from: terraform output -raw app_service_name (core/)
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/HealthCheck.Tests.ps1

BeforeAll {
    if (-not $env:APP_SERVICE_NAME) { throw "APP_SERVICE_NAME env var is required" }

    $healthUrl   = "https://$($env:APP_SERVICE_NAME).azurewebsites.net/health"
    $maxAttempts = 8   # 8 × 15s = 2 minutes max; covers B1 cold-start latency after fresh deploy
    $attempt     = 0
    $script:statusCode  = 0
    $script:healthBody  = $null

    do {
        try {
            $raw = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -ErrorAction Stop
            $script:statusCode = $raw.StatusCode
            $script:healthBody = $raw.Content | ConvertFrom-Json
            break
        } catch {
            $attempt++
            if ($attempt -ge $maxAttempts) {
                throw "Health endpoint unreachable after $($maxAttempts * 15)s at $healthUrl : $_"
            }
            Write-Host "  Attempt $attempt — not ready ($_), retrying in 15s..."
            Start-Sleep -Seconds 15
        }
    } while ($attempt -lt $maxAttempts)
}

Describe "App Service Health" {
    It "GET /health returns HTTP 200" {
        # Single HTTP call in BeforeAll; all three tests read from shared $script: variables.
        $script:statusCode | Should -Be 200
    }

    It "GET /health returns status Healthy in JSON body" {
        $script:healthBody.status | Should -Be "Healthy"
    }

    It "GET /health body contains a database check entry with status Healthy" {
        # HealthCheck.cs returns checks as Dictionary<string,string>, serialized as a JSON object
        # (not an array). PowerShell deserializes this as PSCustomObject with named properties.
        # Access via property syntax — NOT Where-Object { $_.name -eq "database" }.
        # Validates that HealthCheck.cs wires the database connectivity check correctly.
        $script:healthBody.checks.database | Should -Not -BeNullOrEmpty `
            -Because "HealthCheck.cs must include a 'database' key in the checks dictionary"
        $script:healthBody.checks.database | Should -Be "Healthy"
    }
}
