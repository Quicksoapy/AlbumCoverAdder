using System.Diagnostics;
using System.Drawing;
using FFMpegCore;
using LastFM.AspNetCore.Stats;
using LastFM.AspNetCore.Stats.Utils;

string? directory = Console.ReadLine();

LastFMCredentials credentials = new LastFMCredentials()
{
    APIKey = "a033dfb817a58a9e9ff37b463ddb7624",
    SharedSecret = "8cfae5cc25c9b0295b405517746980c6"
};

LastFMStatsController lastFmStatsController = new LastFMStatsController(credentials);

bool timerElapsed = false;

var timer = new System.Timers.Timer(1000);

timer.Elapsed += TimerElapsedEvent;

GlobalFFOptions.Current.Encoding = System.Text.Encoding.UTF8;


string[] supportedFormats = new[] { ".mp3", ".flac", ".ogg", ".wav" };

string[] musicFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
    .Where(f => supportedFormats.Contains(System.IO.Path.GetExtension(f).ToLower())).ToArray();

foreach (var musicFile in musicFiles)
{
    timer.Enabled = true;
    var mediaInfo = await FFProbe.AnalyseAsync(musicFile);

    if (mediaInfo.VideoStreams.Count > 0)
    {
        continue;
    }
    
    mediaInfo.Format.Tags.TryGetValue("album", out var album);
    
    mediaInfo.Format.Tags.TryGetValue("album_artist", out var artist);
    
    mediaInfo.Format.Tags.TryGetValue("title", out var title);
    
    var formatName = mediaInfo.Format.FormatName;

    if (string.IsNullOrEmpty(artist))
    {
        Console.WriteLine("File: " + musicFile + " does not have an album artist applied in the metadata of the file.");
        continue;
    }
    
    if (string.IsNullOrEmpty(album))
    {
        Console.WriteLine("File: " + musicFile + " does not have an album applied in the metadata of the file.");
        continue;
    }

    var directoryFile = Directory.GetParent(musicFile);
    
    try
    {
        var albumInfo = await lastFmStatsController.GetAlbumInfo(artist, album);
        using (Process p = new Process())
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "CMD.exe";
            p.StartInfo.Arguments = "ffmpeg -i \""+ musicFile +"\" -i  \""+  albumInfo.Image.Uri.ToString() +"\" -map 0:a -map 1 -codec copy -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover (front)\" -disposition:v attached_pic \"" + directoryFile + "\\" + title + " - " + artist + "." + formatName + "\" & exit /b";
            p.Start();
            p.WaitForExit();
        }

        //File.Delete(musicFile);
    }
    catch (Exception e)
    {
        Console.WriteLine("Album image has probably not been found cuz probably the metadata is incorrect \n\n" + e);
        throw;
    }
    
}

void TimerElapsedEvent(Object source, System.Timers.ElapsedEventArgs e)
{
    timerElapsed = true;
}

System.Drawing.Image DownloadImageFromUrl(string imageUrl)
{
    System.Drawing.Image image = null;
     
    try
    {
        System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
        webRequest.AllowWriteStreamBuffering = true;
        webRequest.Timeout = 30000;
     
        System.Net.WebResponse webResponse = webRequest.GetResponse();
     
        System.IO.Stream stream = webResponse.GetResponseStream();
     
        image = System.Drawing.Image.FromStream(stream);
     
        webResponse.Close();
    }
    catch (Exception ex)
    {
        return null;
    }
     
    return image;
}