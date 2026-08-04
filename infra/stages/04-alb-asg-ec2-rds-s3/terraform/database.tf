resource "random_password" "rds" {
  length  = 32
  special = false
}

resource "random_password" "jwt" {
  length  = 64
  special = false
}

resource "aws_ssm_parameter" "rds_password" {
  name        = local.rds_password_parameter
  description = "Senha do RDS usada somente pelo ShopMicro Stage 04"
  type        = "SecureString"
  value       = random_password.rds.result
}

resource "aws_ssm_parameter" "jwt_secret" {
  name        = local.jwt_secret_parameter
  description = "JWT compartilhado pelas EC2 do ShopMicro Stage 04"
  type        = "SecureString"
  value       = random_password.jwt.result
}

resource "aws_db_subnet_group" "shopmicro" {
  name       = "${local.name}-db-subnet-group"
  subnet_ids = sort(data.aws_subnets.default.ids)

  tags = { Name = "${local.name}-db-subnet-group" }
}

resource "aws_db_instance" "postgres" {
  identifier             = "${local.name}-postgres"
  engine                 = "postgres"
  engine_version         = "18"
  instance_class         = var.db_instance_class
  allocated_storage      = var.db_allocated_storage
  storage_type           = "gp3"
  storage_encrypted      = true
  db_name                = "shopdb"
  username               = "shopadmin"
  password               = random_password.rds.result
  port                   = 5432
  publicly_accessible    = false
  multi_az               = false
  db_subnet_group_name   = aws_db_subnet_group.shopmicro.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  backup_retention_period = 0
  deletion_protection     = false
  skip_final_snapshot     = true
  apply_immediately       = true

  tags = { Name = "${local.name}-postgres" }
}
