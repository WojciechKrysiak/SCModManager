using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using System.Reactive;

namespace SCModManager.Utility
{
    public static class IObservableExtensions
	{
		public static IReactiveDerivedList<TNew> ToReactiveCollection<T, TNew, TDontCare>(this IObservable<IEnumerable<T>> source, Func<T, TNew> selector, Func<T, bool> filter = null, Func<TNew, TNew, int> orderer = null, IObservable<TDontCare> reset = null, IScheduler scheduler = null)
		{
			List<T> currentValue = new List<T>();

			var resetTrigger = source.Do(enumerable =>
			{
				currentValue.Clear();
				if (enumerable != null)
					currentValue.AddRange(enumerable);
			});

			if (reset != null)
				resetTrigger = resetTrigger.Merge(reset.Select(tdc => new T[0]));

			return currentValue.CreateDerivedCollection(selector, filter, orderer, resetTrigger, scheduler);
		}
		
		public static IReactiveCollection<TNew> ToReactiveCollection<T, TNew>(this IObservable<IEnumerable<T>> source, Func<T, TNew> selector, Func<T, bool> filter = null, Func<TNew, TNew, int> orderer = null,IScheduler scheduler = null)
		{
			return source.ToReactiveCollection(selector, filter, orderer, (IObservable<Unit>)null, scheduler);
		}
	}
}
