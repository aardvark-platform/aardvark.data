using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aardvark.Data.Vrml97
{
    internal static class Vrml97Extensions
    {
        /// <summary>
         /// Encapsulates the expression "[object] != null ? 'select something from [object]' : defaultValue
         /// </summary>
        public static Tr TrySelect<To, Tr>(this To o, Func<To, Tr> selector, Tr defaultValue = default(Tr))
            where To : class
        {
            return o != null ? selector(o) : defaultValue;
        }
    }
}
