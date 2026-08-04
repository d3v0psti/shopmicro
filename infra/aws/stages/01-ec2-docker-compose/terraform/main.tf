data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
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
  compose_base64 = base64encode(file("${path.module}/templates/compose.yaml"))
}
