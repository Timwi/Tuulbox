﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tuulbox {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Tuulbox.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $(function () {
        ///    function setHsl(h, s, l, skipRgb) {
        ///        h = ((h % 360 + 360) % 360) | 0;
        ///        s = Math.min(100, Math.max(0, s)) | 0;
        ///        l = Math.min(100, Math.max(0, l)) | 0;
        ///
        ///        $(&apos;#colors_hsl&apos;).val(`hsl(${h}, ${s}%, ${l}%)`);
        ///        $(&apos;#colors_hue&apos;).val(h);
        ///        $(&apos;#colors_saturation&apos;).val(s);
        ///        $(&apos;#colors_lightness&apos;).val(l);
        ///
        ///        if (skipRgb)
        ///            return setCursorPosition();
        ///        var c = (1 - Math.abs(l / 50 - 1)) * (s / 100);
        ///        var x = c * [rest of string was truncated]&quot;;.
        /// </summary>
        public static string ColorsJs {
            get {
                return ResourceManager.GetString("ColorsJs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        public static byte[] MainCss {
            get {
                object obj = ResourceManager.GetObject("MainCss", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        public static byte[] MainJs {
            get {
                object obj = ResourceManager.GetObject("MainJs", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $(function () {
        ///    var $curTooltip = null;
        ///
        ///    var htmlEscape = function (str) {
        ///        return str.replace(&apos;&amp;&apos;, &apos;&amp;amp;&apos;).replace(&apos;&lt;&apos;, &apos;&amp;lt;&apos;).replace(&apos;&gt;&apos;, &apos;&amp;gt;&apos;).replace(&apos;&quot;&apos;, &apos;&amp;quot;&apos;).replace(&quot;&apos;&quot;, &apos; &amp;#39; &apos;);
        ///    };
        ///
        ///    var explains = [
        ///        [&apos;then&apos;, &apos;&lt;h3&gt;Literal&lt;/h3&gt;&lt;p&gt;Matches the specified text.&lt;/p&gt;&apos;],
        ///        [&apos;literal&apos;, function (elem) { return &apos;&lt;h3&gt;Literal&lt;/h3&gt;&lt;p&gt;Matches &apos; + htmlEscape(elem.data(&apos;text&apos;)) + &apos;.&apos; + (elem.data(&apos;isqe&apos;) ? &apos;&lt;p&gt;The &lt;code&gt;\\Q&lt;/code&gt;...&lt;code&gt;\\E&lt;/code&gt; operator [rest of string was truncated]&quot;;.
        /// </summary>
        public static string RegexesJs {
            get {
                return ResourceManager.GetString("RegexesJs", resourceCulture);
            }
        }
    }
}
