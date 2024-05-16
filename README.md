# SampleCode
Some sample code to show things I've worked on. Generally I'm not able to show the entirety of the project, but I can show some scripts I created.


## HighlightPlace Codes
Used in Unity with Oculus plugins for the Physics lab.
There are a few versions of this code, each attempting a different way of snapping objects into certain places. When hovering over a triggering collider, a mesh would enable. This mesh would represent a holographic view of where the object would snap to once released.
This repository shows only a few versions.

[HighlightPlaceList.cs](https://github.com/reeseras/SampleCode/blob/main/HighlightPlaceList.cs)
This one attempted using a serializable class meant to be placed on other objects. Generally it was more annoying to move around/fix because of the serializable class that had to be attached to other objects. However, the serializable class could support filtering.

[HighlightPlaceJointList.cs](https://github.com/reeseras/SampleCode/blob/main/HighlightPlaceJointList.cs)
This one is the most widely used; it was easy to attach and remove from objects as needed. This one focused on creating a physics joint connecting the grabbable object and its placement. Worked very well for physics-based interactions.
