# infrastructure/tests/pester/SqlDatabase.Tests.ps1
# Pester v5 — validates live SQL Server firewall configuration after terraform apply
#
# Inputs (from environment variables — never hardcode):
#   $env:RESOURCE_GROUP_NAME — from: terraform output -raw resource_group_name (data/)
#
# Run: Invoke-Pester -CI ./infrastructure/tests/pester/SqlDatabase.Tests.ps1

BeforeAll {
    if (-not $env:RESOURCE_GROUP_NAME) { throw "RESOURCE_GROUP_NAME env var is required" }

    # Derive server name from resource group convention: innoventity-{env}-rg → innoventity-{env}-sql
    $env_name = $env:RESOURCE_GROUP_NAME -replace "innoventity-(.+)-rg", '$1'
    $serverName = "innoventity-${env_name}-sql"

    $firewallRules = az sql server firewall-rule list `
        --server $serverName `
        --resource-group $env:RESOURCE_GROUP_NAME `
        --output json | ConvertFrom-Json
}

Describe "SQL Server Firewall" {
    It "has AllowAzureServices rule with startIpAddress = 0.0.0.0 and endIpAddress = 0.0.0.0" {
        $azureRule = $firewallRules | Where-Object { $_.name -eq "AllowAzureServices" }
        $azureRule | Should -Not -BeNullOrEmpty
        $azureRule.startIpAddress | Should -Be "0.0.0.0"
        $azureRule.endIpAddress   | Should -Be "0.0.0.0"
    }
}

Describe "SQL Server TLS" {
    BeforeAll {
        $server = az sql server show `
            --name $serverName `
            --resource-group $env:RESOURCE_GROUP_NAME `
            --query "{minTls:minimalTlsVersion}" `
            --output json | ConvertFrom-Json
    }

    It "enforces minimum TLS 1.2" {
        $server.minTls | Should -Be "1.2"
    }
}
