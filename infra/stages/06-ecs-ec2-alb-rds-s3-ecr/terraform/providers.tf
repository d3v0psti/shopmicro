provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "Terraform"
      Stage       = "06-ecs-ec2-alb-rds-s3-ecr"
    }
  }
}
