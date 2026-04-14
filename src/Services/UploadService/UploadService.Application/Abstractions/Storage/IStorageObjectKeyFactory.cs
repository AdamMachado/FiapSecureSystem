public interface IStorageObjectKeyFactory
{
    string CreateForAnalysisUpload(Guid analysisRequestId, string originalFileName, DateTime nowUtc);
}