
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
   /// they are added.
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
      /// This method can be passed any number of IDisposables.
      /// When the Clear() method is called, the elements will
      /// be disposed.
      /// 
      /// e.g.:
      /// 
      ///    Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 1.0);
      ///    Disposables.Add(circle);
      ///    
      /// Note that in lieu of calling Add(), the AutoDispose() extension
      /// method can be used thusly:
      ///
      ///    Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 1.0).AutoDispose();
      ///    
      /// </summary>
      /// <param name="disposable">The item to be disposed at shutdown
      /// or when the Clear() method is called</param>
      /// <param name="add">troe to add the item, false to remove it</param>
      /// 

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
               if(!list.Contains(disposable))
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
      /// Checks if a given IDisposable is already queued for disposing:
      /// </summary>

      public static bool Contains(IDisposable disposable) => list.Contains(disposable);

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
      /// using the Add() method.
      /// </summary>

      public static void Clear()
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
      /// An extension method targeting IDisposable, that can be used
      /// in lieu of the Add() method. If the add argument is true, the
      /// target is queued for disposal. If the add argument is false
      /// the target is dequeued for disposal and is not disposed, and
      /// the caller becomes responsible for disposing the target.
      /// </summary>
      /// <param name="item">The item to be queued/dequeued for 
      /// automatic disposal</param>
      /// <param name="add">true to queue the item for disposal,
      /// or false to dequeue the item</param>
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
   }
}

