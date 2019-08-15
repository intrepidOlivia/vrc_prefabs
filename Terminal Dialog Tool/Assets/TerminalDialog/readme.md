# Terminal Dialog Tool

## Introduction

The Terminal Dialog Tool is a set of Unity Editor scripts that will enables the creation of computer dialog screens (ala text-based games, NPC conversations, etc) for use in VRChat. These are generated using VRC Triggers, but without the hassle of setting up the VRC Triggers manually. The user can write text for an arbitrary number of screens and options for those screens, and then automatically generate the game objects and triggers required.

A default computer terminal model and screen/option prefabs are provided, but you can replace them with your own as long as they follow the necessary hierarchy constraints.

## Instructions

### Getting started

* Import the TerminalDialogCreator.package Unity Package into your Unity project.
  * Note: This package requires the VRCSDK, but it is not included in the package. You must download this yourself from [VRChat.com](https://docs.vrchat.com/docs/setting-up-the-sdk).
* Open up the TerminalSampleScene scene found in Assets/TerminalDialog/Scenes/. This is where it is recommended to set up the prefab you will use in your own scene.
* Included in the scene are the pre-existing Computer Terminal object (enabled) and the Screen and Option prefabs (disabled). You can enable the Screen and Option to get a preview of what they look like, and can modify their properties (like font size) for use in your own terminal.
* The Computer Terminal object in the scene has a component with an "ID" property. Input a unique identifier for your computer terminal.

### Writing your content

* When you're ready to start building your screens, open up the Tools menu at the top and select TerminalDialog to display the dialog editor window.
  * The two first fields are text strings which are paths to the screen and option prefabs. Do not modify these unless you have moved those prefabs from their default spot in your project.
  * NOTE: It is highly recommended that you plan out beforehand what you want your screens/options to say.
* Drag the Computer Terminal into the Terminal Object field.
* To start a new screen, press the [+] button next to the screen selection bar.
  * Name your screen with a unique identifier and press Add Screen.
  * NOTE: Give your screens descriptive names to easily identify them later.
* Two editing sections will appear: Edit Screen and Edit Option. Modify the text that will be on the screen by inputting text into the "screen text" box.
* To add a new option, press the [+] button next to the option selection bar.
  * This will immediately create an option for your screen (options have numbers instead of ID's). Edit the Option's text by inputting text into the "option text" field, and select its destination screen by selecting from the dropdown menu. Its destination screen is the Screen that will appear when the user clicks this Option.
* Recommended Workflow: Create all Screens first. Then create all Options. this will allow you to select from all available screens when choosing a destination for your new Option.

### Finalizing your dialog

* You can preview the game objects for the screens you've created by pressing the Generate All Game Objects button. Or you can generate game objects for only one screen at a time by clicking Generate Game Objects for screen.
  * NOTE: Generating game objects will replace any previously-generated game objects with the same ID(s).
* All screens will be enabled by default; disable them selectively to inspect each one and make sure it looks the way you want. Any adjustments you make to the game objects will remain until you generate terminal game objects again.
* When everything looks the way you want, click Generate all VRCTriggers to generate all VRCTriggers for the Options on each screen, based on the option's set destination.
* Your terminal is done! At this point it is recommended to lock it as a prefab by dragging the entire Computer Terminal game object into the Project window. More information on prefabs and updating them can be found [here](https://docs.unity3d.com/2017.4/Documentation/Manual/Prefabs.html).
* Once you've created a prefab, you can open up another scene in your project and drag the prefab into it.

For questions, comments, or feedback, please visit this project's github at: https://github.com/intrepidOlivia/vrc_prefabs
