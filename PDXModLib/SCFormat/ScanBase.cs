using QUT.Gppg;

namespace PDXModLib.SCFormat
{
    public abstract class ScanBase : AbstractScanner<SCValue, LexLocation>
    {
        protected virtual bool yywrap() { return true; }
    }
}
