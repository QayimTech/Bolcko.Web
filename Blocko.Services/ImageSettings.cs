namespace Blocko.Services
{
    public class ImageSettings
    {
        /// <summary>Local disk folder (used only when S3 is NOT configured).</summary>
        public string BasePath { get; set; } = string.Empty;

        /// <summary>
        /// Name of the S3 bucket that stores uploaded images.
        /// Set via environment variable ImageSettings__S3BucketName.
        /// When empty, the app falls back to saving images on local disk (old behaviour).
        /// </summary>
        public string S3BucketName { get; set; } = string.Empty;

        /// <summary>AWS region of the bucket, e.g. "eu-central-1". Env var: ImageSettings__S3Region.</summary>
        public string S3Region { get; set; } = "eu-central-1";

        /// <summary>Optional folder prefix inside the bucket (e.g. "site/"). Usually left empty.</summary>
        public string S3KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Optional KMS key (ID or ARN) used to encrypt uploads. The bucket policy requires
        /// "x-amz-server-side-encryption: aws:kms" on every upload, so uploads are always sent
        /// with KMS encryption; if this is empty the bucket's default KMS key is used.
        /// Env var: ImageSettings__S3KmsKeyId (or S3__KmsKeyId).
        /// </summary>
        public string S3KmsKeyId { get; set; } = string.Empty;

        public bool UseS3 => !string.IsNullOrWhiteSpace(S3BucketName);
    }
}
