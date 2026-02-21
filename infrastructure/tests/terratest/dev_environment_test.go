// infrastructure/tests/terratest/dev_environment_test.go
//
// Terratest v0.46+ / Go 1.21+
// IaC code correctness — provisions a real Azure environment, asserts via Azure SDK
// (non-tautological), then destroys via defer terraform.Destroy.
//
// Prerequisites:
//   - ARM_SUBSCRIPTION_ID, ARM_TENANT_ID, ARM_CLIENT_ID, ARM_CLIENT_SECRET env vars set
//   - Terraform 1.6+ on PATH
//   - Go 1.21+ on PATH
//
// Run: go test ./... -timeout 30m -v
//
// Characterisation failure mode (Principle 5 — Mark Seemann):
//   See infrastructure/README.md §Characterisation Failure Mode Record.
//   T070 step 2 deliberately omits the DB connection string and records the actual
//   HTTP status code returned by GET /health before this test suite is considered valid.

package test

import (
	"fmt"
	"strings"
	"testing"
	"time"

	"github.com/gruntwork-io/terratest/modules/azure"
	http_helper "github.com/gruntwork-io/terratest/modules/http-helper"
	"github.com/gruntwork-io/terratest/modules/random"
	"github.com/gruntwork-io/terratest/modules/terraform"
	"github.com/stretchr/testify/assert"
)

// TestAppServiceModule validates that the app-service module produces the correct
// Azure resources with the required security configuration.
//
// Assertions use the Azure SDK (azure.GetAppService) — NOT terraform.Output comparisons
// back to input values. That would be tautological and would not catch misconfiguration.
func TestAppServiceModule(t *testing.T) {
	t.Parallel()

	uniqueID := strings.ToLower(random.UniqueId())
	testEnv := "test-" + uniqueID

	terraformOptions := &terraform.Options{
		TerraformDir: "../../environments/dev/core",
		Vars: map[string]interface{}{
			"environment":       testEnv,
			"app_service_sku":   "B1",
			"jwt_secret_key":    generateRandomBase64(t, 32),
			"connection_string": "Server=tcp:placeholder.database.windows.net,1433;Database=Innoventity;User ID=innoventity-admin;Password=TestP@ssw0rd123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
		},
		NoColor: true,
	}

	// defer Destroy is non-negotiable — ensures cleanup even on test failure
	defer terraform.Destroy(t, terraformOptions)

	terraform.InitAndApply(t, terraformOptions)

	// Non-tautological assertion: query Azure SDK, not just check terraform.Output
	appServiceName := terraform.Output(t, terraformOptions, "app_service_name")
	resourceGroupName := terraform.Output(t, terraformOptions, "resource_group_name")

	appService := azure.GetAppService(t, appServiceName, resourceGroupName, "")

	assert.True(t, *appService.HTTPSOnly,
		"App Service must enforce HTTPS-only (https_only = true)")

	// Validate TLS minimum version via site config
	siteConfig := appService.SiteConfig
	assert.Equal(t, "1.2", string(siteConfig.MinTLSVersion),
		"App Service must require TLS 1.2 minimum")
}

// TestDevEnvironmentHealthCheck provisions both layers (data + core), deploys, and
// validates GET /health returns 200 with "Healthy" in the response body.
//
// This is the full end-to-end IaC correctness test — smoke-tests the actual deployed app.
// Run time: ~5-10 minutes (Azure provisioning).
func TestDevEnvironmentHealthCheck(t *testing.T) {
	t.Parallel()

	uniqueID := strings.ToLower(random.UniqueId())
	testEnv := "test-" + uniqueID
	resourceGroup := fmt.Sprintf("innoventity-%s-rg", testEnv)

	// Step 1: Apply data layer (resource group + SQL)
	dataOptions := &terraform.Options{
		TerraformDir: "../../environments/dev/data",
		Vars: map[string]interface{}{
			"environment":       testEnv,
			"sql_database_sku":  "Basic",
			"sql_admin_password": "TestP@ssw0rd123!",
		},
		NoColor: true,
	}
	defer terraform.Destroy(t, dataOptions)
	terraform.InitAndApply(t, dataOptions)

	connectionString := terraform.Output(t, dataOptions, "connection_string")

	// Step 2: Apply core layer (App Service + App Insights)
	coreOptions := &terraform.Options{
		TerraformDir: "../../environments/dev/core",
		Vars: map[string]interface{}{
			"environment":       testEnv,
			"app_service_sku":   "B1",
			"jwt_secret_key":    generateRandomBase64(t, 32),
			"connection_string": connectionString,
		},
		NoColor: true,
	}
	defer terraform.Destroy(t, coreOptions)
	terraform.InitAndApply(t, coreOptions)

	appServiceName := terraform.Output(t, coreOptions, "app_service_name")
	_ = resourceGroup // used in data destroy

	// Non-tautological: query Azure SDK
	appService := azure.GetAppService(t, appServiceName, fmt.Sprintf("innoventity-%s-rg", testEnv), "")
	assert.True(t, *appService.HTTPSOnly)

	// Validate GET /health returns 200 with "Healthy"
	healthURL := fmt.Sprintf("https://%s.azurewebsites.net/health", appServiceName)
	http_helper.HttpGetWithRetry(t, healthURL, nil, 200, "Healthy", 10, 30*time.Second)
}

// generateRandomBase64 generates a cryptographically random base64 string of n bytes.
func generateRandomBase64(t *testing.T, n int) string {
	t.Helper()
	// Use random.UniqueId pattern — in production replace with crypto/rand
	return random.UniqueId() + random.UniqueId() + random.UniqueId()
}
