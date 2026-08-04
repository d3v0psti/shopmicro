resource "aws_vpc_endpoint" "s3" {
  vpc_id            = data.aws_vpc.default.id
  service_name      = "com.amazonaws.${var.aws_region}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids   = [data.aws_route_table.default_main.id]

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "RestrictEndpointToStageBucket"
      Effect    = "Allow"
      Principal = "*"
      Action = [
        "s3:GetObject",
        "s3:PutObject"
      ]
      Resource = "${aws_s3_bucket.uploads.arn}/uploads/*"
    }]
  })

  tags = { Name = "${local.name}-s3-endpoint" }
}
