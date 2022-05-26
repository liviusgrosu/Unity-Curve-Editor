# Path Creator

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Top%20down%20road.PNG)

##### Table of Contents  
* [Description](#description)
* [Controls](#controls)
* [Algorithm](#algorithm)
* [Articles](#articles)

## Description

An editor that creates complex curves with various settings/parameters

## Controls

### Editor Window

| Actions                                 | Key                                                 |
| --------------------------------------- | --------------------------------------------------- |
| Click on red anchor                     | Select anchor and its corresponding control points  |
| Handle on anchor                        | Move anchor point around                            |
| Handle on control                       | Move control point around                           |
| Click + drag red circle                 | Rotate anchor point around                          |
| Shift left click                        | Create new point                                    |
| Shift left click + hover over segment   | Create new point in-between segments                |
| Control left click + hover over anchor  | delete anchor point                                 |

### Inspector Window

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Path%20creator%20editor.PNG)

1) Anchor Colour: Change colour of the anchor points
2) Control Colour: Change colour of the control points
3) Segment Colour: Change colour of the segment curve
4) Selected Segment Colour: Change colour of the selected segment curve
5) Create New Button: Create new path

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Road%20creator%20editor.PNG)

- Road Width: Changes the width of the road
- Spacing: changes the spacing of each evenly placed point alongst the path
- Auto Update: Toggle automatic update of generating the road mesh
- Tiling: Changes the tiling of the texture
- Path Depth: Changes how deep the edges are
- Path Edge Width: Changes how far the edges are 

## Algorithm

In the most simplist form, a path is comprised of multiple segments that connect to one another. Each segement is a bezier curve that is constructed from the position data of two anchor points and two control points. The anchor point controls the position of start/end segment whilst the control point changes the influence of the bezier curve. The more drastic the position of the control point to its corresponding anchor point, the greater the influence

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Segment.PNG)
![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Collection%20of%20segments.png)

To construct a mesh, better resolution is needed as the given anchor points will not be enough information to make the roads turn smoothly & realistacly. In order to do that, multiple intermediate points are placed in each segement to provide that needed information. 

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Evenly%20spaced%20points.png)

From here, immediate left and right points can be added in order add width to the path but most importantly they allow triangles to inserted. Taking a look at the picture below, the set of points to form the first triangle will be {A - 1, B - 1, A + 1}. Take note the clockwise order as that indicates the normal direction of that constructed triangle. With that, all triangles can be created with these new immediate points which results in the second picture below. (NOTE: the resolution has been lowered to help better visualize the concept mentioned)

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Mesh%20vertices.png)
![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Mesh%20triangles.png)

Using the same coordinates for the vertices, the UV mesh coordinates can be consturcted too thus allowing for a textured mesh. Tiling can be adjusted to make the road look more believable

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Created%20mesh.PNG)
![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Textured%20mesh.PNG)

Next, the road mesh needs to include an additional working axis; the Z axis! Converting this to 3D space wasn't so much as a challenge as converting all vector2 types to vector3. However the biggest obstacle was converting HandleUtility.DistancePointBezier to a functional 3D counter-part. Now the solution I came up with isn't an exact coorelation but it gets the jump done. Essentially, a vector between each anchor is created and its compared to the mouse ray. The shortest distance between the two is calcualted and whichever segment vector is the closet becomes the selected. 

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/3d.PNG)
![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Skew%20shortest%20distance.png)

Rotation data was also added to each anchor point which allows users the ability to parts of the path. This is done why drawing a disc arc around the point in the direction of the previous and next 'evenly spaced point', and taking in the difference between the old and new rotation when the arc is dragged.

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Rotation%20arc.PNG)

Finally edges were added to the path to allow for more seamless transitions between floor and path. This was mainly added to make dirt paths feel more seamless when placed next to a floor mesh. Two additional vertices were added on the outside of the path as well as controls added to the heirchy editor to adjust the look of that edge.

![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Mesh%20edge.PNG)
![image](https://raw.githubusercontent.com/liviusgrosu/unity-curve-editor-demo/main/Pictures/Mesh%20edge.PNG)

## Articles

https://forum.unity.com/threads/how-to-make-camera-screentowroldpoint-work-on-xz-plane.918587/

https://www.youtube.com/watch?v=RF04Fi9OCPc&list=PLFt_AvWsXl0d8aDaovNztYf6iTChHzrHP

https://answers.unity.com/questions/660369/how-to-convert-python-in-to-c-maths.html

https://www.youtube.com/watch?v=HC5YikQxwZA

https://answers.unity.com/questions/599393/angles-from-quaternionvector-problem.html
