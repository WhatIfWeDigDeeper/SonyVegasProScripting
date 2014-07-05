/**
 * Usage: add folder "
 * 
 * Modified from Batch Render Sample script that performs batch renders with GUI for selecting
 * render templates.
 *
 * Revision Date: Jun. 28, 2006.
 **/
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Media;

using Sony.Vegas;

public class EntryPoint
{
    /// <summary>
    /// Change the file name here if you need multiple renderers.  Then save as.
    /// </summary>
    String _defaultInputFileName = "MyRendererInput.txt";

    /// <summary>
    /// directory for input file My Documents/SonyVegasRender/
    /// </summary>
    String _defaultInputDirectory = "SonyVegasRender";
    
    /// <summary>
    /// Writes all renderers to VegasRenderList.txt in above directory.
    /// </summary>
    bool _writeAllRenderers = true;

    /// <summary>
    /// set this to true if you want to allow files to be overwritten
    /// </summary>
    bool OverwriteExistingFiles = true;

    /// <summary>
    /// Only default.  Reads render mode from 1st line in input file.
    /// </summary>
    RenderMode _renderMode = RenderMode.Project;

    String defaultBasePath = "Untitled_";

    Sony.Vegas.Vegas myVegas = null;

    enum RenderMode
    {
        Project = 0,
        Selection,
        Regions,
    }

    public void FromVegas(Vegas vegas)
    {
        myVegas = vegas;

        String myDocDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        String inputFileDirectory = Path.Combine(myDocDirectory, _defaultInputDirectory);
        String inputFilePath = Path.Combine(inputFileDirectory, _defaultInputFileName);

        String projectPath = myVegas.Project.FilePath;
        if (String.IsNullOrEmpty(projectPath))
        {
            String dir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            defaultBasePath = Path.Combine(dir, defaultBasePath);
        }
        else
        {
            String dir = Path.GetDirectoryName(projectPath);
            String fileName = Path.GetFileNameWithoutExtension(projectPath);
            defaultBasePath = Path.Combine(dir, fileName + "_");
        }
        if (_writeAllRenderers)
            WriteAvailableRenderers(inputFileDirectory);

        ArrayList lookup =  ReadInputFile(inputFilePath);
        ArrayList selectedTemplates = FindMatchingTemplates(lookup);
        if (selectedTemplates.Count == 0)
        {
            MessageBox.Show("No templates found.");
            return;
        }

        DoBatchRender(selectedTemplates, defaultBasePath, _renderMode);

        string soundFile = Path.Combine(inputFileDirectory, "EndSound.wav");
        if (File.Exists(soundFile))
        {
            (new SoundPlayer(soundFile)).Play();
        }
        else
        {
            SystemSounds.Asterisk.Play();
            SystemSounds.Asterisk.Play();
        }

    }

    public ArrayList ReadInputFile(String inputFilePath)
    {
        String line = null;
        ArrayList lookup = new ArrayList();

        if (!File.Exists(inputFilePath))
        {
            string[] content = new string[2];
            content[0] = "Project  #valid choices: Project Regions Selection. Sample below, remove # to enable.";
            content[1] = "# Wave (Microsoft);44,100 Hz, 24 Bit, Stereo, PCM";
            File.WriteAllLines(inputFilePath, content);
            MessageBox.Show("Created " + inputFilePath + "  Please look in this directory for VegasRendererList.txt and add needed renderers to MyRendererInput.txt");
            return lookup;
        }

        using (System.IO.StreamReader file = new System.IO.StreamReader(inputFilePath))
        {
            string firstLine = file.ReadLine();
            SetProjectType(firstLine);
            while ((line = file.ReadLine()) != null)
            {
                //skip blank lines
                if (line.Trim().Length == 0)
                    continue;
                string[] templates = line.Split(';');
                if (templates.Length == 1 || templates[1].Trim().Length == 0)
                {
                    MessageBox.Show("Invalid line in input file.  Make sure you have 2 items seperated by ';' " + line);
                    continue;
                }
                String templateName = templates[0].Trim();
                if (templateName.StartsWith("#"))
                {
                    continue;
                }
                String formatName = templates[1].Trim();
                lookup.Add(new KeyValuePair(templateName, formatName));
            }
        }
        return lookup;
    }

    private ArrayList FindMatchingTemplates(ArrayList lookup)
    {
        ArrayList result = new ArrayList();
        for (int i = 0; i < lookup.Count; i++)
        {
            KeyValuePair keyVal = (KeyValuePair)lookup[i];
            RenderItem item = FindMatch(keyVal.RendererName, keyVal.TemplateName);
            if (item == null)
            {
                MessageBox.Show("Could not find renderer " + keyVal.RendererName + " and template " + keyVal.TemplateName);
                continue;
            }
            result.Add(item);
        }
        return result;
    }

