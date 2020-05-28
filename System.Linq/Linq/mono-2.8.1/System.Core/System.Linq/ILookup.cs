using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
	/// <summary>
	///         Definiert eine Indexer, eine Größeneigenschaft und eine boolesche Suchmethode für Datenstrukturen, die Schlüssel zuordnen <see cref="T:System.Collections.Generic.IEnumerable`1" /> -Sequenzen von Werten.
	///       </summary>
	/// <typeparam name="TKey">
	///           Der Typ der Schlüssel in der <see cref="T:System.Linq.ILookup`2" />.
	///         </typeparam>
	/// <typeparam name="TElement">
	///           Der Typ der Elemente in der <see cref="T:System.Collections.Generic.IEnumerable`1" /> Sequenzen, die die Werte der <see cref="T:System.Linq.ILookup`2" />.
	///         </typeparam>
	public interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
	{
		/// <summary>
		///         Ruft die Anzahl der Schlüssel-Wert-Paare der Auflistung der <see cref="T:System.Linq.ILookup`2" />.
		///       </summary>
		/// <returns>
		///         Die Anzahl der Schlüssel-Wert-Paare der Auflistung in der <see cref="T:System.Linq.ILookup`2" />.
		///       </returns>
		int Count
		{
			get;
		}

		/// <summary>
		///         Ruft die <see cref="T:System.Collections.Generic.IEnumerable`1" /> Sequenz von Werten nach einem angegebenen Schlüssel indiziert.
		///       </summary>
		/// <param name="key">
		///           Der Schlüssel der gewünschten Sequenz von Werten.
		///         </param>
		/// <returns>
		///         Die <see cref="T:System.Collections.Generic.IEnumerable`1" /> Sequenz von Werten ab, das durch den angegebenen Schlüssel.
		///       </returns>
		IEnumerable<TElement> this[TKey key]
		{
			get;
		}

		/// <summary>
		///         Ermittelt, ob ein angegebener Schlüssel der <see cref="T:System.Linq.ILookup`2" />.
		///       </summary>
		/// <param name="key">
		///           Der Schlüssel zum Suchen der <see cref="T:System.Linq.ILookup`2" />.
		///         </param>
		/// <returns>
		///         <see langword="true" /> Wenn <paramref name="key" /> befindet sich in der <see cref="T:System.Linq.ILookup`2" />andernfalls <see langword="false" />.
		///       </returns>
		bool Contains(TKey key);
	}
}
