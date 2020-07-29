using System;
using System.Xml.Linq;
using System.ComponentModel;

namespace MediaCurator
{
   public class MediaContainerFlags : INotifyPropertyChanged
   {
      #region Constants

      public enum FlagsMasks
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
            return GetFlagValue((uint)FlagsMasks.Favorite);
         }

         set
         {
            SetFlagValue((uint)FlagsMasks.Favorite, value);
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
            return GetFlagValue((uint)FlagsMasks.Deleted);
         }

         set
         {
            SetFlagValue((uint)FlagsMasks.Deleted, value);
         }
      }

      #endregion // Fields

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
      /// Returns the value of the specified flag.
      /// </summary>
      /// <param name="mask">The flag mask.</param>
      /// <returns>
      ///   <c>true</c> if flag is set; otherwise, <c>false</c>.
      /// </returns>
      private bool GetFlagValue(uint mask)
      {
         bool flagValue = false;
         string flagsString = Tools.GetAttributeValue(Self, "Flags");

         if (flagsString.Length > 0)
         {
            uint flags = Convert.ToUInt32(flagsString, 16);

            if ((flags & mask) != 0)
            {
               flagValue = true;
            }
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
         uint flags = 0x00000000;

         // Read the current value of the flags attribute.
         string flagsString = Tools.GetAttributeValue(Self, "Flags");

         if (flagsString.Length > 0)
         {
            flags = Convert.ToUInt32(flagsString, 16);
         }

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
            case ((uint)FlagsMasks.Favorite):
               NotifyPropertyChanged("Favorite");
               break;

            case ((uint)FlagsMasks.Deleted):
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
