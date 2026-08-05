provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "Terraform"
      Stage       = "05-alb-asg-ec2-rds-s3-ecr"
    }
  }
}
