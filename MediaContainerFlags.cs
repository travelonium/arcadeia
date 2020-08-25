using System;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace MediaCurator
{
   public class MediaContainerFlags : INotifyPropertyChanged
   {
      #region Constants

      public enum Flag
      {
         None = 0,
         Deleted = 1 << 0,
         Favorite = 1 << 1
      }

      #endregion // Constants

      #region Fields

      /// <summary>
      /// The XML Element this particular container object is associated with in the MediaLibrary. 
      /// This is passed on to the <see cref="MediaContainerFlags"/> by the parent object's 
      /// constructor.
      /// </summary>
      private XElement Self = null;

      public event PropertyChangedEventHandler PropertyChanged;

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
            return GetFlagValue((uint)Flag.Favorite);
         }

         set
         {
            SetFlagValue((uint)Flag.Favorite, value);
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
            return GetFlagValue((uint)Flag.Deleted);
         }

         set
         {
            SetFlagValue((uint)Flag.Deleted, value);
         }
      }

      /// <summary>
      /// Returns a list of all the flags set on the <see cref="MediaContainer"/>.
      /// </summary>
      public List<Flag> All
      {
         get
         {
            var flags = new List<Flag>();

            if (GetFlags() > 0)
            {
               foreach (var flag in Enum.GetValues(typeof(Flag)).Cast<Flag>())
               {
                  if (GetFlagValue((uint)flag))
                  {
                     flags.Add(flag);
                  }
               }
            }

            return flags;
         }
      }

      #endregion // Fields

      #region Operators

      #endregion // Operators

      #region Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaContainerFlags"/> class.
      /// </summary>
      /// <param name="self">The node element representing the current MediaContainer.</param>
      /// <exception cref="NullReferenceException">The MediaContainerFlags cannot be initialized using
      /// a non-existing element!</exception>
      public MediaContainerFlags(XElement self)
      {
         if (self != null)
         {
            Self = self;
         }
         else
         {
            throw new NullReferenceException("The MediaContainerFlags cannot be initialized using" +
                                             "a non-existing element!");
         }
      }

      #endregion // Constructors

      #region Common Functionality

      /// <summary>
      /// Returns the value of the Flags attribute.
      /// </summary>
      /// <returns>
      /// The integer which encapsulates all the flags set.
      /// </returns>
      private uint GetFlags()
      {
         uint flags = 0x00000000;
         string flagsString = Tools.GetAttributeValue(Self, "Flags");

         if (flagsString.Length > 0)
         {
            flags = Convert.ToUInt32(flagsString, 16);
         }

         return flags;
      }


      /// <summary>
      /// Sets the value of the Flags attribute.
      /// </summary>
      /// <param name="flags">The flags.</param>
      private void SetFlags(uint flags)
      {
         Tools.SetAttributeValue(Self, "Flags", Convert.ToString(flags, 16));
      }

      /// <summary>
      /// Sets the flags supplied and clears the non-listed ones.
      /// </summary>
      /// <param name="flags">The flags to set.</param>
      public void SetFlags(IEnumerable<Flag> flags)
      {
         uint value = 0x00000000;

         foreach (var flag in flags)
         {
            value |= (uint)flag;
         }

         SetFlags(value);
      }

      /// <summary>
      /// Returns the value of the specified flag.
      /// </summary>
      /// <param name="mask">The flag mask.</param>
      /// <returns>
      ///   <c>true</c> if flag is set; otherwise, <c>false</c>.
      /// </returns>
      private bool GetFlagValue(uint mask)
      {
         bool flagValue = false;
         uint flags = GetFlags();

         if ((flags & mask) != 0)
         {
            flagValue = true;
         }

         return flagValue;
      }

      /// <summary>
      /// Sets the value of the supplied flag value.
      /// </summary>
      /// <param name="mask">The flag mask.</param>
      /// <param name="value">if set to <c>true</c> [value].</param>
      private void SetFlagValue(uint mask, bool value)
      {
         uint flags = GetFlags();

         // Read the current value of the flags attribute.
         string flagsString = Tools.GetAttributeValue(Self, "Flags");

         if (value)
         {
            flags |= mask;
         }
         else
         {
            flags &= ~mask;
         }

         // Set the new value of the flags attribute.
         Tools.SetAttributeValue(Self, "Flags", Convert.ToString(flags, 16));

         switch (mask)
         {
            case ((uint)Flag.Favorite):
               NotifyPropertyChanged("Favorite");
               break;

            case ((uint)Flag.Deleted):
               NotifyPropertyChanged("Deleted");
               break;
         }

         NotifyPropertyChanged("Source");
      }

      protected void NotifyPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
         {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
         }
      }

      #endregion // Common Functionality
   }
}
