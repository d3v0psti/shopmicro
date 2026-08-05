# Repositórios criados manualmente e preservados fora do lifecycle do Terraform.
data "aws_ecr_repository" "shopmicro_backend" {
  name = var.backend_ecr_repository
}

data "aws_ecr_repository" "shopmicro_frontend" {
  name = var.frontend_ecr_repository
}

data "aws_ecr_repository" "shopmicro_frontend_admin" {
  name = var.frontend_admin_ecr_repository
}
