using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.Utility
{
    public static class IEnumerableExtensions
    {
        public static Tuple<ICollection<T>, ICollection<K>> OuterJoin<T,K>(this IEnumerable<T> source, IEnumerable<K> target,
            Func<T, K, int> comparator)
        {
            var added = new List<K>();
            var removed = new List<T>();
            using (var sourceEnumerator = source.GetEnumerator())
            using (var targetEnumerator = target.GetEnumerator())
            {
                var hasSource = sourceEnumerator.MoveNext();
                var hasTarget = targetEnumerator.MoveNext();

                while (hasSource || hasTarget)
                {
                    T sourceElement = hasSource ? sourceEnumerator.Current : default(T);
                    K targetElement = hasTarget ? targetEnumerator.Current : default(K);

                    if (hasSource && hasTarget)
                    {
                        var comparisonResult = comparator(sourceElement, targetElement);

                        if (comparisonResult < 0)
                        {
                            removed.Add(sourceElement);
                            hasSource = sourceEnumerator.MoveNext();
                            continue;
                        }

                        if (comparisonResult > 0)
                        {
                            added.Add(targetElement);
                            hasTarget = targetEnumerator.MoveNext();
                            continue;
                        }

                        hasSource = sourceEnumerator.MoveNext();
                        hasTarget = targetEnumerator.MoveNext();
                        continue;
                    }

                    if (hasSource)
                    {
                        removed.Add(sourceElement);
                        hasSource = sourceEnumerator.MoveNext();
                    }
                    else
                    {
                        added.Add(targetElement);
                        hasTarget = targetEnumerator.MoveNext();
                    }
                }
            }

            return Tuple.Create((ICollection<T>)removed, (ICollection<K>)added);
        }
    }
}
