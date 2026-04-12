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
	"crypto/rand"
	"encoding/base64"
	"fmt"
	"os"
	"strings"
	"testing"

	"github.com/gruntwork-io/terratest/modules/azure"
	"github.com/gruntwork-io/terratest/modules/random"
	"github.com/gruntwork-io/terratest/modules/terraform"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
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
			"connection_string": fmt.Sprintf("Server=tcp:placeholder.database.windows.net,1433;Database=Innoventity;User ID=innoventity-admin;Password=%s;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;", getTestSQLPassword(t)),
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

// TestDevEnvironmentAppServiceInfra provisions both layers (data + core) and validates
// that the App Service is reachable and correctly configured via the Azure SDK.
//
// D1=OptionA: HTTP /health assertion removed — the app container is not deployed in this
// Terraform-only run, so the health endpoint is not expected to return 200. Infrastructure
// correctness (HTTPS-only, TLS, resource existence) is asserted via azure.GetAppService.
//
// Run time: ~5-10 minutes (Azure provisioning).
func TestDevEnvironmentAppServiceInfra(t *testing.T) {
	t.Parallel()

	uniqueID := strings.ToLower(random.UniqueId())
	testEnv := "test-" + uniqueID
	resourceGroup := fmt.Sprintf("innoventity-%s-rg", testEnv)

	// Step 1: Apply data layer (resource group + SQL)
	dataOptions := &terraform.Options{
		TerraformDir: "../../environments/dev/data",
		Vars: map[string]interface{}{
			"environment":        testEnv,
			"sql_database_sku":   "Basic",
			"sql_admin_password": getTestSQLPassword(t),
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

	// Non-tautological: query Azure SDK — asserts correct infra config, not tautological output comparison
	appService := azure.GetAppService(t, appServiceName, resourceGroup, "")
	require.NotNil(t, appService,
		"App Service must be provisioned and retrievable via the Azure SDK")
	assert.True(t, *appService.HTTPSOnly,
		"App Service must enforce HTTPS-only (https_only = true)")
	assert.Equal(t, "1.2", string(appService.SiteConfig.MinTLSVersion),
		"App Service must require TLS 1.2 minimum")
}

// getTestSQLPassword returns the SQL admin password for Terratest runs.
// It reads TERRATEST_SQL_ADMIN_PASSWORD from the environment so that CI pipelines
// can supply a secret without committing credentials. When the variable is absent
// (local runs without a configured secret) a cryptographically random base64 value
// is generated and used for that test run only; it is never stored anywhere.
func getTestSQLPassword(t *testing.T) string {
	t.Helper()
	if pw := os.Getenv("TERRATEST_SQL_ADMIN_PASSWORD"); pw != "" {
		return pw
	}
	return generateRandomBase64(t, 16)
}

// generateRandomBase64 generates a cryptographically random base64-encoded string
// from n random bytes.  For n=32 the output is 44 characters — the minimum length
// required by the jwt_secret_key Terraform variable validation (≥44 chars, HS256).
func generateRandomBase64(t *testing.T, n int) string {
	t.Helper()
	buf := make([]byte, n)
	_, err := rand.Read(buf)
	if err != nil {
		t.Fatalf("generateRandomBase64: crypto/rand.Read failed: %v", err)
	}
	return base64.StdEncoding.EncodeToString(buf)
}
