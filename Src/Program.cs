using System.Reflection;
using RT.PropellerApi;
using RT.Util;

namespace Tuulbox
{
    partial class Program
    {
        const bool _isDebug =
#if DEBUG
 true
#else
            false
#endif
;

        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--post-build-check")
                return Ut.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());

            PropellerUtil.RunStandalone(PathUtil.AppPathCombine("Tuulbox.Settings.json"), new TuulboxModule());
            return 0;
        }
    }
}
