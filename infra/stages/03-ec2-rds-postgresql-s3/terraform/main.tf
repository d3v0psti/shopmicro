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

data "aws_route_table" "default_main" {
  filter {
    name   = "association.main"
    values = ["true"]
  }

  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

data "aws_ssm_parameter" "amazon_linux_2023_ami" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64"
}

locals {
  name                   = "${var.project_name}-${var.environment}"
  subnet_id              = sort(data.aws_subnets.default_supported.ids)[0]
  bucket_name            = "${local.name}-uploads-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  rds_password_parameter = "/${var.project_name}/${var.environment}/rds/password"
  compose_base64         = base64encode(file("${path.module}/templates/compose.yaml"))
}
