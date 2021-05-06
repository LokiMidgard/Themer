# Themer
A library to calculate forground and background colors from an image.

It is based on [vabriant.js](https://github.com/jariz/vibrant.js/) and [nQuant.cs](https://github.com/mcychan/nQuant.cs/tree/core/nQuant.Master).

## Getting Started

```c#
System.Drawing.Image image;
// assign image...

var vibrant = await Themer.Themes.Calculate(image);
var list = vibrant.GetColorPairs();
var (forgroundColor, backgroundColor, _, _, _, _) = list.First();

// use colors
```

You can also look in the provided sample.
