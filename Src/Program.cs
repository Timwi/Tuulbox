using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Generexes;

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

        private static IEnumerable<ITool> _toolsCache = getTools().ToArray();
        public static IEnumerable<ITool> Tools { get { return _toolsCache; } }

        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--post-build-check")
                return Ut.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());

            //var s=new Stringerex().Process(m => (ITool)null);
            //s.Then

            var server = new HttpServer(new HttpServerOptions { Port = 8765, MaxSizePostContent = 1024 * 1024 * 4 }) { PropagateExceptions = _isDebug };
            var resolver = new UrlResolver(Tools.SelectMany(generateHooks));
            server.Handler = resolver.Handle;
            Console.WriteLine("Server listening on Port {0}.".Fmt(server.Options.Port));
            server.StartListening(true);
            return 0;
        }

        static IEnumerable<ITool> getTools()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => !type.IsAbstract && typeof(ITool).IsAssignableFrom(type))
                .Select(type => (ITool) Activator.CreateInstance(type));
        }

        static void PostBuildCheck(IPostBuildReporter rep)
        {
            reportDuplicate(rep, t => t.Name, "name");
            reportDuplicate(rep, t => t.Url, "URL");

            foreach (var tool in Program.Tools)
                if (tool.Url == null || !tool.Url.StartsWith("/"))
                    rep.Error(@"The tool URL ""{0}"" does not start with a slash.".Fmt(tool.Url), "class " + tool.GetType().Name);
        }

        static Tuple<T, T> firstDuplicate<T, TCriterion>(IEnumerable<T> source, Func<T, TCriterion> criterion)
        {
            var dic = new Dictionary<TCriterion, T>();
            foreach (var item in source)
            {
                var cri = criterion(item);
                if (cri == null)
                    continue;
                if (dic.ContainsKey(cri))
                    return Tuple.Create(dic[cri], item);
                dic[cri] = item;
            }
            return null;
        }

        static void reportDuplicate(IPostBuildReporter rep, Func<ITool, string> criterion, string thing)
        {
            var duplicate = firstDuplicate(Tools, criterion);
            if (duplicate != null)
            {
                rep.Error(@"The tool {0} ""{1}"" is used more than once.".Fmt(thing, criterion(duplicate.Item1)), "class " + duplicate.Item1.GetType().Name);
                rep.Error(@"... second use here.", "class " + duplicate.Item2.GetType().Name);
            }
        }

        static IEnumerable<UrlMapping> generateHooks(ITool tool)
        {
            yield return new UrlMapping(path: tool.Url, specificPath: true, handler: req => handle(req, tool));
            var js = tool.Js;
            if (js != null)
            {
                /*
                var jsBytes = JsonValue.Fmt(js).ToUtf8();
                /*/
                var jsBytes = js.ToUtf8();
                /**/
                yield return new UrlMapping(path: tool.Url + "/js", specificPath: true, handler: req => HttpResponse.JavaScript(jsBytes));
            }
            var css = tool.Css;
            if (css != null)
            {
                var cssBytes = css.ToUtf8();
                yield return new UrlMapping(path: tool.Url + "/css", specificPath: true, handler: req => HttpResponse.Css(cssBytes));
            }
        }
    }
}
