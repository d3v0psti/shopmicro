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

data "aws_ssm_parameter" "amazon_linux_2023_ami" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64"
}

locals {
  name           = "${var.project_name}-${var.environment}"
  subnet_id      = sort(data.aws_subnets.default_supported.ids)[0]
  compose_base64 = base64encode(file("${path.module}/templates/compose.yaml"))
}
