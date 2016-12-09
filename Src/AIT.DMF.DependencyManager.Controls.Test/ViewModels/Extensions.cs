
using System.Collections;
using System.Linq;

namespace AIT.DMF.DependencyManager.Controls.Test.ViewModels
{
    public static class Extensions
    {
        public static T GetEntry<T>(this IList list)
        {
            var result = list.OfType<T>().FirstOrDefault();
            return result;
        }
    }
}
