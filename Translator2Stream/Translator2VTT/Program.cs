using Media.Captions.WebVTT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Translator2VTT
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a translate.it file path");
                return 1;
            }

            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(args[0]);
            }
            catch (Exception)
            {
                Console.WriteLine("File not found");
                return 1;
            }

            MediaCaptions transcript = new MediaCaptions();
            List<Cue> cueList = new List<Cue>();
            string header = "Presenter @ ";
            string translation = "Translation";
            TimeSpan origin = new TimeSpan();
            TimeSpan currentCaption;
            bool firstCaption = true;
            Cue phrase = new Cue();

            foreach (string line in lines)
            {
                // Assumption: the translation always have Presenter / Translation / Recognition lines with the format:
                /*
                    Presenter @ 10:28:04

                    Translation (English): It's in English, too.

                    Recognition (Spanish): (Está en inglés, también.)
                 */

                // get time from Caption
                if (line.Contains(header) && firstCaption == false)
                {
                    currentCaption = TimeSpan.Parse(line.Substring(translation.Length));
                    TimeSpan diff = currentCaption.Subtract(origin);
                    phrase.Start = TimeSpan.FromSeconds(diff.TotalSeconds);
                    phrase.End = phrase.Start.Add(TimeSpan.FromSeconds(5));
                }

                // get baseline Time from first Caption
                if (line.Contains(header) && firstCaption == true)
                {
                    origin = TimeSpan.Parse(line.Substring(header.Length));
                    TimeSpan diff = origin.Subtract(origin);
                    phrase.Start = TimeSpan.FromSeconds(diff.TotalSeconds);
                    phrase.End = phrase.Start.Add(TimeSpan.FromSeconds(5));
                    firstCaption = false;
                }

                // get text from Caption
                if (line.StartsWith(translation))
                {
                    Span[] translatedCaption = new Span[]
                    {
                    new Span() { Type = SpanType.Text, Text = line.Substring((line.IndexOf("): ") + 3)) },
                    };

                    phrase.Content = translatedCaption;
                    
                    cueList.Add(phrase);

                    phrase = new Cue();
                }
            }

            // Set a default end time, and update in a second loop
            // for clarity
            for (int item = 0; item < cueList.Count - 1; item++) {
                cueList[item].End = cueList[item + 1].Start;
            }

            Cue[] Cues = cueList.ToArray();
            transcript.Cues = Cues;

            StringBuilder sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            WebVttSerializer.SerializeAsync(transcript, writer).ConfigureAwait(false).GetAwaiter().GetResult();

            File.WriteAllText(String.Concat(args[0],".vtt"), sb.ToString());

            Console.WriteLine("Press any key to exit. {0} File saved", String.Concat(args[0], ".vtt"));
            System.Console.ReadKey();

            return 0;
        }
    }
}
