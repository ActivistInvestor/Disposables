
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace MyNamespace
{

   /// <summary>
   /// A class that manages the disposal of an ordered
   /// set of IDisposable instances.
   /// 
   /// Contract: IDisposable instances managed by this
   /// class are disposed in the reverse order in which 
   /// they are added. Care must be taken in cases where
   /// there is an inter-dependence between objects added 
   /// to the disposal queue. Dependent objects should be
   /// added AFTER the objects they're dependent on have 
   /// been added.
   /// 
   /// Following that rule will serve to ensure that when 
   /// a dependent object is disposed, the object(s) that
   /// it depends on have not been disposed yet.
   /// 
   /// </summary>

   public static class Disposables
   {
      static object lockObj = new object();
      static OrderedSet<IDisposable> list = new OrderedSet<IDisposable>();

      static Disposables()
      {
         Application.QuitWillStart += quitWillStart;
      }

      private static void quitWillStart(object sender, EventArgs e)
      {
         Clear(true);  
      }

      /// <summary>
      /// Disposables.Add(IDisposable item,...)
      /// 
      /// This method can be passed any number of IDisposables.
      /// When the Clear() method is called, the arguments will
      /// be disposed and dequeued.
      /// 
      /// e.g.:
      /// 
      ///    Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 1.0);
      ///    Disposables.Add(circle);
      ///    
      /// Note that in lieu of calling Add(), the AutoDispose() 
      /// extension method can instead be used thusly:
      ///
      ///    Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 1.0);
      ///    circle.AutoDispose();
      ///    
      /// or with this one-liner:
      /// 
      ///    Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 1.0).AutoDispose();
      ///    
      /// If for some reason, you no longer want an object
      /// that was previously-queued for disposal to not
      /// be disposed, you can remove it from the queue via
      /// either the Remove() method, or the AutoDispose()
      /// method, like so:
      /// 
      ///    Disposables.Remove(circle);
      ///    
      /// or using the AutoDispose() extension method, by
      /// passing false as the argument:
      /// 
      ///    circle.AutoDispose(false);
      /// 
      /// If either the Remove() or AutoDispose(false) methods are used
      /// to dequeue an IDisposable, the caller becaumes responsible for
      /// disposing the IDisposable.
      /// 
      /// </summary>
      /// <param name="disposable">The items to be disposed at shutdown
      /// or when the Clear() method is called</param>

      public static void Add(params IDisposable[] args)
      {
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         lock(lockObj)
         {
            foreach(IDisposable disposable in args)
            {
               if(disposable == null)
                  throw new ArgumentException("null element");
               list.Add(disposable);
            }
         }
      }

      /// <summary>
      /// Removes an IDisposable that is queued for 
      /// disposiong without disposing it.
      /// </summary>

      public static bool Remove(IDisposable disposable)
      {
         lock(lockObj)
         {
            return list.Remove(disposable);
         }
      }

      /// <summary>
      /// Checks if a given IDisposable is already queued 
      /// for disposal:
      /// </summary>

      public static bool Contains(IDisposable item) => list.Contains(item);

      /// <summary>
      /// The count of elements currently queued for disposal:
      /// </summary>
      
      public static int Count => list.Count;

      /// <summary>
      /// Disposes all elements queued for disposal. This method
      /// is autonomously called when AutoCAD shuts down, so there
      /// is no need to explicitly call it at shutdown. 
      /// 
      /// This method can also be called at any time to dispose and 
      /// dequeue IDisposable instances that were previously-queued
      /// using the Add() method or the AutoDispose() method.
      /// </summary>

      public static void Dispose()
      {
         Clear(false);
      }

      static void Clear(bool terminating = false)
      {
         System.Exception exception = null;
         lock(lockObj)
         {
            for(int i = list.Count - 1; i > -1; i--)
            {
               try
               {
                  IDisposable item = list[i];
                  if(item != null)
                  {
                     DisposableWrapper wrapper = item as DisposableWrapper;
                     if(wrapper != null && wrapper.IsDisposed)
                        continue;
                     item.Dispose();
                  }
               }
               catch(System.Exception ex)
               {
                  exception = exception ?? ex;
               }
            }
            list.Clear();
         }
         if(exception != null)
         {
            if(!terminating)
               throw exception;
            else
            {
               Debug.WriteLine($"Dispose() threw {exception}");
               Console.Beep();
            }
         }
      }

      /// <summary>
      /// This method is typically used only in specialized
      /// use cases. It will remove any elements derived from
      /// DisposableWrapper whose IsDisposed property is true.
      /// If a DisposableWrapper's IdDisposed property is true,
      /// the instance has been disposed, making a call to its
      /// Dispose() method unecessary.
      /// </summary>

      public static void PurgeDisposableWrappers()
      {
         lock(lockObj)
         {
            list.ExceptWhere(d => d is DisposableWrapper dw && dw.IsDisposed);
         }
      }

      /// <summary>
      /// An extension method targeting IDisposable, that can be used
      /// in lieu of the Add() method. If the add argument is true, the
      /// target is queued for disposal. If the add argument is false
      /// the target is dequeued for disposal and is not disposed, and
      /// the caller becomes responsible for disposing the target.
      /// </summary>
      /// <param name="item">The item to be queued/dequeued for 
      /// automatic disposal</param>
      /// <param name="add">true to queue the item for disposal,
      /// or false to dequeue the item (default = true)</param>
      /// <exception cref="ArgumentNullException"></exception>

      public static T AutoDispose<T>(this T item, bool add = true)
         where T : IDisposable
      {
         if(item == null)
            throw new ArgumentNullException(nameof(item));
         if(add)
            Add(item);
         else
            Remove(item);
         return item;
      }

      /// <summary>
      /// An extension method that can be used in lieu of the
      /// Disposables.Contains() method to indicate if a given
      /// IDisposable has been queued for disposal.
      /// </summary>
      /// <param name="item">The IDisposable to query for</param>
      /// <returns>true if the argument is queued for disposal</returns>

      public static bool IsAutoDispose(this IDisposable item)
      {
         return Disposables.Contains(item);
      }
   }
}

