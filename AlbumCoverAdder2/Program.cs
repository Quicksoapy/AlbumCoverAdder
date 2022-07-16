using System.Diagnostics;
using FFMpegCore;
using LastFM.AspNetCore.Stats;
using LastFM.AspNetCore.Stats.Entities;
using LastFM.AspNetCore.Stats.Utils;

Console.WriteLine("directory:");
string? directory = Console.ReadLine();
Console.WriteLine("Api key:");
string? apiKey = Console.ReadLine();
Console.WriteLine("Shared secret:");
string? sharedSecret = Console.ReadLine();
Console.WriteLine("0 for making a new directory and keeping old one, 1 for overwriting current directory (backup before continuing recommended)");
bool? overwriteBool = Convert.ToBoolean(Convert.ToInt16(Console.ReadLine()) );

DateTime oldTime = DateTime.UnixEpoch;

LastFMCredentials credentials = new LastFMCredentials()
{
    APIKey = apiKey,
    SharedSecret = sharedSecret
};

LastFMStatsController lastFmStatsController = new LastFMStatsController(credentials);

GlobalFFOptions.Current.Encoding = System.Text.Encoding.UTF8;


string[] supportedFormats = new[] { ".mp3", ".flac", ".ogg", ".wav" };

string[] musicFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
    .Where(f => supportedFormats.Contains(System.IO.Path.GetExtension(f).ToLower())).ToArray();

foreach (var musicFile in musicFiles)
{
    var mediaInfo = await FFProbe.AnalyseAsync(musicFile);

    if (mediaInfo.VideoStreams.Count > 0)
    {
        continue;
    }
    
    mediaInfo.Format.Tags.TryGetValue("album", out var album);
    
    mediaInfo.Format.Tags.TryGetValue("album_artist", out var albumArtist);

    mediaInfo.Format.Tags.TryGetValue("artist", out var artist);
    
    mediaInfo.Format.Tags.TryGetValue("title", out var title);
    
    var formatName = mediaInfo.Format.FormatName;

    if (string.IsNullOrEmpty(albumArtist))
    {
        Console.WriteLine("File: " + musicFile + " does not have an album artist applied in the metadata of the file. Trying to use artist instead...");
        
        if (string.IsNullOrEmpty(artist))
        {
            Console.WriteLine("File: " + musicFile + " does not have artist in metadata either. skipping"); 
            continue;
        }
    }
    
    if (string.IsNullOrEmpty(album))
    {
        Console.WriteLine("File: " + musicFile + " does not have an album applied in the metadata of the file.");
        continue;
    }

    

    var directoryFile = Directory.GetParent(musicFile);
    
    try
    {
        Album albumInfo;

        if (oldTime.AddSeconds(1) < DateTime.UnixEpoch)
        {
            Thread.Sleep(oldTime.AddSeconds(1) - oldTime);
        }
        
        if (string.IsNullOrEmpty(albumArtist))
        {
            albumInfo = await lastFmStatsController.GetAlbumInfo(artist, album);
        }
        else
        {
            albumInfo = await lastFmStatsController.GetAlbumInfo(albumArtist, album);
        }
        oldTime = DateTime.UnixEpoch;
        
        if (albumInfo.Image.Uri == null)
        {
            Console.WriteLine("File: " + musicFile + " have not found album cover. The metadata is wrong or last.fm does not have a cover for that album.");
            continue;
        }
        
        //i couldn't think of a better way to do this lmao
        string stupidString = "";
        if (overwriteBool == false)
        {
            stupidString = "ACA";
        }

        if (!Directory.Exists(directoryFile + stupidString))
        {
            Directory.CreateDirectory(directoryFile + stupidString);
        }
        using (Process p = new Process())
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "ffmpeg.exe";
            p.StartInfo.Arguments = "-i \""+ musicFile +"\" -i  \""+  albumInfo.Image.Uri.ToString() +"\" -map 0:a -map 1 -codec copy -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover (front)\" -disposition:v attached_pic \"" + directoryFile + stupidString + "\\" + title + " - " + albumArtist + "." + formatName + "\"";
            p.Start();
            p.WaitForExit();
        }

        if (overwriteBool == true)
        {
            File.Delete(musicFile);   
        }
    }
    
    catch (Exception e)
    {
        Console.WriteLine("Album image has probably not been found cuz probably the metadata is incorrect \n\n" + e);
    }
    
}
