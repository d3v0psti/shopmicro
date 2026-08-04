resource "aws_security_group" "ec2" {
  name        = "${local.name}-ec2"
  description = "Acesso restrito ao estagio 02 do ShopMicro"
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
    description = "Saida necessaria para pacotes, Git, imagens Docker e AWS APIs"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${local.name}-ec2" }
}
