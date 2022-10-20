# Eye Tracking System for Radiologists
We introduce a DICOM viewer that can record the status of medical images in real time and some modules for eye tracking.

<div align="center">
    <img src="/res/ovreall.png">
</div>

## Contributions
We modified the source code of the ClearCanvas open source project to accomplish the following functions.

- Custom image list. After the software is pre-loaded with the list, the radiologists can switch to observe the images in the list at will on the software.

- Real-time image status tracking. We captured the software's graphical interface parameters in the source code, including the software's screen position, the screen position of each display panel, and the most important the position and zoom ratio of medical images in the display panel.

- The module of writing diagnostic reports. Radiologists can open the report writing module when observing images, and write reports while observing.

- Eye tracking module. We added the interface of tobii eye tracker, so that the eye tracker can run and collect the radiologists' eye movement data at the same time.


## Build
**1. You should download the ClearCanvas source code and compile it.**

ClearCanvas source code can be found at [ClearCanvas Project](https://clearcanvas.github.io/).
ClearCanvas is an open source code (C#) base for enabling software innovation in medical imaging. It contains a DICOM Viewer and a DICOM Server. In this project, we only use DICOM Viewer for image viewing. You have to open "ImageViewer.sln" with VisualStudio software and compile the project.

**2, Copy the code from the "Modified Code" folder to the appropriate location to overwrite the original code.**


**3, Make your own change like Data Saving Path, Data Saving Format, etc.**


Still modifying...
