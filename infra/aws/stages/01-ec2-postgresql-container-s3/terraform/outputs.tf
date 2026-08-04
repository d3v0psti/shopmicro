output "instance_id" {
  description = "ID da instância do estágio 01."
  value       = aws_instance.shopmicro.id
}

output "public_ip" {
  description = "IP público da instância."
  value       = aws_instance.shopmicro.public_ip
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

output "user_data_log_command" {
  description = "Comando para acompanhar o bootstrap dentro da EC2."
  value       = "sudo tail -f /var/log/shopmicro-aws-stage-01.log"
}
