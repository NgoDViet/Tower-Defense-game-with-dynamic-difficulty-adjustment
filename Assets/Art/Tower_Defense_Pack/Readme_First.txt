 ___        __       
|_ _|_ __  / _| ___  
 | || '_ \| |_ / _ \ 
 | || | | |  _| (_) |
|___|_| |_|_|  \___/ 

-- #### Before you run version 2.0 .........

-*To start into "MainMenu" scene click on Start.
-"Important" you must add the different layers used in this pack, please see the example capture image "TagsAndLayers" (TagsAndLayers.png)
-"Important" you must add in "Build Settings" (File/ Build Settings...) the different scenes. You must add:
		-SelectPlataform
		-MainMenu
		-MainMenuPhone
		-Example_Scene, 2 and 3
		-Phone_Example_Scene, 2 and 3
	*I recommend... if you only want build "finally" for Pc o android, please see the example capture of build settings ("PC_Build_Settings_Example.png" or "Android_Build_Settings_Example.png")
	*While you are testing on pc i recommend to add all previous scenes.

There is more info and contact-info in "/Tutorials_and_Info" sub-folder.

Note: If you do not add the layers and the scenes in Build Settings, the code will not work correctly.
Note: If you see the "hand" mouse cursor in an error place into the screen (Only editor), please stop and play again or put "scene window" and "game window" on a same TV screen, play and now you can put on place that you want.
          -It usually occurs when the "scene window" and "game window" are in different TV screens.

##################################################################################[ Update v 2.0 ]##########################################################

This Update contains:

	-Pc scenes						("MainMenu","Example_Scene", "Example_Scene2", "Example_Scene3")
   	-Android phone scenes			("Phone_Example_Scene" ,"Phone_Example_Scene2" ,"Phone_Example_Scene3")
   	-Waves editor					("Waves_Editor" scene)
   	-Sorcerer enemy
   	-Air enemy
   	-Fire controlled by "Object Pooling".

How to prepare the scene for waves editor:
	-All you need is: Create the custom path points like in the video ("https://www.youtube.com/watch?v=AVhgq9JfewU"), but How can i create different paths?
			In the video you can see the path 'a' --> a1, a2, a3... aEnd. 
			Example creating path "b": like the video but name b0, b1, b2, b3 ... bEnd.
			Example creating path "c": like the video but name c0, c1, c2, c3 ... cEnd.
			......................
	When you use the waves editor you must to put a, b, c or your custom letter into "Path inputfield" to create your custom wave.

-"The waves editor": How to use video here "https://www.youtube.com/watch?v=vPLLUSIcC50"
	-Also you have a little tuto into "Tutorials_and_Info/Waves_Editor.txt"

-"The sorcerer": 
	-He can create runes to deactivate the towers a little time. By default this time is 10f but you can change it into scene, go to Instance_Point gameobject, Master_Instance.cs script and change "Sorcerer_Runes_Time".
	-He will deactivate the towers near him. If a tower is created near... the sorcerer automatically will deactivate it.
	-When is calling the runes, the sorcerer must be stopped.

-"The air enemy"
	-He only can be damaged from archer o mage towers. Never will stop!!!!

-Here you can see an "Android Phone MiniGamePLay"---> "https://www.youtube.com/watch?v=E8xkLTOzqLo"

-Fire controlled by "Object Pooling", "Fire_Pooling.cs", this script is very important to get more memory into mobile plataforms, it contains one var called "amount" by default = 50, what is this? 
	-Only can exist 50 fire particles at the same time in the scene, why this method?
	-Object pooling re-use the objects and save memory!!!
	-Fantastic tutorial here: "https://unity3d.com/es/learn/tutorials/modules/beginner/live-training-archive/object-pooling"

##################################################################################[ ------------ ]###########################################################
##################################################################################[ Update v 3.0 ]##########################################################

Now you can set the sounds easily, go to AudioManager gameobject into scene / Audio_Manager.cs, here you can change the sounds and volume.
	-Some elements have array of sounds, you can add all sounds that you want, the sound will be selected via random among them.
	-Music is assigned on AudioManager gameobject / AudioSource into the scene.
	-Sound when click on button is assigned on 'UI' gameobject / AudioSource into the scene.

*More free effects here: http://www.bfxr.net/

######################### [ ]
For Phone scenes set the Game window on editor to 16:9 Landscape
For build to Android target, first add on buid settings the phone scenes:
	MainMenuPhone, Phone_Example_Scene, .. 2, .. 3.
	For build... First open the MainMenuPhone scene on the editor and then build