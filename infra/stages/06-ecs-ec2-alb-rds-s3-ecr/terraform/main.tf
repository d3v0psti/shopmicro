data "aws_caller_identity" "current" {}

data "aws_vpc" "default" {
  default = true
}

data "aws_ec2_instance_type_offerings" "selected" {
  filter {
    name   = "instance-type"
    values = [var.instance_type]
  }

  location_type = "availability-zone"
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

data "aws_subnets" "default_supported" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }

  filter {
    name   = "availability-zone"
    values = data.aws_ec2_instance_type_offerings.selected.locations
  }
}

data "aws_route_tables" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

data "aws_ssm_parameter" "ecs_optimized_ami" {
  name = "/aws/service/ecs/optimized-ami/amazon-linux-2023/recommended/image_id"
}

locals {
  name                    = "${var.project_name}-${var.environment}"
  application_subnet_ids  = slice(sort(data.aws_subnets.default_supported.ids), 0, 2)
  bucket_name             = "${local.name}-uploads-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  ecr_registry            = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.aws_region}.amazonaws.com"
  backend_image           = "${data.aws_ecr_repository.shopmicro_backend.repository_url}:latest"
  frontend_image          = "${data.aws_ecr_repository.shopmicro_frontend.repository_url}:latest"
  frontend_admin_image    = "${data.aws_ecr_repository.shopmicro_frontend_admin.repository_url}:latest"
  rds_password_parameter  = "/${var.project_name}/${var.environment}/rds/password"
  jwt_secret_parameter    = "/${var.project_name}/${var.environment}/jwt/secret"
  db_connection_parameter = "/${var.project_name}/${var.environment}/database/connection-string"
}
