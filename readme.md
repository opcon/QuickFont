
###Using Vertex Buffers
Initialize your vertex buffer
```C#
var config = new QFontBuilderConfiguration() 
{ 
  UseVertexBuffer = true,
  TextGenerationRenderHint = TextGenerationRenderHint.SystemDefault 
};

QFont qfont = new QFont(font, config);
```

Print to the vertex buffer
```C#
qfont.PrintToVBO("i love", new Vector3(0, 0, 0), Color.Red);
qfont.PrintToVBO("quickfont", new Vector3(0, 10, 0), Color.Blue);
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