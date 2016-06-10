using QUT.Gppg;

namespace SCModManager.SCFormat
{
    public abstract class ScanBase : AbstractScanner<SCValue, LexLocation>
    {
        protected virtual bool yywrap() { return true; }
    }
}
