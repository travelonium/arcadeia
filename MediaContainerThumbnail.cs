using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;

namespace MediaMonster
{
   /// <summary>
   /// The different supported caching methods.
   /// </summary>
   public enum CachingMethods
   {
      [Description("None")]
      None = 0,

      [Description("On Demand")]
      OnDemand = 1,

      [Description("On Load")]
      OnLoad = 2,

      [Description("Background")]
      Background = 3
   }

   class MediaContainerThumbnail : INotifyPropertyChanged
   {
      private CachingMethods _Caching = CachingMethods.None;
      private string _DefaultSource = "pack://application:,,,/Icons/256x144/File.png";
      private CancellationTokenSource _AnimationCancellationToken = new CancellationTokenSource();
      private List<byte[]> _Thumbnails = Enumerable.Repeat<byte[]>(null, Properties.Settings.Default.MaximumThumbnailsCount).ToList();

      private string _ID
      {
         get;
         set;
      }

      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Gets or sets the currently being displayed thumbnail.
      /// </summary>
      /// <value>
      /// The ImageSource instance containing the thumbnail to be displayed.
      /// </value>
      public ImageSource Source { get; set; }

      /// <summary>
      /// Returns an instance of <see cref="ThumbnailsDatabase"/> class.
      /// </summary>
      private ThumbnailsDatabase Database
      {
         get
         {
            return ThumbnailsDatabase.Instance;
         }
      }

      public byte[] this[int index]
      {
         get
         {
            byte[] thumbnail = null;

            switch (_Caching)
            {
               case CachingMethods.None:

                  thumbnail = Database.GetThumbnail(_ID, index);

                  break;

               case CachingMethods.OnDemand:

                  if (_Thumbnails[index] == null)
                  {
                     _Thumbnails.Insert(index, Database.GetThumbnail(_ID, index));
                  }

                  thumbnail = _Thumbnails[index];

                  break;

               case CachingMethods.OnLoad:

                  thumbnail = _Thumbnails[index];

                  break;

               case CachingMethods.Background:

                  throw new NotImplementedException("This caching method has not yet been implemented!");

                  break;
            }

            return thumbnail;
         }
      }

      /// <summary>
      /// Gets the total number of existing thumbnails in existence for this particular file.
      /// </summary>
      /// <value>
      /// The number of thumbnails available.
      /// </value>
      public int Count
      {
         get
         {
            return Database.GetThumbnailsCount(_ID);
         }
      }

      public AnimationPositionType AnimationPosition { get; set; }

      /// <summary>
      /// Gets or sets the animation tasks count currently running. This is meant for debugging
      /// purposes.
      /// </summary>
      /// <value>
      /// The running animation tasks count.
      /// </value>
      private static int AnimationTasksCount { get; set; }

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaContainerThumbnail"/> class having received a 
      /// resource or physical file path as the source. A resource file source should be in a similar
      /// format as "pack://application:,,,/Icons/256x144/Folder.png".
      /// </summary>
      /// <param name="thumbnailSource">The thumbnail source.</param>
      public MediaContainerThumbnail(string thumbnailSource)
      {
         // Initialize the AnimationPosition regardless of what happens next.
         AnimationPosition = new AnimationPositionType();

         // Set the Thumbnail Source to the supplied value.
         SetThumbnailSource(thumbnailSource);
      }

      public MediaContainerThumbnail(string id, CachingMethods caching)
      {
         // Associate the current instance with the Id of the owner.
         _ID = id;

         // Set the caching method to be used by this instance.
         _Caching = caching;

         // Initialize the AnimationPosition regardless of what happens next.
         AnimationPosition = new AnimationPositionType();

         // Set the default thumbnail for the current MediaContainer.
         SetThumbnailSource(_DefaultSource);

         switch (_Caching)
         {
            case CachingMethods.None:

               break;

            case CachingMethods.OnDemand:

               break;

            case CachingMethods.OnLoad:

               // Retrieve and cache all the thumbnails of the current MediaContainer.
               _Thumbnails = Database.GetThumbnails(id);

               break;

            case CachingMethods.Background:

               throw new NotImplementedException("This caching method has not yet been implemented!");

               break;
         }
      }

      ~MediaContainerThumbnail()
      {

      }

