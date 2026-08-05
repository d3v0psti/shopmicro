output "marketplace_url" {
  description = "URL do marketplace."
  value       = "http://${aws_lb.shopmicro.dns_name}"
}

output "admin_url" {
  description = "URL do painel administrativo."
  value       = "http://${aws_lb.shopmicro.dns_name}:81"
}

output "ecs_cluster_name" {
  description = "Nome do cluster ECS."
  value       = aws_ecs_cluster.shopmicro.name
}

output "ecs_services" {
  description = "Serviços independentes executados pelo ECS."
  value = {
    backend        = aws_ecs_service.backend.name
    frontend       = aws_ecs_service.frontend.name
    frontend_admin = aws_ecs_service.frontend_admin.name
  }
}

output "autoscaling_group_name" {
  description = "Auto Scaling Group das instâncias do cluster ECS."
  value       = aws_autoscaling_group.shopmicro_ecs.name
}

output "bucket_name" {
  description = "Bucket privado usado pelos uploads."
  value       = aws_s3_bucket.uploads.id
}

output "rds_endpoint" {
  description = "Endpoint privado do RDS, sem revelar credenciais."
  value       = aws_db_instance.postgres.address
}

output "rds_password_parameter" {
  description = "Parâmetro SecureString que contém a senha do RDS."
  value       = aws_ssm_parameter.rds_password.name
}

output "ecr_images" {
  description = "Imagens manuais consumidas pelo Stage 06; não são gerenciadas pelo Terraform."
  value = {
    backend        = local.backend_image
    frontend       = local.frontend_image
    frontend_admin = local.frontend_admin_image
  }
}

output "ecs_diagnostics" {
  description = "Comandos para consultar serviços e tasks."
  value = {
    services = "aws ecs describe-services --cluster ${aws_ecs_cluster.shopmicro.name} --services ${aws_ecs_service.backend.name} ${aws_ecs_service.frontend.name} ${aws_ecs_service.frontend_admin.name}"
    tasks    = "aws ecs list-tasks --cluster ${aws_ecs_cluster.shopmicro.name}"
  }
}
