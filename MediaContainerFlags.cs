using System;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace MediaCurator
{
   public class MediaContainerFlags
   {
      #region Constants

      public enum Flag
      {
         None = 0,
         Deleted = 1 << 0,
         Favorite = 1 << 1
      }

      #endregion // Constants

      private readonly HashSet<Flag> _flags = new();

      #region Fields

      /// <summary>
      /// Gets or sets a value indicating whether this <see cref="MediaContainerFlags"/> instance is
      /// flagged as favorite.
      /// </summary>
      /// <value>
      ///   <c>true</c> if flagged as favorite; otherwise, <c>false</c>.
      /// </value>
      public bool Favorite
      {
         get
         {
            return GetFlagValue(Flag.Favorite);
         }

         set
         {
            SetFlagValue(Flag.Favorite, value);
         }
      }

      /// <summary>
      /// Gets or sets a value indicating whether this <see cref="MediaContainerFlags"/> instance is
      /// flagged as deleted.
      /// </summary>
      /// <value>
      ///   <c>true</c> if flagged as deleted; otherwise, <c>false</c>.
      /// </value>
      public bool Deleted
      {
         get
         {
            return GetFlagValue(Flag.Deleted);
         }

         set
         {
            SetFlagValue(Flag.Deleted, value);
         }
      }

      /// <summary>
      /// Returns a list of all the flags set on the <see cref="MediaContainer"/>.
      /// </summary>
      public HashSet<Flag> All
      {
         get => _flags;
      }

      #endregion // Fields

      #region Operators


      /// <summary>
      /// Returns all the flags set as a string array.
      /// </summary>
      /// <returns>A string array listing all the set flags.</returns>
      public string[] ToArray()
      {
         return All.Select(flag => Enum.GetName(typeof(MediaContainerFlags.Flag), flag)).OfType<string>().ToArray();
      }

      #endregion // Operators

      #region Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaContainerFlags"/> class.
      /// </summary>
      public MediaContainerFlags()
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaContainerFlags"/> class by parsing the.
      /// </summary>
      /// <param name="self">The node element representing the current MediaContainer.</param>
      /// <exception cref="NullReferenceException">The MediaContainerFlags cannot be initialized using
      /// a non-existing element!</exception>
      public MediaContainerFlags(IEnumerable<string> flags)
      {
         if (flags == null) return;

         foreach (var flag in flags)
         {
            _flags.Add(Enum.Parse<Flag>(flag, true));
         }
      }

      #endregion // Constructors

      #region Common Functionality

      /// <summary>
      /// Returns the value of the specified flag.
      /// </summary>
      /// <param name="flag">The flag.</param>
      /// <returns>
      ///   <c>true</c> if flag is set; otherwise, <c>false</c>.
      /// </returns>
      private bool GetFlagValue(Flag flag)
      {
         return _flags.Contains(flag);
      }

      /// <summary>
      /// Sets the value of the supplied flag value.
      /// </summary>
      /// <param name="flag">The flag to set/reset.</param>
      /// <param name="value">if set to <c>true</c> [value].</param>
      private void SetFlagValue(Flag flag, bool value)
      {
         if (value)
         {
            _flags.Add(flag);
         }
         else
         {
            _flags.Remove(flag);
         }
      }

      #endregion // Common Functionality
   }
}
