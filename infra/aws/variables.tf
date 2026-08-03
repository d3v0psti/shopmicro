variable "aws_region" {
  description = "Região AWS para todos os recursos."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Prefixo usado nos recursos."
  type        = string
  default     = "shopmicro"
}

variable "environment" {
  description = "Nome do ambiente."
  type        = string
  default     = "dev"
}

variable "image_tag" {
  description = "Tag das imagens publicadas no ECR."
  type        = string
  default     = "latest"
}

variable "service_desired_count" {
  description = "Quantidade de tarefas por serviço. Use 0 no bootstrap e 1 após publicar as imagens."
  type        = number
  default     = 0
  validation {
    condition     = var.service_desired_count >= 0
    error_message = "service_desired_count deve ser zero ou maior."
  }
}

variable "db_instance_class" {
  description = "Classe da instância PostgreSQL."
  type        = string
  default     = "db.t4g.micro"
}

variable "ecs_instance_type" {
  description = "Tipo da instância EC2 que hospeda as tarefas ECS."
  type        = string
  default     = "t3.small"
}

variable "ecs_min_size" {
  description = "Mínimo de instâncias EC2 no cluster."
  type        = number
  default     = 1
}

variable "ecs_max_size" {
  description = "Máximo de instâncias EC2 no cluster."
  type        = number
  default     = 1
}

variable "ecs_desired_capacity" {
  description = "Quantidade inicial de instâncias EC2."
  type        = number
  default     = 1
}

variable "db_multi_az" {
  description = "Habilita RDS Multi-AZ. Aumenta o custo."
  type        = bool
  default     = false
}

variable "protect_database" {
  description = "Protege o banco contra exclusão acidental e cria snapshot final."
  type        = bool
  default     = false
}

variable "protect_uploads" {
  description = "Impede que terraform destroy remova o bucket com os uploads. Habilite em produção."
  type        = bool
  default     = false
}
