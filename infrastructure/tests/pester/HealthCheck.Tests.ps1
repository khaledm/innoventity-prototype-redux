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
}

Describe "App Service Health" {
    It "GET /health returns HTTP 200" {
        $response = Invoke-WebRequest `
            -Uri "https://$($env:APP_SERVICE_NAME).azurewebsites.net/health" `
            -UseBasicParsing `
            -ErrorAction Stop
        $response.StatusCode | Should -Be 200
    }

    It "GET /health returns status Healthy in JSON body" {
        $body = Invoke-RestMethod `
            -Uri "https://$($env:APP_SERVICE_NAME).azurewebsites.net/health" `
            -ErrorAction Stop
        $body.status | Should -Be "Healthy"
    }

    It "GET /health body contains a database check entry" {
        $body = Invoke-RestMethod `
            -Uri "https://$($env:APP_SERVICE_NAME).azurewebsites.net/health" `
            -ErrorAction Stop
        $dbCheck = $body.checks | Where-Object { $_.name -eq "database" }
        $dbCheck | Should -Not -BeNullOrEmpty
        $dbCheck.status | Should -Be "Healthy"
    }
}
