resource "aws_launch_template" "shopmicro" {
  name_prefix   = "${local.name}-"
  image_id      = data.aws_ssm_parameter.amazon_linux_2023_ami.value
  instance_type = var.instance_type

  iam_instance_profile {
    name = aws_iam_instance_profile.ec2.name
  }

  network_interfaces {
    associate_public_ip_address = true
    security_groups             = [aws_security_group.ec2.id]
  }

  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 2
  }

  block_device_mappings {
    device_name = "/dev/xvda"

    ebs {
      volume_size           = var.root_volume_size
      volume_type           = "gp3"
      encrypted             = true
      delete_on_termination = true
    }
  }

  user_data = base64encode(templatefile("${path.module}/templates/user-data.sh.tftpl", {
    aws_region                  = var.aws_region
    bucket_name                 = aws_s3_bucket.uploads.id
    compose_base64              = local.compose_base64
    jwt_secret_parameter_name   = aws_ssm_parameter.jwt_secret.name
    rds_host                    = aws_db_instance.postgres.address
    rds_password_parameter_name = aws_ssm_parameter.rds_password.name
    repository_url              = var.repository_url
    repository_branch           = var.repository_branch
  }))

  tag_specifications {
    resource_type = "instance"
    tags          = { Name = "${local.name}-ec2" }
  }

  tag_specifications {
    resource_type = "volume"
    tags          = { Name = "${local.name}-ebs" }
  }

  depends_on = [
    aws_db_instance.postgres,
    aws_iam_role_policy.application,
    aws_iam_role_policy_attachment.ssm,
    aws_s3_bucket_policy.uploads,
    aws_vpc_endpoint.s3
  ]
}

resource "aws_autoscaling_group" "shopmicro" {
  name                      = "${local.name}-asg"
  min_size                  = 2
  desired_capacity          = 2
  max_size                  = 2
  vpc_zone_identifier       = local.application_subnet_ids
  target_group_arns         = [aws_lb_target_group.marketplace.arn, aws_lb_target_group.admin.arn]
  health_check_type         = "ELB"
  health_check_grace_period = 900
  min_elb_capacity          = 2
  wait_for_capacity_timeout = "30m"

  launch_template {
    id      = aws_launch_template.shopmicro.id
    version = "$Latest"
  }

  instance_refresh {
    strategy = "Rolling"

    preferences {
      min_healthy_percentage = 50
    }
  }

  tag {
    key                 = "Name"
    value               = "${local.name}-ec2"
    propagate_at_launch = true
  }
}
