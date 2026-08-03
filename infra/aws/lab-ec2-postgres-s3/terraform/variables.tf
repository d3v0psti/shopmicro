variable "aws_region" {
  description = "Região AWS usada pelo laboratório."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Prefixo aplicado aos recursos."
  type        = string
  default     = "shopmicro"
}

variable "environment" {
  description = "Identificação do ambiente."
  type        = string
  default     = "lab"
}

variable "admin_cidr" {
  description = "Seu IP público no formato CIDR /32 para acesso SSH e ao painel."
  type        = string

  validation {
    condition     = can(cidrhost(var.admin_cidr, 0)) && var.admin_cidr != "0.0.0.0/0"
    error_message = "Informe um CIDR válido e restrito, por exemplo 203.0.113.10/32."
  }
}

variable "instance_type" {
  description = "Tipo da instância do laboratório."
  type        = string
  default     = "t3.small"
}

variable "root_volume_size" {
  description = "Tamanho em GiB do volume raiz GP3."
  type        = number
  default     = 25

  validation {
    condition     = var.root_volume_size >= 20
    error_message = "O volume raiz deve possuir pelo menos 20 GiB."
  }
}

variable "repository_url" {
  description = "Repositório Git clonado pelo user-data."
  type        = string
  default     = "https://github.com/d3v0psti/shopmicro.git"
}

variable "repository_branch" {
  description = "Branch clonada pelo user-data."
  type        = string
  default     = "develop"
}

variable "key_name" {
  description = "Key pair opcional. EC2 Instance Connect pode ser usado sem informar este valor."
  type        = string
  default     = null
  nullable    = true
}

variable "force_destroy_bucket" {
  description = "Permite esvaziar e excluir o bucket no terraform destroy. Use somente no laboratório."
  type        = bool
  default     = true
}
