resource "random_password" "database" {
  length  = 24
  special = false
}

resource "random_password" "jwt" {
  length  = 64
  special = false
}

resource "aws_db_instance" "postgres" {
  identifier                 = "${local.name}-postgres"
  engine                     = "postgres"
  engine_version             = "16"
  instance_class             = var.db_instance_class
  allocated_storage          = 20
  max_allocated_storage      = 100
  storage_type               = "gp3"
  storage_encrypted          = true
  db_name                    = "shopdb"
  username                   = "shopadmin"
  password                   = random_password.database.result
  port                       = 5432
  db_subnet_group_name       = aws_db_subnet_group.main.name
  vpc_security_group_ids     = [aws_security_group.database.id]
  publicly_accessible        = false
  multi_az                   = var.db_multi_az
  backup_retention_period    = 7
  auto_minor_version_upgrade = true
  deletion_protection        = var.protect_database
  skip_final_snapshot        = !var.protect_database
  final_snapshot_identifier  = var.protect_database ? "${local.name}-final" : null
  apply_immediately          = true
}

resource "aws_secretsmanager_secret" "database_connection" {
  name                    = "${local.name}/database-connection"
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "database_connection" {
  secret_id     = aws_secretsmanager_secret.database_connection.id
  secret_string = "Host=${aws_db_instance.postgres.address};Port=5432;Database=shopdb;Username=shopadmin;Password=${random_password.database.result};SSL Mode=Require;Trust Server Certificate=true"
}

resource "aws_secretsmanager_secret" "jwt" {
  name                    = "${local.name}/jwt-secret"
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "jwt" {
  secret_id     = aws_secretsmanager_secret.jwt.id
  secret_string = random_password.jwt.result
}

resource "aws_efs_file_system" "uploads" {
  encrypted       = true
  throughput_mode = "bursting"
  tags            = { Name = "${local.name}-uploads" }
}

resource "aws_efs_mount_target" "uploads" {
  count           = 2
  file_system_id  = aws_efs_file_system.uploads.id
  subnet_id       = aws_subnet.public[count.index].id
  security_groups = [aws_security_group.efs.id]
}

resource "aws_efs_access_point" "uploads" {
  file_system_id = aws_efs_file_system.uploads.id
  posix_user {
    gid = 1000
    uid = 1000
  }
  root_directory {
    path = "/uploads"
    creation_info {
      owner_gid   = 1000
      owner_uid   = 1000
      permissions = "0755"
    }
  }
}
