using System;

namespace Arcadeia
{
   /// <summary>
   /// The type representing a photo or video file's resolution. It has a width and a height property
   /// and multiple ways of supplying them.
   /// </summary>
   public class ResolutionType : IComparable, IEquatable<ResolutionType>
   {
      public long Width { get; set; }
      public long Height { get; set; }

      /// <summary>
      /// Initializes a new instance of the <see cref="ResolutionType"/> class having been supplied
      /// the resolution inside a string of "WxH" format.
      /// </summary>
      /// <param name="resolution">The resolution with width and height separated using 'x'.</param>
      public ResolutionType(string resolution)
      {
         // Initialize the Width and Height.
         Width = Height = 0;

         try
         {
            if (resolution.Contains('x'))
            {
               Width = Convert.ToInt64(resolution.Substring(0, resolution.IndexOf('x')));
               Height = Convert.ToInt64(resolution[(resolution.IndexOf('x') + 1)..]);
            }
         }
         catch (Exception)
         {
            // 0x0 it is then!
            return;
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="ResolutionType"/> class having been supplied
      /// the width and the height.
      /// </summary>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      public ResolutionType(long width, long height)
      {
         Width = width;
         Height = height;
      }

      /// <summary>
      /// Parameterless constructor used for deserialization of JSON values.
      /// </summary>
      public ResolutionType()
      {

      }

      /// <summary>
      /// Returns a <see cref="System.String" /> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String" /> that represents this instance. The format in which the
      /// resolution is returned is "WxH".
      /// </returns>
      public override string ToString()
      {
         string resolution = "";

         if ((Width > 0) && (Height > 0))
         {
            resolution = string.Format("{0}x{1}", Width, Height);
         }

         return resolution;
      }

      public int CompareTo(object? obj)
      {
         if (obj == null) return 1;

         if (obj is not ResolutionType other)
         {
            throw new ArgumentException("The supplied object is not a ResolutionType object.");
         }

         if (((other.Width == Width) && (other.Height == Height)) || ((other.Width + other.Height) == (Width + Height))) return 0;

         if ((other.Width + other.Height) > (Width + Height)) return -1;

         return 1;
      }

      public bool Equals(ResolutionType? other)
      {
         if (other is null)
         {
            return false;
         }

         // Optimization for a common success case.
         if (Object.ReferenceEquals(this, other))
         {
            return true;
         }

         // If run-time types are not exactly the same, return false.
         if (this.GetType() != other.GetType())
         {
            return false;
         }

         // Return true if the fields match.
         return (Width == other.Width) && (Height == other.Height);
      }

      public static bool operator ==(ResolutionType lhs, ResolutionType rhs)
      {
         if (lhs is null)
         {
            if (rhs is null)
            {
               return true;
            }

            // Only the left side is null.
            return false;
         }

         // Equals handles case of null on right side.
         return lhs.Equals(rhs);
      }

      public static bool operator !=(ResolutionType lhs, ResolutionType rhs) => !(lhs == rhs);

      public override bool Equals(object? obj)
      {
         return Equals(obj as ResolutionType);
      }

      public override int GetHashCode()
      {
         throw new NotImplementedException();
      }
   }
}