using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
	/// <summary>
	///         Stellt Funktionen zur Auswertung von Abfragen für eine bestimmte Datenquelle ohne Angabe des Datentyps bereit.
	///       </summary>
	public interface IQueryable : IEnumerable
	{
		/// <summary>
		///         Ruft den Typ der Elemente, die zurückgegeben werden, wenn diese Instanz die Ausdrucksbaumstruktur zugeordnete <see cref="T:System.Linq.IQueryable" /> ausgeführt wird.
		///       </summary>
		/// <returns>
		///         Ein <see cref="T:System.Type" /> das den Typ der Elemente, die zurückgegeben werden, wenn die diesem Objekt zugeordnete Ausdrucksstruktur ausgeführt wird, darstellt.
		///       </returns>
		Type ElementType
		{
			get;
		}

		/// <summary>
		///         Ruft die Ausdrucksbaumstruktur, die mit der Instanz von verknüpft ist <see cref="T:System.Linq.IQueryable" />.
		///       </summary>
		/// <returns>
		///         Die <see cref="T:System.Linq.Expressions.Expression" /> die mit dieser Instanz von verknüpft ist <see cref="T:System.Linq.IQueryable" />.
		///       </returns>
		Expression Expression
		{
			get;
		}

		/// <summary>
		///         Ruft den Abfrageanbieter, der dieser Datenquelle zugeordnet ist.
		///       </summary>
		/// <returns>
		///         Die <see cref="T:System.Linq.IQueryProvider" /> dieser Datenquelle zugeordnet ist.
		///       </returns>
		IQueryProvider Provider
		{
			get;
		}
	}
	/// <summary>
	///         Stellt Funktionen zur Auswertung von Abfragen für eine bestimmte Datenquelle mit bekanntem Datentyp bereit.
	///       </summary>
	/// <typeparam name="T">
	///           Der Typ der Daten in der Datenquelle.
	///         </typeparam>
	public interface IQueryable<T> : IEnumerable<T>, IEnumerable, IQueryable
	{
	}
}
