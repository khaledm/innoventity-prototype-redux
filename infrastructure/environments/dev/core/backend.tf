# environments/dev/core/backend.tf
# Remote state for dev/core (stateless layer)
#
# ⚠️ Bootstrap requirement: storage account and container must exist before terraform init.
# Creation (one-time, manual):
#   az group create --name innoventity-tfstate-rg --location "East US"
#   az storage account create --name innoventitytfstate \
#     --resource-group innoventity-tfstate-rg --sku Standard_LRS
#   az storage container create --name tfstate-dev-core \
#     --account-name innoventitytfstate
#
# This storage account bootstraps itself — once the container exists, Terraform
# can store all future state (including the storage account itself, if imported).

terraform {
  backend "azurerm" {
    resource_group_name  = "innoventity-tfstate-rg"
    storage_account_name = "innoventitytfstate"
    container_name       = "tfstate-dev-core"
    key                  = "platform-core.tfstate"
  }
}
