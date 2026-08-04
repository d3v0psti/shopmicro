data "aws_caller_identity" "current" {}

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
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

data "aws_ssm_parameter" "ubuntu_ami" {
  name = "/aws/service/canonical/ubuntu/server/24.04/stable/current/amd64/hvm/ebs-gp3/ami-id"
}

locals {
  name           = "${var.project_name}-${var.environment}"
  subnet_id      = sort(data.aws_subnets.default.ids)[0]
  bucket_name    = "${local.name}-uploads-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  compose_base64 = base64encode(file("${path.module}/templates/compose.yaml"))
}
