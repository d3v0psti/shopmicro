resource "aws_launch_template" "shopmicro_ecs" {
  name_prefix   = "${local.name}-ecs-"
  image_id      = data.aws_ssm_parameter.ecs_optimized_ami.value
  instance_type = var.instance_type

  iam_instance_profile { name = aws_iam_instance_profile.ecs_instance.name }

  network_interfaces {
    associate_public_ip_address = true
    security_groups             = [aws_security_group.ecs_instances.id]
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

  user_data = base64encode(<<-EOT
    #!/usr/bin/env bash
    set -e
    echo 'ECS_CLUSTER=${aws_ecs_cluster.shopmicro.name}' >> /etc/ecs/ecs.config
    echo 'ECS_ENABLE_AWSLOGS_EXECUTIONROLE_OVERRIDE=true' >> /etc/ecs/ecs.config
    systemctl enable --now amazon-ssm-agent
    systemctl enable --now ecs
  EOT
  )

  tag_specifications {
    resource_type = "instance"
    tags          = { Name = "${local.name}-ecs" }
  }
  tag_specifications {
    resource_type = "volume"
    tags          = { Name = "${local.name}-ecs-ebs" }
  }
}

resource "aws_autoscaling_group" "shopmicro_ecs" {
  name                  = "${local.name}-ecs-asg"
  min_size              = var.ecs_instance_min_size
  desired_capacity      = var.ecs_instance_min_size
  max_size              = var.ecs_instance_max_size
  vpc_zone_identifier   = local.application_subnet_ids
  protect_from_scale_in = true

  launch_template {
    id      = aws_launch_template.shopmicro_ecs.id
    version = "$Latest"
  }

  tag {
    key                 = "Name"
    value               = "${local.name}-ecs"
    propagate_at_launch = true
  }

  lifecycle {
    ignore_changes = [desired_capacity]
  }
}

resource "aws_ecs_capacity_provider" "shopmicro_ec2" {
  name = "${local.name}-ec2"
  auto_scaling_group_provider {
    auto_scaling_group_arn         = aws_autoscaling_group.shopmicro_ecs.arn
    managed_draining               = "ENABLED"
    managed_termination_protection = "ENABLED"
    managed_scaling {
      status                    = "ENABLED"
      target_capacity           = 80
      minimum_scaling_step_size = 1
      maximum_scaling_step_size = 1
      instance_warmup_period    = 300
    }
  }
}

resource "aws_ecs_cluster_capacity_providers" "shopmicro" {
  cluster_name       = aws_ecs_cluster.shopmicro.name
  capacity_providers = [aws_ecs_capacity_provider.shopmicro_ec2.name]
  default_capacity_provider_strategy {
    capacity_provider = aws_ecs_capacity_provider.shopmicro_ec2.name
    weight            = 1
  }
}
