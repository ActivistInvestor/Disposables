﻿using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;

namespace System.Collections.Generic
{
   /// <summary>
   /// Marshals both a List<T> and a HashSet<T> as a quick/dirty
   /// means of implementing an ordered set (whose elements are
   /// ordered like a List, but have fast membership lookup like
   /// a HashSet). It uses the list to maintain ordering and the
   /// HashSet to perform membership testing.
   /// 
   /// Obviously, this approach has the downside of having to 
   /// store each element in duplicate, meaning that it will use 
   /// slightly more than 2x the memory used by a simple List<T>.
   /// </summary>
   /// <typeparam name="T">The element type</typeparam>

   public class OrderedSet<T> : ICollection<T>
   {
      HashSet<T> set;
      List<T> list;
      IEqualityComparer<T> comparer;

      public OrderedSet(IEqualityComparer<T> comparer = null)
      {
         set = new HashSet<T>(comparer);
         list = new List<T>();
         this.comparer = set.Comparer;
      }

      public T this[int index]
      {
         get { return list[index]; }
      }

      public bool ExceptWhere(Func<T, bool> predicate)
      {
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         return ExceptWith(list.Where(predicate));
      }

      public bool ExceptWith(IEnumerable<T> enumerable)
      {
         if(enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));
         if(this.Count > 0)
         {
            int count = list.Count;
            set.ExceptWith(enumerable);
            if(set.Count < list.Count)
            {
               PurgeList();
               return true;
            }
         }
         return false;
      }

      public bool UnionWith(IEnumerable<T> enumerable, bool acceptNull = true)
      {
         if(enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));
         int count = this.Count;
         foreach(T item in enumerable)
         {
            if(!acceptNull && item == null)
               throw new ArgumentException("null element");
            this.Add(item);
         }
         return this.Count > count;
      }

      void PurgeList()
      {
         list = list.Where(item => set.Contains(item)).ToList();
      }

      public int Count => set.Count;

      public bool IsReadOnly => false;

      public void Add(T item)
      {
         if(set.Add(item))
            list.Add(item);
      }

      public void Clear()
      {
         set.Clear();
         list.Clear();
      }

      public bool Contains(T item) => set.Contains(item);

      public void CopyTo(T[] array, int arrayIndex)
      {
         list.CopyTo(array, arrayIndex);
      }

      public bool Remove(T item)
      {
         if(set.Remove(item))
         {
            int pos = IndexOf(item);
            if(pos > -1)
               list.RemoveAt(pos);
            return true;
         }
         return false;
      }

      /// <summary>
      /// Because a user-specified IEqualityComparer<T> is
      /// supported, we cannot rely on comparisons done by
      /// List<T>, which always uses the default equality
      /// comparer for the element type. In this case, the
      /// same equality comparer used by the HashSet must
      /// be used by the List.
      /// </summary>

      public int IndexOf(T item)
      {
         for(int i = 0; i < list.Count; i++)
         {
            if(comparer.Equals(list[i], item))
               return i;
         }
         return -1;
      }

      public IEnumerator<T> GetEnumerator()
      {
         return list.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return this.GetEnumerator();
      }
   }


}