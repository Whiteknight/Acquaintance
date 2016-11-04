namespace Acquaintance.Logging
{
    public interface ILogger
    {
        void Debug(string fmt, params object[] args);
        void Info(string fmt, params object[] args);
        void Warn(string fmt, params object[] args);
        void Error(string fmt, params object[] args);
    }
}
