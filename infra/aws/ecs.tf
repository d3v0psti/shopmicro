resource "aws_ecs_cluster" "main" {
  name = local.name
  setting {
    name  = "containerInsights"
    value = "enabled"
  }
}

resource "aws_cloudwatch_log_group" "services" {
  for_each          = toset(["backend", "frontend", "frontend-admin"])
  name              = "/ecs/${local.name}/${each.key}"
  retention_in_days = 14
}

locals {
  common_log_options = {
    awslogs-region        = var.aws_region
    awslogs-stream-prefix = "ecs"
  }
}

resource "aws_ecs_task_definition" "backend" {
  family                   = "${local.name}-backend"
  requires_compatibilities = ["EC2"]
  network_mode             = "awsvpc"
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  volume {
    name = "uploads"
    efs_volume_configuration {
      file_system_id     = aws_efs_file_system.uploads.id
      transit_encryption = "ENABLED"
      authorization_config { access_point_id = aws_efs_access_point.uploads.id }
    }
  }

  container_definitions = jsonencode([{
    name         = "backend"
    image        = "${aws_ecr_repository.services["backend"].repository_url}:${var.image_tag}"
    essential    = true
    cpu          = 512
    memory       = 512
    portMappings = [{ containerPort = 8080, hostPort = 8080, protocol = "tcp" }]
    environment = [
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "ENABLE_SWAGGER", value = "false" },
      { name = "CORS_ALLOWED_ORIGINS", value = "*" }
    ]
    secrets = [
      { name = "DB_CONNECTION_STRING", valueFrom = aws_secretsmanager_secret.database_connection.arn },
      { name = "JWT_SECRET", valueFrom = aws_secretsmanager_secret.jwt.arn }
    ]
    mountPoints = [{ sourceVolume = "uploads", containerPath = "/app/uploads", readOnly = false }]
    logConfiguration = {
      logDriver = "awslogs"
      options   = merge(local.common_log_options, { awslogs-group = aws_cloudwatch_log_group.services["backend"].name })
    }
  }])
}

resource "aws_ecs_task_definition" "web" {
  for_each = {
    frontend       = aws_ecr_repository.services["frontend"].repository_url
    frontend-admin = aws_ecr_repository.services["frontend-admin"].repository_url
  }
  family                   = "${local.name}-${each.key}"
  requires_compatibilities = ["EC2"]
  network_mode             = "awsvpc"
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn
  container_definitions = jsonencode([{
    name         = each.key
    image        = "${each.value}:${var.image_tag}"
    essential    = true
    cpu          = 256
    memory       = 256
    portMappings = [{ containerPort = 80, hostPort = 80, protocol = "tcp" }]
    environment  = [{ name = "BACKEND_UPSTREAM", value = "http://127.0.0.1:8080" }]
    logConfiguration = {
      logDriver = "awslogs"
      options   = merge(local.common_log_options, { awslogs-group = aws_cloudwatch_log_group.services[each.key].name })
    }
  }])
}

resource "aws_ecs_service" "backend" {
  name                              = "backend"
  cluster                           = aws_ecs_cluster.main.id
  task_definition                   = aws_ecs_task_definition.backend.arn
  desired_count                     = var.service_desired_count
  health_check_grace_period_seconds = 60
  capacity_provider_strategy {
    capacity_provider = aws_ecs_capacity_provider.ec2.name
    weight            = 1
  }
  network_configuration {
    subnets         = aws_subnet.public[*].id
    security_groups = [aws_security_group.backend.id]
  }
  load_balancer {
    target_group_arn = aws_lb_target_group.backend.arn
    container_name   = "backend"
    container_port   = 8080
  }
  depends_on = [aws_lb_listener.marketplace, aws_lb_listener.admin, aws_efs_mount_target.uploads, aws_ecs_cluster_capacity_providers.main]
}

resource "aws_ecs_service" "web" {
  for_each = {
    frontend       = { listener = aws_lb_listener.marketplace.arn, target = aws_lb_target_group.frontend.arn }
    frontend-admin = { listener = aws_lb_listener.admin.arn, target = aws_lb_target_group.admin.arn }
  }
  name                              = each.key
  cluster                           = aws_ecs_cluster.main.id
  task_definition                   = aws_ecs_task_definition.web[each.key].arn
  desired_count                     = var.service_desired_count
  health_check_grace_period_seconds = 30
  capacity_provider_strategy {
    capacity_provider = aws_ecs_capacity_provider.ec2.name
    weight            = 1
  }
  network_configuration {
    subnets         = aws_subnet.public[*].id
    security_groups = [aws_security_group.frontend.id]
  }
  load_balancer {
    target_group_arn = each.value.target
    container_name   = each.key
    container_port   = 80
  }
  depends_on = [aws_lb_listener.marketplace, aws_lb_listener.admin, aws_ecs_cluster_capacity_providers.main]
}
