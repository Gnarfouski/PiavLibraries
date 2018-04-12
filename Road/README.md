# README #

### What is this repository for? ###

Making roads from points in 3D space, generating 3D meshes and XML navigation files.

### How do I get set up? ###

Get the /bin/Debug/TGD3RoadLibrary.dll, put it in a Unity project.
1. Create an object with the RoadConfig monobehavior attached.
2. Define your lane and road templates.
3. Make roads : GameObject > Create Other > Road Mesh
4. (optional) Make parking spaces with the ParkingSpaceData and stop signs / traffic lights with the StoppingData monobehaviors.
5. Create an object with the FullRoadXMLRegeneration monobehavior attached. Press 'Regenerate' to create XML file in Assets folder.

### Who do I talk to? ###

tgd.vitali.douliez@gmail.com