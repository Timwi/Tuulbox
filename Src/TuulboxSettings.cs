using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuulbox
{
    public sealed class TuulboxSettings
    {
        /// <summary>If <c>true</c>, every tuul will be on a subdomain; otherwise, URL paths are used.</summary>
        public bool UseDomain = false;

        /// <summary>
        ///     Depending on the value of <see cref="UseDomain"/>, either the base domain (e.g. <c>tuulbox.com</c>) or the
        ///     base path (e.g. <c>/tuulbox</c>, or the empty string).</summary>
        public string BaseDomainOrPath = "";

        /// <summary>Specifies the information to be displayed in the Impressum, or <c>null</c> to disable the Impressum entirely.</summary>
        public string[] Impressum = null;
    }
}
