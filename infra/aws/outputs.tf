output "marketplace_url" {
  value = "http://${aws_lb.main.dns_name}"
}

output "aws_region" {
  value = var.aws_region
}

output "admin_url" {
  value = "http://${aws_lb.main.dns_name}:81"
}

output "ecr_repositories" {
  value = { for name, repository in aws_ecr_repository.services : name => repository.repository_url }
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.main.name
}

output "rds_endpoint" {
  value     = aws_db_instance.postgres.address
  sensitive = true
}

output "uploads_bucket_name" {
  description = "Bucket S3 privado usado para imagens de produtos."
  value       = aws_s3_bucket.uploads.id
}
