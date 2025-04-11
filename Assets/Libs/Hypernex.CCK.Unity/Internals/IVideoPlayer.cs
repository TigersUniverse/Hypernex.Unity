using System;

namespace Hypernex.CCK.Unity.Internals
{
    public interface IVideoPlayer : IDisposable
    {
        /// <summary>
        /// If the Media is playing
        /// </summary>
        bool IsPlaying { get; }
        /// <summary>
        /// Controls if the audio is muted
        /// </summary>
        bool Muted { get; set; }
        /// <summary>
        /// Controls of the video is looping
        /// </summary>
        bool Looping { get; set; }
        /// <summary>
        /// Controls the pitch of the audio
        /// </summary>
        float Pitch { get; set; }
        /// <summary>
        /// Controls the volume of the audio
        /// </summary>
        float Volume { get; set; }
        /// <summary>
        /// Controls the position of the media
        /// </summary>
        double Position { get; set; }
        /// <summary>
        /// Gets the length of the media
        /// </summary>
        double Length { get; }
        /// <summary>
        /// Sets the source media of the Video
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// Plays the video
        /// </summary>
        void Play();
        /// <summary>
        /// Pauses the video
        /// </summary>
        void Pause();
        /// <summary>
        /// Stops the video
        /// </summary>
        void Stop();

        /// <summary>
        /// The schematic for a local file. Most video players accept "file:///", but some are special.
        /// </summary>
        /// <returns>The local file schematic</returns>
        string GetFileHeader();
    }
}