      public async Task<byte[]> GetAsync(int index, CancellationToken cancellationToken)
      {
         byte[] thumbnail = null;

         switch (_Caching)
         {
            case CachingMethods.None:

               thumbnail = await Database.GetThumbnailAsync(_ID, index, cancellationToken);

               break;

            case CachingMethods.OnDemand:

               if (_Thumbnails[index] == null)
               {
                  _Thumbnails.Insert(index, await Database.GetThumbnailAsync(_ID, index, cancellationToken));
               }

               thumbnail = _Thumbnails[index];

               break;

            case CachingMethods.OnLoad:

               thumbnail = _Thumbnails[index];

               break;

            case CachingMethods.Background:

               throw new NotImplementedException("This caching method has not yet been implemented!");

               break;
         }

         return thumbnail;
      }

      private void SetThumbnailSource(string source)
      {
         if (source != "")
         {
            var newSource = new BitmapImage(new Uri(source));

            newSource.Freeze();
            Source = newSource;
         }
         else
         {
            Source = null;
         }

         NotifyPropertyChanged("Source");
         NotifyPropertyChanged("AnimationPosition");
      }

      private void SetThumbnailSource(BitmapImage source)
      {
         Source = source;

         NotifyPropertyChanged("Source");
         NotifyPropertyChanged("AnimationPosition");
      }

      private bool SetThumbnailSource(byte[] source)
      {
         if ((source == null) || (source.Length == 0))
         {
            return false;
         }

         using (var ms = new System.IO.MemoryStream((byte[])source))
         {
            var thumbnail = new BitmapImage();

            thumbnail.BeginInit();
            thumbnail.CacheOption = BitmapCacheOption.OnLoad;
            thumbnail.StreamSource = ms;
            thumbnail.EndInit();
            thumbnail.Freeze();

            Source = thumbnail;
         }

         NotifyPropertyChanged("Source");
         NotifyPropertyChanged("AnimationPosition");

         return true;
      }

      public async void StartAnimation()
      {
         _AnimationCancellationToken = new CancellationTokenSource();

         Task thumbnailAnimationTask = AnimationTask(_AnimationCancellationToken.Token);
         await thumbnailAnimationTask;
      }

      public void StopAnimation()
      {
         _AnimationCancellationToken.Cancel();
      }

      private Task AnimationTask(CancellationToken cancellationToken)
      {
         int index;
         int count = AnimationPosition.Length = Properties.Settings.Default.MaximumThumbnailsCount - 1;

         return Task.Factory.StartNew(async delegate
        {
           AnimationTasksCount++;

           Debug.WriteLine(String.Format("( {0,2} ) Thumbnail Task Started...", AnimationTasksCount));

           while (!cancellationToken.IsCancellationRequested)
           {
              bool empty = true;

              for (index = 0;
                   index <= count && !cancellationToken.IsCancellationRequested;
                   index++)
              {
                  // Update the Animation ProgressBar position.
                  AnimationPosition.Position = index;

                 try
                 {
                     // Set the thumbnail source.
                     if (SetThumbnailSource(await GetAsync(index, cancellationToken)))
                    {
                        // The index contained a valid thumbnail.
                        empty = false;
                    }

                     // Wait a short while before displaying the next frame.
                     await Task.Delay((int)Properties.Settings.Default.ThumbnailAnimationFrameDelay,
                                      cancellationToken);
                 }
                 catch (TaskCanceledException)
                 {
                    break;
                 }
                 catch (OperationCanceledException)
                 {
                    break;
                 }
              }

              if (empty)
              {
                  // This file lacks any thumbnails. No point in keeping the task running...
                  break;
              }
           }

            // Looks like a cancellation has been requested. Initialize the Animation ProgressBar Position.
            AnimationPosition.Position = 0;

           if (_Caching == CachingMethods.None)
           {
               // Clear the currently displayed thumbnail.
               SetThumbnailSource(_DefaultSource);
           }

           AnimationTasksCount--;

           Debug.WriteLine(String.Format("( {0,2} ) Thumbnail Task Stopped...", AnimationTasksCount));

        }, TaskCreationOptions.LongRunning);
      }

      protected void NotifyPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
         {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
         }
      }
   }

   public class AnimationPositionType
   {
      /// <summary>
      /// Total count of thumbnail files.
      /// </summary>
      public int Length { get; set; }

      /// <summary>
      /// The current thumbnail index within the list of thumbnail files.
      /// </summary>
      public int Position { get; set; }

      /// <summary>
      /// Gets the visibility of the ProgressBar.
      /// </summary>
      /// <value>
      /// The visibility property of the ProgressBar which can either be "Visible" or "Hidden".
      /// </value>
      public string Visibility
      {
         get
         {
            return (Length > 1) ? "Visible" : "Hidden";
         }
      }

      public AnimationPositionType()
      {
         Length = 0;
         Position = 0;
      }
   }
}
