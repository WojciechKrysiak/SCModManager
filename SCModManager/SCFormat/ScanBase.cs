using QUT.Gppg;

namespace SCModManager.SCFormat
{
    internal abstract class ScanBase : AbstractScanner<SCValue, LexLocation>
    {
        protected virtual bool yywrap() { return true; }
    }
}
