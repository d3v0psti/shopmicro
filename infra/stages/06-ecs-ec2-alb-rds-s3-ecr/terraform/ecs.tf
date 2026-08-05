resource "aws_ecs_cluster" "shopmicro" {
  name = local.name

  setting {
    name  = "containerInsights"
    value = "disabled"
  }
}

resource "aws_ecs_account_setting_default" "awsvpc_trunking" {
  name  = "awsvpcTrunking"
  value = "enabled"
}

resource "aws_cloudwatch_log_group" "backend" {
  name              = "/ecs/${local.name}/backend"
  retention_in_days = 7
}

resource "aws_cloudwatch_log_group" "frontend" {
  name              = "/ecs/${local.name}/frontend"
  retention_in_days = 7
}

resource "aws_cloudwatch_log_group" "frontend_admin" {
  name              = "/ecs/${local.name}/frontend-admin"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "backend" {
  family                   = "${local.name}-backend"
  network_mode             = "awsvpc"
  requires_compatibilities = ["EC2"]
  execution_role_arn       = aws_iam_role.task_execution.arn
  task_role_arn            = aws_iam_role.backend_task.arn

  container_definitions = jsonencode([
    {
      name              = "backend"
      image             = local.backend_image
      essential         = true
      cpu               = 256
      memoryReservation = 384
      portMappings = [{
        containerPort = 8080
        hostPort      = 8080
        protocol      = "tcp"
      }]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "ASPNETCORE_URLS", value = "http://+:8080" },
        { name = "CORS_ALLOWED_ORIGINS", value = "*" },
        { name = "ENABLE_SWAGGER", value = "true" },
        { name = "STORAGE_PROVIDER", value = "S3" },
        { name = "S3_BUCKET_NAME", value = aws_s3_bucket.uploads.id },
        { name = "AWS_REGION", value = var.aws_region }
      ]
      secrets = [
        { name = "DB_CONNECTION_STRING", valueFrom = aws_ssm_parameter.db_connection.arn },
        { name = "JWT_SECRET", valueFrom = aws_ssm_parameter.jwt_secret.arn }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = aws_cloudwatch_log_group.backend.name
          awslogs-region        = var.aws_region
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_task_definition" "frontend" {
  family                   = "${local.name}-frontend"
  network_mode             = "awsvpc"
  requires_compatibilities = ["EC2"]
  execution_role_arn       = aws_iam_role.task_execution.arn

  container_definitions = jsonencode([
    {
      name              = "frontend"
      image             = local.frontend_image
      essential         = true
      cpu               = 128
      memoryReservation = 128
      portMappings = [{
        containerPort = 80
        hostPort      = 80
        protocol      = "tcp"
      }]
      environment = [{
        name  = "BACKEND_UPSTREAM"
        value = "http://${aws_lb.shopmicro.dns_name}"
      }]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = aws_cloudwatch_log_group.frontend.name
          awslogs-region        = var.aws_region
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_task_definition" "frontend_admin" {
  family                   = "${local.name}-frontend-admin"
  network_mode             = "awsvpc"
  requires_compatibilities = ["EC2"]
  execution_role_arn       = aws_iam_role.task_execution.arn

  container_definitions = jsonencode([
    {
      name              = "frontend-admin"
      image             = local.frontend_admin_image
      essential         = true
      cpu               = 128
      memoryReservation = 128
      portMappings = [{
        containerPort = 80
        hostPort      = 80
        protocol      = "tcp"
      }]
      environment = [{
        name  = "BACKEND_UPSTREAM"
        value = "http://${aws_lb.shopmicro.dns_name}"
      }]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = aws_cloudwatch_log_group.frontend_admin.name
          awslogs-region        = var.aws_region
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_service" "backend" {
  name                              = "${local.name}-backend"
  cluster                           = aws_ecs_cluster.shopmicro.id
  task_definition                   = aws_ecs_task_definition.backend.arn
  desired_count                     = var.service_desired_count
  health_check_grace_period_seconds = 180

  capacity_provider_strategy {
    capacity_provider = aws_ecs_capacity_provider.shopmicro_ec2.name
    weight            = 1
  }

  network_configuration {
    subnets         = local.application_subnet_ids
    security_groups = [aws_security_group.ecs_tasks.id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.backend.arn
    container_name   = "backend"
    container_port   = 8080
  }

  ordered_placement_strategy {
    type  = "spread"
    field = "attribute:ecs.availability-zone"
  }

  depends_on = [
    aws_ecs_account_setting_default.awsvpc_trunking,
    aws_ecs_cluster_capacity_providers.shopmicro,
    aws_lb_listener_rule.frontend_backend,
    aws_lb_listener_rule.admin_backend
  ]
}

resource "aws_ecs_service" "frontend" {
  name                              = "${local.name}-frontend"
  cluster                           = aws_ecs_cluster.shopmicro.id
  task_definition                   = aws_ecs_task_definition.frontend.arn
  desired_count                     = var.service_desired_count
  health_check_grace_period_seconds = 120

  capacity_provider_strategy {
    capacity_provider = aws_ecs_capacity_provider.shopmicro_ec2.name
    weight            = 1
  }

  network_configuration {
    subnets         = local.application_subnet_ids
    security_groups = [aws_security_group.ecs_tasks.id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.frontend.arn
    container_name   = "frontend"
    container_port   = 80
  }

  ordered_placement_strategy {
    type  = "spread"
    field = "attribute:ecs.availability-zone"
  }

  depends_on = [
    aws_ecs_account_setting_default.awsvpc_trunking,
    aws_ecs_cluster_capacity_providers.shopmicro,
    aws_lb_listener.frontend
  ]
}

resource "aws_ecs_service" "frontend_admin" {
  name                              = "${local.name}-frontend-admin"
  cluster                           = aws_ecs_cluster.shopmicro.id
  task_definition                   = aws_ecs_task_definition.frontend_admin.arn
  desired_count                     = var.service_desired_count
  health_check_grace_period_seconds = 120

  capacity_provider_strategy {
    capacity_provider = aws_ecs_capacity_provider.shopmicro_ec2.name
    weight            = 1
  }

  network_configuration {
    subnets         = local.application_subnet_ids
    security_groups = [aws_security_group.ecs_tasks.id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.admin.arn
    container_name   = "frontend-admin"
    container_port   = 80
  }

  ordered_placement_strategy {
    type  = "spread"
    field = "attribute:ecs.availability-zone"
  }

  depends_on = [
    aws_ecs_account_setting_default.awsvpc_trunking,
    aws_ecs_cluster_capacity_providers.shopmicro,
    aws_lb_listener.admin
  ]
}
