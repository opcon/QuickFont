###Todo
- [x] Implement Vertex Array support
- [x] Implement Shader support
- [x] Remove legacy OpenGL render code and switch to a fully programmable pipeline forward compatible with OpenGL 4.4
- [x] Remove option to pass in a null Configuration object - this only adds to visual complexity

###Please not this API is not backwards compatible with previous QuickFont releases


###Using Vertex Buffers
Initialize your vertex buffer
```C#
var config = new QFontBuilderConfiguration() 
{ 
  TextGenerationRenderHint = TextGenerationRenderHint.SystemDefault 
};

QFont qfont = new QFont(font, config);
```

Set orthographic projection matrix
````C#
qfont.ProjectionMatrix = projectionMatrix;
````

Print to the vertex buffer
```C#
qfont.Print("i love", new Vector3(0, 0, 0), Color.Red);
qfont.Print("quickfont", new Vector3(0, 10, 0), Color.Blue);
```

When you've printed everything call Load 
```C#
qfont.LoadVBOs();
```

Then draw it
```C#
qfont.DrawVBOs();
```

Keep calling DrawVBOs() each frame.  When something needs to change, reset the VBO
```C#
qfont.ResetVBOs();
```

Then repeat the process: Print, Load Draw.