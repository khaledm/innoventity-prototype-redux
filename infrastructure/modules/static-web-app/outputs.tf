# modules/static-web-app/outputs.tf

output "default_host_name" {
  value       = azurerm_static_web_app.main.default_host_name
  description = "The default hostname of the Azure Static Web Apps resource (without https://)"
}

output "api_key" {
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
  description = "Deployment token — store as AZURE_STATIC_WEB_APPS_API_TOKEN GitHub secret"
}

output "id" {
  value       = azurerm_static_web_app.main.id
  description = "Resource ID of the Azure Static Web Apps resource"
}
