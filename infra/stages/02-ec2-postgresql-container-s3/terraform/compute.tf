resource "aws_instance" "shopmicro" {
  ami                         = data.aws_ssm_parameter.amazon_linux_2023_ami.value
  instance_type               = var.instance_type
  subnet_id                   = local.subnet_id
  associate_public_ip_address = true
  vpc_security_group_ids      = [aws_security_group.ec2.id]
  iam_instance_profile        = aws_iam_instance_profile.ec2.name

  user_data = templatefile("${path.module}/templates/user-data.sh.tftpl", {
    aws_region        = var.aws_region
    bucket_name       = aws_s3_bucket.uploads.id
    compose_base64    = local.compose_base64
    repository_url    = var.repository_url
    repository_branch = var.repository_branch
  })
  user_data_replace_on_change = true

  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 2
  }

  root_block_device {
    volume_size           = var.root_volume_size
    volume_type           = "gp3"
    encrypted             = true
    delete_on_termination = true
  }

  tags = { Name = "${local.name}-ec2" }

  depends_on = [
    aws_iam_role_policy.uploads,
    aws_iam_role_policy_attachment.ssm,
    aws_s3_bucket_policy.uploads,
    aws_vpc_endpoint.s3
  ]
}
