# environments/dev/data/backend.tf
# Remote state for dev/data (stateful layer)
#
# ⚠️ Bootstrap requirement: storage account and container must exist before terraform init.
# See environments/dev/core/backend.tf for bootstrap commands.
# Additional container to create:
#   az storage container create --name tfstate-dev-data \
#     --account-name innoventitytfstate

terraform {
  backend "azurerm" {
    resource_group_name  = "innoventity-tfstate-rg"
    storage_account_name = "innoventitytfstate"
    container_name       = "tfstate-dev-data"
    key                  = "platform-core.tfstate"
  }
}
