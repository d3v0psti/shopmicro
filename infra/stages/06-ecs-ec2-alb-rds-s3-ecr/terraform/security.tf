resource "aws_security_group" "alb" {
  name        = "${local.name}-alb"
  description = "Entrada publica no ALB"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description = "Marketplace"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = [var.marketplace_cidr]
  }

  ingress {
    description = "Painel administrativo"
    from_port   = 81
    to_port     = 81
    protocol    = "tcp"
    cidr_blocks = [var.admin_cidr]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-alb" }
}

resource "aws_security_group" "ecs_instances" {
  name        = "${local.name}-ecs-instances"
  description = "Hosts ECS sem entrada publica"
  vpc_id      = data.aws_vpc.default.id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-ecs-instances" }
}

resource "aws_security_group" "ecs_tasks" {
  name        = "${local.name}-ecs-tasks"
  description = "Tasks acessiveis somente pelo ALB"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description     = "Frontends pelo ALB"
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  ingress {
    description     = "Backend pelo ALB"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-ecs-tasks" }
}

resource "aws_security_group" "rds" {
  name        = "${local.name}-rds"
  description = "PostgreSQL somente para tasks ECS"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description     = "PostgreSQL pelas tasks ECS"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs_tasks.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-rds" }
}
