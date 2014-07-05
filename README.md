# Sony Vegas Pro Personalized Batch Renderer Template

 This render template for Sony Vegas Pro (10+) allows you to personalize the batch renderer.
 This allows you to kick off a batch render without going through the GUI dialog for selecting templates.
 The code was taken from the Vegas included Batch Render script and then modified to read choices from a file
 and skip the GUI.
 
## Installation

Download MyRenderer.cs file to the Vegas script directory (change version if needed):
    
    C:\Program Files\Sony\Vegas Pro 10.0\Script Menu\


## Steps to run

  - Restart Vegas Pro
  - Run script one time at first and this will create a folder under "My Documents/SonyVegasRender/"
    - This will contain 2 files:
        - VegasRendererList.txt
        - MyRendererInput.txt
    - You will take the lines that you want to run from VegasRendererList and copy them into the MyRendererInput.txt

## VegasRendererList.txt sample

Contains all supported Vegas file formats

    Renderer: Dolby Digital AC-3 Pro
    -----------------------------------
    Dolby Digital AC-3 Pro;Default Template
    Dolby Digital AC-3 Pro;Stereo DVD
    Dolby Digital AC-3 Pro;5.1 Surround DVD

    Renderer: MainConcept MPEG-2
    -----------------------------------
    MainConcept MPEG-2;Default Template
    MainConcept MPEG-2;SVCD NTSC
    MainConcept MPEG-2;SVCD PAL
    MainConcept MPEG-2;DVD NTSC
    MainConcept MPEG-2;DVD PAL
    MainConcept MPEG-2;DVD NTSC video stream
    MainConcept MPEG-2;DVD Architect NTSC video stream
    MainConcept MPEG-2;DVD Architect NTSC Widescreen video stream
    MainConcept MPEG-2;Blu-ray 1920x1080-24p, 25 Mbps video stream
    MainConcept MPEG-2;Blu-ray 1920x1080-50i, 25 Mbps video stream
    MainConcept MPEG-2;Blu-ray 1920x1080-60i, 25 Mbps video stream
 

## MyRendererInput.txt sample

The first line of the file is the render mode, Project, Regions or Selection
The next lines are for however many formats you would like to include in the batch render.

Sample Project mode:

    Project  #valid choices: Project Regions Selection
    Wave (Microsoft);44,100 Hz, 32 Bit (IEEE Float), Mono, PCM (float)
    Dolby Digital AC-3 Pro;Stereo DVD
    MainConcept MPEG-2;DVD Architect NTSC video stream

Sample Regions mode:

    Regions
    Wave (Microsoft);44,100 Hz, 32 Bit (IEEE Float), Mono, PCM (float)
    Dolby Digital AC-3 Pro;Stereo DVD
    MainConcept MPEG-2;DVD Architect NTSC video stream

Sample Selection mode:

    Selection
    Wave (Microsoft);44,100 Hz, 32 Bit (IEEE Float), Mono, PCM (float)
    MainConcept MPEG-2;Blu-ray 1920x1080-60i, 25 Mbps video stream


### Options

One nice feature is that you can play a .wav file to alert you when the render completes.

 - Place a sound file in My Documents\SonyVegasRender\ and rename it as EndSound.wav

You may create multiple customized render scripts
 
 - First Open MyRenderer.cs and Save As a new file name in the same directory,
  such as MyBluRayRenderer.cs
 - Edit this line in the file:

```C#
String _defaultInputFileName = "MyRendererInput.txt";
```

to

```C#
String _defaultInputFileName = "MyBluRayRendererInput.txt";
```

 - Next create a matching input file, such as MyBluRayRendererInput.txt in the My Documents/SonyVegasRender folder.
 - Restart Vegas Pro
 - Run new script, like MyBluRayRenderer


### Advanced Options

By default

 - If the rendered media file already exists in the directory, it will overwrite it!
 - The script will recreate the VegasRendererList.txt every time.

These and other defaults can be changed by editing the top of the MyRenderer.cs script.

## Hints

Create a toolbar button to enable single click batch render:

- From Vegas menu Options/Customer Toolbar
    - Choose the MyRenderer script.


## License

MIT

I created this for my own use and decided to share it with the community.  Please read instructions carefully and backup your project until you are comfortable with the script.  Please use at your own risk.