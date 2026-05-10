# modules/static-web-app/variables.tf

variable "name" {
  type        = string
  description = "Name of the Azure Static Web Apps resource"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group (created by data/ layer)"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "sku_tier" {
  type        = string
  description = "SWA pricing tier: Free or Standard"
  default     = "Free"

  validation {
    condition     = contains(["Free", "Standard"], var.sku_tier)
    error_message = "sku_tier must be 'Free' or 'Standard'."
  }
}

variable "tags" {
  type        = map(string)
  description = "Tags to apply to the SWA resource"
  default     = {}
}