    private RenderItem FindMatch(string renderRequest, string templateRequest)
    {
        int projectAudioChannelCount = 0;
        if (AudioBusMode.Stereo == myVegas.Project.Audio.MasterBusMode)
        {
            projectAudioChannelCount = 2;
        }
        else if (AudioBusMode.Surround == myVegas.Project.Audio.MasterBusMode)
        {
            projectAudioChannelCount = 6;
        }
        
        bool projectHasVideo = ProjectHasVideo();
        bool projectHasAudio = ProjectHasAudio();
        foreach (Renderer renderer in myVegas.Renderers)
        {
            try
            {
                String rendererName = renderer.FileTypeName.Trim(); 
                if (!rendererName.Equals(renderRequest, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                foreach (RenderTemplate template in renderer.Templates)
                {
                    try
                    {
                        if (!IsTemplateValid(projectAudioChannelCount, projectHasVideo, projectHasAudio, template))
                            continue;

                        String templateName = template.Name;
                        if (!templateName.Equals(templateRequest, StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        return new RenderItem(renderer, template, template.FileExtensions[0]);
                    }
                    catch (Exception e)
                    {
                        // skip it
                        MessageBox.Show(e.ToString());
                    }
                }
            }
            catch
            {
                // skip it
            }
        }
        return null;

    }

    private static bool IsTemplateValid(int projectAudioChannelCount, bool projectHasVideo, bool projectHasAudio, RenderTemplate template)
    {
        // filter out invalid templates
        if (!template.IsValid())
        {
            return false; 
        }
        // filter out video templates when project has
        // no video.
        if (!projectHasVideo && (0 < template.VideoStreamCount))
        {
            return false;
        }
        // filter out audio-only templates when project has no audio
        if (!projectHasAudio && (0 == template.VideoStreamCount) && (0 < template.AudioStreamCount))
        {
            return false;
        }
        // filter out templates that have more channels than the project
        if (projectAudioChannelCount < template.AudioChannelCount)
        {
            return false;
        }
        // filter out templates that don't have
        // exactly one file extension
        String[] extensions = template.FileExtensions;
        if (1 != extensions.Length)
        {
            return false;
        }
        return true;
    }

    private void WriteAvailableRenderers(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        string outputFilePath = Path.Combine(outputDirectory, "VegasRendererList.txt");
        using (StreamWriter writer = new StreamWriter(outputFilePath, false))
        {
            foreach (Renderer renderer in myVegas.Renderers)
            {
                try
                {
                    String rendererName = renderer.FileTypeName;
                    writer.WriteLine("Renderer: " + renderer.FileTypeName);
                    writer.WriteLine("-----------------------------------");

                    foreach (RenderTemplate template in renderer.Templates)
                    {
                        try
                        {
                            // filter out invalid templates
                            if (!template.IsValid())
                            {
                                continue;
                            }

                            // filter out templates that don't have
                            // exactly one file extension
                            String[] extensions = template.FileExtensions;
                            if (1 != extensions.Length)
                            {
                                continue;
                            }
                            String templateName = template.Name;
                            writer.WriteLine(renderer.FileTypeName + ";" + template.Name);
                        }
                        catch (Exception e)
                        {
                            // skip it
                            MessageBox.Show(e.ToString());
                        }
                    }
                    writer.WriteLine();
                    writer.WriteLine();
                }
                catch
                {
                    // skip it
                }
            }

        }  //using file

    }

    private void SetProjectType(string line)
    {
        int end = line.IndexOf('#');
        if (end == -1)
            return;
        string type = line.Substring(0, end).ToLower().Trim();
        if (type.Equals("selection"))
            this._renderMode = RenderMode.Selection;
        else if (type.Equals("regions"))
            this._renderMode = RenderMode.Regions;
        else
            this._renderMode = RenderMode.Project;
    }

    void DoBatchRender(ArrayList selectedTemplates, String basePath, RenderMode renderMode)
    {
        String outputDirectory = Path.GetDirectoryName(basePath);
        String baseFileName = Path.GetFileName(basePath);

        // make sure templates are selected
        if ((null == selectedTemplates) || (0 == selectedTemplates.Count))
            throw new ApplicationException("No render templates selected.");

        // make sure the output directory exists
        if (!Directory.Exists(outputDirectory))
            throw new ApplicationException("The output directory does not exist.");

        RenderStatus status = RenderStatus.Canceled;

        // enumerate through each selected render template
        foreach (RenderItem renderItem in selectedTemplates)
        {
            // construct the file name (most of it)
            String filename = Path.Combine(outputDirectory,
                                           FixFileName(baseFileName) +
                                           FixFileName(renderItem.Renderer.FileTypeName) +
                                           "_" +
                                           FixFileName(renderItem.Template.Name));

            
            //ShowRenderInfo(renderItem, filename);

            if (RenderMode.Regions == renderMode)
            {
                int regionIndex = 0;
                foreach (Sony.Vegas.Region region in myVegas.Project.Regions)
                {
                    String regionFilename = String.Format("{0}[{1}]{2}",
                                                          filename,
                                                          regionIndex.ToString(),
                                                          renderItem.Extension);
                    // Render the region
                    status = DoRender(regionFilename, renderItem, region.Position, region.Length);
                    if (RenderStatus.Canceled == status) break;
                    regionIndex++;
                }
            }
            else
            {
                filename += renderItem.Extension;
                Timecode renderStart, renderLength;
                if (renderMode == RenderMode.Selection)
                {
                    renderStart = myVegas.SelectionStart;
                    renderLength = myVegas.SelectionLength;
                }
                else
                {
                    renderStart = new Timecode();
                    renderLength = myVegas.Project.Length;
                }
                status = DoRender(filename, renderItem, renderStart, renderLength);
            }
            if (RenderStatus.Canceled == status) break;
        }
    }

    private static void ShowRenderInfo(RenderItem renderItem, String filename)
    {
        StringBuilder msg = new StringBuilder("Render info\n");
        msg.Append("\n    file name: ");
        msg.Append(filename);
        msg.Append("\n    Renderer: ");
        msg.Append(renderItem.Renderer.FileTypeName);
        msg.Append("\n    Template: ");
        msg.Append(renderItem.Template.Name);
        msg.Append("\n    Start Time: ");
        MessageBox.Show(msg.ToString());
    }

    // perform the render.  The Render method returns a member of the
    // RenderStatus enumeration.  If it is anything other than OK,
    // exit the loops.  This will throw an error message string if the
    // render does not complete successfully.
    RenderStatus DoRender(String filePath, RenderItem renderItem, Timecode start, Timecode length)
    {
        ValidateFilePath(filePath);

        // make sure the file does not already exist
        if (!OverwriteExistingFiles && File.Exists(filePath))
        {
            throw new ApplicationException("File already exists: " + filePath);
        }

        // perform the render.  The Render method returns
        // a member of the RenderStatus enumeration.  If
        // it is anything other than OK, exit the loops.
        RenderStatus status = myVegas.Render(filePath, renderItem.Template, start, length);

        switch (status)
        {
            case RenderStatus.Complete:
            case RenderStatus.Canceled:
                break;
            case RenderStatus.Failed:
            default:
                StringBuilder msg = new StringBuilder("Render failed:\n");
                msg.Append("\n    file name: ");
                msg.Append(filePath);
                msg.Append("\n    Renderer: ");
                msg.Append(renderItem.Renderer.FileTypeName);
                msg.Append("\n    Template: ");
                msg.Append(renderItem.Template.Name);
                msg.Append("\n    Start Time: ");
                msg.Append(start.ToString());
                msg.Append("\n    Length: ");
                msg.Append(length.ToString());
                throw new ApplicationException(msg.ToString());
        }
        return status;
    }

    String FixFileName(String name)
    {
        const Char replacementChar = '-';
        foreach (char badChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(badChar, replacementChar);
        }
        return name;
    }

    void ValidateFilePath(String filePath)
    {
        if (filePath.Length > 260)
            throw new ApplicationException("File name too long: " + filePath);
        foreach (char badChar in Path.GetInvalidPathChars())
        {
            if (0 <= filePath.IndexOf(badChar))
            {
                throw new ApplicationException("Invalid file name: " + filePath);
            }
        }
    }

    class RenderItem
    {
        public readonly Renderer Renderer = null;
        public readonly RenderTemplate Template = null;
        public readonly String Extension = null;

        public RenderItem(Renderer r, RenderTemplate t, String e)
        {
            this.Renderer = r;
            this.Template = t;
            // need to strip off the extension's leading "*"
            if (null != e) this.Extension = e.TrimStart('*');
        }
    }

    bool ProjectHasVideo()
    {
        foreach (Track track in myVegas.Project.Tracks)
        {
            if (track.IsVideo())
            {
                return true;
            }
        }
        return false;
    }

    bool ProjectHasAudio()
    {
        foreach (Track track in myVegas.Project.Tracks)
        {
            if (track.IsAudio())
            {
                return true;
            }
        }
        return false;
    }

}

public class KeyValuePair
{
    public string RendererName;
    public string TemplateName;

    public KeyValuePair(string rendererName, string templateName)
    {
        this.RendererName = rendererName;
        this.TemplateName = templateName;
    }
}