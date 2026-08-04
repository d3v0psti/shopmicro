variable "aws_region" {
  description = "Região AWS usada pelo estágio."
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
  default     = "stage-04"
}

variable "marketplace_cidr" {
  description = "CIDR autorizado a acessar o marketplace."
  type        = string
  default     = "0.0.0.0/0"

  validation {
    condition     = can(cidrhost(var.marketplace_cidr, 0))
    error_message = "Informe um CIDR válido."
  }
}

variable "admin_cidr" {
  description = "Seu IP público no formato CIDR /32 para acessar o painel administrativo."
  type        = string

  validation {
    condition     = can(cidrhost(var.admin_cidr, 0)) && var.admin_cidr != "0.0.0.0/0"
    error_message = "Informe um CIDR válido e restrito, por exemplo 203.0.113.10/32."
  }
}

variable "instance_type" {
  description = "Tipo da instância EC2 do estágio."
  type        = string
  default     = "t3.micro"
}

variable "root_volume_size" {
  description = "Tamanho em GiB do volume raiz GP3 da EC2."
  type        = number
  default     = 8

  validation {
    condition     = var.root_volume_size >= 8
    error_message = "O volume raiz deve possuir pelo menos 8 GiB."
  }
}

variable "db_instance_class" {
  description = "Classe de menor custo usada pelo RDS deste treinamento."
  type        = string
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  description = "Armazenamento GP3 do RDS em GiB."
  type        = number
  default     = 20

  validation {
    condition     = var.db_allocated_storage >= 20
    error_message = "O RDS PostgreSQL deve possuir pelo menos 20 GiB."
  }
}

variable "repository_url" {
  description = "Repositório Git clonado pelo user data."
  type        = string
  default     = "https://github.com/d3v0psti/shopmicro.git"
}

variable "repository_branch" {
  description = "Branch clonada pelo user data."
  type        = string
  default     = "develop"
}

variable "force_destroy_bucket" {
  description = "Permite excluir objetos e bucket no terraform destroy deste treinamento."
  type        = bool
  default     = true
}
