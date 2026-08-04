output "instance_id" {
  description = "ID da instância do Stage 03."
  value       = aws_instance.shopmicro.id
}

output "marketplace_url" {
  description = "URL do marketplace."
  value       = "http://${aws_instance.shopmicro.public_ip}"
}

output "admin_url" {
  description = "URL do painel administrativo."
  value       = "http://${aws_instance.shopmicro.public_ip}:81"
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

output "user_data_log_command" {
  description = "Comando para acompanhar o bootstrap dentro da EC2."
  value       = "sudo tail -f /var/log/shopmicro-aws-stage-03.log"
}
