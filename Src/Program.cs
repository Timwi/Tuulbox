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

        private static IEnumerable<ITuul> _toolsCache = getTools().ToArray();
        public static IEnumerable<ITuul> Tuuls { get { return _toolsCache; } }

        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--post-build-check")
                return Ut.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());

            var server = new HttpServer(new HttpServerOptions { Port = 8765, MaxSizePostContent = 1024 * 1024 * 4 }) { PropagateExceptions = _isDebug };
            var resolver = new UrlResolver(Tuuls.SelectMany(generateHooks));
            server.Handler = resolver.Handle;
            Console.WriteLine("Server listening on Port {0}.".Fmt(server.Options.Port));
            server.StartListening(true);
            return 0;
        }

        static IEnumerable<ITuul> getTools()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => !type.IsAbstract && typeof(ITuul).IsAssignableFrom(type))
                .Select(type => (ITuul) Activator.CreateInstance(type))
                .Where(tuul => tuul.Enabled);
        }

        static void PostBuildCheck(IPostBuildReporter rep)
        {
            reportDuplicate(rep, t => t.Name, "name");
            reportDuplicate(rep, t => t.Url, "URL");

            foreach (var tuul in Program.Tuuls)
                if (tuul.Url == null || !tuul.Url.StartsWith("/"))
                    rep.Error(@"The tuul URL “{0}” does not start with a slash.".Fmt(tuul.Url), "class " + tuul.GetType().Name);
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

        static void reportDuplicate(IPostBuildReporter rep, Func<ITuul, string> criterion, string thing)
        {
            var duplicate = firstDuplicate(Tuuls, criterion);
            if (duplicate != null)
            {
                rep.Error(@"The tuul {0} ""{1}"" is used more than once.".Fmt(thing, criterion(duplicate.Item1)), "class " + duplicate.Item1.GetType().Name);
                rep.Error(@"... second use here.", "class " + duplicate.Item2.GetType().Name);
            }
        }

        static IEnumerable<UrlMapping> generateHooks(ITuul tuul)
        {
            yield return new UrlMapping(path: tuul.Url, specificPath: true, handler: req => handle(req, tuul));
            var js = tuul.Js;
            if (js != null)
            {
                //*
                var jsBytes = JsonValue.Fmt(js).ToUtf8();
                /*/
                var jsBytes = js.ToUtf8();
                /**/
                yield return new UrlMapping(path: tuul.Url + "/js", specificPath: true, handler: req => HttpResponse.JavaScript(jsBytes));
            }
            var css = tuul.Css;
            if (css != null)
            {
                var cssBytes = css.ToUtf8();
                yield return new UrlMapping(path: tuul.Url + "/css", specificPath: true, handler: req => HttpResponse.Css(cssBytes));
            }
        }
    }
}
