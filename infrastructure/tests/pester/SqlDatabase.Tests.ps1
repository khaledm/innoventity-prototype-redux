# infrastructure/tests/pester/SqlDatabase.Tests.ps1
# Pester v5 — validates live SQL Server firewall and TLS configuration after terraform apply
#
# Inputs (from environment variables — never hardcode):
#   $env:SQL_SERVER_NAME     — from: terraform output -raw sql_server_name (data/)
#   $env:RESOURCE_GROUP_NAME — from: terraform output -raw resource_group_name (data/)
#   $env:ENVIRONMENT         — set by validate-environment.ps1 (dev/test/prod)
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/SqlDatabase.Tests.ps1

BeforeAll {
    if (-not $env:SQL_SERVER_NAME)     { throw "SQL_SERVER_NAME env var is required (source: terraform output -raw sql_server_name in data/)" }
    if (-not $env:RESOURCE_GROUP_NAME) { throw "RESOURCE_GROUP_NAME env var is required" }

    # Explicit env var — no naming-convention inference that silently returns wrong value
    $script:serverName = $env:SQL_SERVER_NAME

    # Fetch both firewall rules and server config in one BeforeAll to avoid redundant CLI calls
    $script:firewallRules = az sql server firewall-rule list `
        --server $script:serverName `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --output json | ConvertFrom-Json

    $script:server = az sql server show `
        --name $script:serverName `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --query "{minTls:minimalTlsVersion}" `
        --output json | ConvertFrom-Json
}

Describe "SQL Server Firewall" {
    It "has AllowAzureServices rule with startIpAddress = 0.0.0.0 and endIpAddress = 0.0.0.0" {
        $azureRule = $script:firewallRules | Where-Object { $_.name -eq "AllowAzureServices" }
        $azureRule | Should -Not -BeNullOrEmpty
        $azureRule.startIpAddress | Should -Be "0.0.0.0"
        $azureRule.endIpAddress   | Should -Be "0.0.0.0"
    }

    It "has no AllowLocalDevelopment rule in non-dev environments" {
        # AllowLocalDevelopment opens 0.0.0.0–255.255.255.255 and is intentional in dev only.
        # This test catches it accidentally surviving a test/prod apply.
        if ($env:ENVIRONMENT -eq "dev") {
            Set-ItResult -Skipped -Because "AllowLocalDevelopment is intentional in dev environments"
        } else {
            $devRule = $script:firewallRules | Where-Object { $_.name -eq "AllowLocalDevelopment" }
            $devRule | Should -BeNullOrEmpty -Because "AllowLocalDevelopment (0.0.0.0-255.255.255.255) must not exist in $env:ENVIRONMENT"
        }
    }

    It "has no wide-open firewall rule (any rule with endIpAddress = 255.255.255.255) in non-dev environments" {
        # Catches any manually-added over-permissive rule, not just AllowLocalDevelopment by name.
        # A wide-open rule allows all public IPs to reach the SQL Server — a critical security gap.
        # infrastructure.md §SQL Database: no rule with endIpAddress = 255.255.255.255 in prod/test.
        if ($env:ENVIRONMENT -eq "dev") {
            Set-ItResult -Skipped -Because "Wide-open firewall access is intentional in dev for local development"
        } else {
            $wideRule = $script:firewallRules | Where-Object { $_.endIpAddress -eq "255.255.255.255" }
            $wideRule | Should -BeNullOrEmpty `
                -Because "No firewall rule should permit all public IPs (endIpAddress = 255.255.255.255) in $env:ENVIRONMENT"
        }
    }
}

Describe "SQL Server TLS" {
    It "enforces minimum TLS 1.2" {
        $script:server.minTls | Should -Be "1.2"
    }
}
