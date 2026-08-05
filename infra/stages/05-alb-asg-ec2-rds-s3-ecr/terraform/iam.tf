data "aws_iam_policy_document" "ec2_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ec2" {
  name               = "${local.name}-ec2"
  assume_role_policy = data.aws_iam_policy_document.ec2_assume_role.json
}

data "aws_iam_policy_document" "application" {
  statement {
    sid       = "AuthenticateToEcr"
    effect    = "Allow"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid    = "PullApplicationImages"
    effect = "Allow"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:BatchGetImage",
      "ecr:GetDownloadUrlForLayer"
    ]
    resources = [
      data.aws_ecr_repository.shopmicro_backend.arn,
      data.aws_ecr_repository.shopmicro_frontend.arn,
      data.aws_ecr_repository.shopmicro_frontend_admin.arn
    ]
  }

  statement {
    sid    = "ReadAndWriteUploads"
    effect = "Allow"
    actions = [
      "s3:GetObject",
      "s3:PutObject"
    ]
    resources = ["${aws_s3_bucket.uploads.arn}/uploads/*"]
  }

  statement {
    sid     = "ReadSharedSecrets"
    effect  = "Allow"
    actions = ["ssm:GetParameter"]
    resources = [
      aws_ssm_parameter.rds_password.arn,
      aws_ssm_parameter.jwt_secret.arn
    ]
  }
}

resource "aws_iam_role_policy" "application" {
  name   = "${local.name}-application"
  role   = aws_iam_role.ec2.id
  policy = data.aws_iam_policy_document.application.json
}

resource "aws_iam_role_policy_attachment" "ssm" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_instance_profile" "ec2" {
  name = "${local.name}-ec2"
  role = aws_iam_role.ec2.name
}
