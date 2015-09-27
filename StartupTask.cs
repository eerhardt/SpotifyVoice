using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Media.SpeechRecognition;
using Windows.Media.Playback;
using Windows.Media.Core;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace BackgroundApplication1
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const string TrackIdKey = "trackid";
        private const string TitleKey = "title";

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral(); // This must be retrieved prior to subscribing to events below which use it

            var speechRecognizer = new SpeechRecognizer(SpeechRecognizer.SystemSpeechLanguage);

            var webSearchGrammar = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.WebSearch, "webSearch");
            speechRecognizer.Constraints.Add(webSearchGrammar);

            SpeechRecognitionCompilationResult compilationResult = await speechRecognizer.CompileConstraintsAsync();

            // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                while (true)
                {
                    var recognitionOperation = speechRecognizer.RecognizeAsync();
                    SpeechRecognitionResult speechRecognitionResult = await recognitionOperation;

                    if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                    {
                        if (speechRecognitionResult.Text.StartsWith("play", StringComparison.OrdinalIgnoreCase))
                        {
                            string song = speechRecognitionResult.Text.Substring(4).Trim();

                            Uri uri = GetSongUri(song);

                            if (uri != null)
                            {
                                var playbackList = new MediaPlaybackList();
                                playbackList.AutoRepeatEnabled = true;

                                // Add playback items to the list
                                //foreach (var song in songs)
                                {
                                    var source = MediaSource.CreateFromUri(uri);
                                    source.CustomProperties[TrackIdKey] = uri;
                                    source.CustomProperties[TitleKey] = "Jag Blaster";
                                    //source.CustomProperties[AlbumArtKey] = song.AlbumArtUri;
                                    playbackList.Items.Add(new MediaPlaybackItem(source));
                                }

                                BackgroundMediaPlayer.Current.IsMuted = false;
                                BackgroundMediaPlayer.Current.Volume = 1;

                                BackgroundMediaPlayer.Current.Source = playbackList;
                                BackgroundMediaPlayer.Current.Play();
                            }
                            //BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;

                            //deferral = taskInstance.GetDeferral(); // This must be retrieved prior to subscribing to events below which use it

                            //BackgroundMediaPlayer.Current.MediaEnded += Current_MediaEnded;
                        }
                        else if (speechRecognitionResult.Text.StartsWith("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            deferral.Complete();
                        }

                    }
                    else
                    {
                        //resultTextBlock.Visibility = Visibility.Visible;
                        //resultTextBlock.Text = string.Format("Speech Recognition Failed, Status: {0}", speechRecognitionResult.Status.ToString());
                    }

                }
            }
        }

        private static Uri GetSongUri(string song)
        {
            string fileName;
            if (song.IndexOf("enter", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fileName = "Enter_Sandman.mp3";
            }
            else if (song.IndexOf("master", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fileName = "Master_Of_Puppets.mp3";
            }
            else if (song.IndexOf("blaster", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fileName = "Jag_Blaster_Melody.mp3";
            }
            else if (song.IndexOf("party", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fileName = "Party_Hard.wma";
            }
            else if (song.IndexOf("smells", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fileName = "Smells_Like_Teen_Spirit.wma";
            }
            else if (song.IndexOf("bad name", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fileName = "You_Give_Love_a_Bad_Name.wma";
            }
            else
            {
                return null;
            }

            return new Uri($"ms-appx:///Assets/{fileName}");
        }
    }
}
