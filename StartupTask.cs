using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Media.SpeechRecognition;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using Windows.Data.Json;

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

            using (MopidyClient client = new MopidyClient())
            {
                await client.Open();
                await client.Play("spotify:track:1hKdDCpiI9mqz1jVHRKG0E");

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
                                string playSearchString = speechRecognitionResult.Text.Substring(4).Trim();

                                string uri;
                                if (playSearchString.StartsWith("artist", StringComparison.OrdinalIgnoreCase))
                                {
                                    uri = await client.SearchArtist(playSearchString.Substring(6).Trim());
                                }
                                else
                                {
                                    uri = await client.Search(playSearchString);
                                }

                                if (uri != null)
                                {
                                    await client.Play(uri);
                                }
                            }
                            else if (speechRecognitionResult.Text.StartsWith("stop", StringComparison.OrdinalIgnoreCase))
                            {
                                await client.Stop();
                            }
                            else if (speechRecognitionResult.Text.StartsWith("louder", StringComparison.OrdinalIgnoreCase))
                            {
                                int volume = await client.GetVolume();
                                volume = Math.Min(volume + 10, 100);
                                await client.SetVolume(volume);
                            }
                            else if (speechRecognitionResult.Text.StartsWith("quieter", StringComparison.OrdinalIgnoreCase))
                            {
                                int volume = await client.GetVolume();
                                volume = Math.Max(volume - 10, 0);
                                await client.SetVolume(volume);
                            }
                            else if (speechRecognitionResult.Text.StartsWith("mute", StringComparison.OrdinalIgnoreCase))
                            {
                                await client.SetVolume(0);
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
        }
    }
}
