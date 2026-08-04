resource "aws_security_group" "alb" {
  name        = "${local.name}-alb"
  description = "Entrada publica centralizada no ALB do ShopMicro"
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
    description = "Saida para os targets EC2"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-alb" }
}

resource "aws_security_group" "ec2" {
  name        = "${local.name}-ec2"
  description = "EC2 acessiveis somente pelo ALB do ShopMicro"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description     = "Marketplace vindo do ALB"
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  ingress {
    description     = "Painel administrativo vindo do ALB"
    from_port       = 81
    to_port         = 81
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    description = "Saida para dependencias, AWS APIs, S3 e RDS"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-ec2" }
}

resource "aws_security_group" "rds" {
  name        = "${local.name}-rds"
  description = "PostgreSQL acessivel somente pela EC2 do ShopMicro"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description     = "PostgreSQL vindo somente do SG da EC2"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.ec2.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-rds" }
}
