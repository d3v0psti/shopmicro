output "autoscaling_group_name" {
  description = "Nome do Auto Scaling Group do Stage 04."
  value       = aws_autoscaling_group.shopmicro.name
}

output "marketplace_url" {
  description = "URL do marketplace."
  value       = "http://${aws_lb.shopmicro.dns_name}"
}

output "admin_url" {
  description = "URL do painel administrativo."
  value       = "http://${aws_lb.shopmicro.dns_name}:81"
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
  description = "Nome do parâmetro SecureString que contém a senha do RDS."
  value       = aws_ssm_parameter.rds_password.name
}

output "jwt_secret_parameter" {
  description = "Parâmetro SecureString compartilhado pelas duas EC2."
  value       = aws_ssm_parameter.jwt_secret.name
}

output "load_balancer_dns" {
  description = "DNS público do Application Load Balancer."
  value       = aws_lb.shopmicro.dns_name
}

output "user_data_log_command" {
  description = "Comando para acompanhar o bootstrap dentro da EC2."
  value       = "sudo tail -f /var/log/shopmicro-aws-stage-04.log"
}
