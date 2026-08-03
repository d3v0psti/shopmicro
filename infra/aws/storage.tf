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

resource "aws_s3_bucket" "uploads" {
  bucket        = "${local.name}-uploads-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  force_destroy = !var.protect_uploads

  tags = { Name = "${local.name}-uploads" }
}

resource "aws_s3_bucket_public_access_block" "uploads" {
  bucket = aws_s3_bucket.uploads.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "uploads" {
  bucket = aws_s3_bucket.uploads.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "uploads" {
  bucket = aws_s3_bucket.uploads.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "uploads" {
  bucket = aws_s3_bucket.uploads.id

  rule {
    id     = "cleanup-incomplete-uploads"
    status = "Enabled"

    filter {}

    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }

    noncurrent_version_expiration {
      noncurrent_days = 30
    }
  }

  depends_on = [aws_s3_bucket_versioning.uploads]
}

resource "aws_s3_bucket_policy" "uploads" {
  bucket = aws_s3_bucket.uploads.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "DenyInsecureTransport"
      Effect    = "Deny"
      Principal = "*"
      Action    = "s3:*"
      Resource = [
        aws_s3_bucket.uploads.arn,
        "${aws_s3_bucket.uploads.arn}/*"
      ]
      Condition = {
        Bool = { "aws:SecureTransport" = "false" }
      }
    }]
  })
}

resource "aws_vpc_endpoint" "s3" {
  vpc_id            = aws_vpc.main.id
  service_name      = "com.amazonaws.${var.aws_region}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids   = [aws_route_table.public.id]

  tags = { Name = "${local.name}-s3" }
}
