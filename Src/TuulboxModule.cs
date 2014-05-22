using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.PropellerApi;
using RT.Servers;
using RT.Util;
using RT.Util.Json;
using System.Reflection;
using RT.Util.Serialization;

namespace Tuulbox
{
    public sealed partial class TuulboxModule : IPropellerModule
    {
        public TuulboxSettings Settings;

        private static IEnumerable<ITuul> _toolsCache = getTools().ToArray();
        public static IEnumerable<ITuul> Tuuls { get { return _toolsCache; } }
        private UrlResolver _resolverCache = null;
        public UrlResolver Resolver
        {
            get
            {
                return _resolverCache ?? (_resolverCache = new UrlResolver(Tuuls.Select(generateUrlMapping)));
            }
        }

        private UrlMapping generateUrlMapping(ITuul tuul)
        {
            var resolver = new UrlResolver();
            resolver.Add(new UrlHook("", Settings.UseDomain, specific: true), handler: req => handle(req, tuul));
            var js = tuul.Js;
            if (js != null)
            {
#if DEBUG
                var jsBytes = js.ToUtf8();
#else
                var jsBytes = JsonValue.Fmt(js).ToUtf8();
#endif
                resolver.Add(new UrlHook("js", Settings.UseDomain, specific: true), handler: req => HttpResponse.JavaScript(jsBytes));
            }
            var css = tuul.Css;
            if (css != null)
            {
                var cssBytes = css.ToUtf8();
                resolver.Add(new UrlHook("css", Settings.UseDomain, specific: true), handler: req => HttpResponse.Css(cssBytes));
            }

            return new UrlMapping(new UrlHook(tuul.UrlName ?? "", Settings.UseDomain), resolver.Handle);
        }

        private static IEnumerable<ITuul> getTools()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => !type.IsAbstract && typeof(ITuul).IsAssignableFrom(type))
                .Select(type => (ITuul) Activator.CreateInstance(type))
                .Where(tuul => tuul.Enabled);
        }

        private static void PostBuildCheck(IPostBuildReporter rep)
        {
            reportDuplicate(rep, t => t.Name, "name");
            reportDuplicate(rep, t => t.UrlName, "URL");

            string tuulWithNullUrl = null;
            foreach (var tuul in Tuuls)
            {
                if (tuul.UrlName == null)
                {
                    if (tuulWithNullUrl != null)
                        rep.Error(@"Two tuuls, “{0}” and “{1}”, have null UrlNames. Only one tuul can be the top-level tuul.".Fmt(tuul.UrlName, tuulWithNullUrl), "class " + tuul.GetType().Name);
                    else
                        tuulWithNullUrl = tuul.UrlName;
                }
                else if (tuul.UrlName.Length == 0)
                    rep.Error(@"The tuul “{0}” has an empty UrlName. To make it the top-level tuul, return null instead.".Fmt(tuul.UrlName), "class " + tuul.GetType().Name);
            }
        }

        private static Tuple<T, T> firstDuplicate<T, TCriterion>(IEnumerable<T> source, Func<T, TCriterion> criterion)
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

        private static void reportDuplicate(IPostBuildReporter rep, Func<ITuul, string> criterion, string thing)
        {
            var duplicate = firstDuplicate(Tuuls, criterion);
            if (duplicate != null)
            {
                rep.Error(@"The tuul {0} ""{1}"" is used more than once.".Fmt(thing, criterion(duplicate.Item1)), "class " + duplicate.Item1.GetType().Name);
                rep.Error(@"... second use here.", "class " + duplicate.Item2.GetType().Name);
            }
        }

        public void Init(LoggerBase log, JsonValue settings, ISettingsSaver saver)
        {
            Settings = ClassifyJson.Deserialize<TuulboxSettings>(settings);
            saver.SaveSettings(ClassifyJson.Serialize(Settings));
        }

        public string Name { get { return "Tuulbox"; } }
        public string[] FileFiltersToBeMonitoredForChanges { get { return null; } }
        public HttpResponse Handle(HttpRequest req) { return Resolver.Handle(req); }
        public bool MustReinitialize { get { return false; } }
        public void Shutdown() { }
    }
}